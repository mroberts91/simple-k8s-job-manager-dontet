using k8s;
using k8s.Models;
using K8sJobManager.Models;

namespace K8sJobManager.Services;

public class KubernetesJobService : IKubernetesJobService
{
    private readonly IKubernetesClientService _kubernetesClient;
    private readonly IKubernetesPodService _kubernetesPodService;
    private readonly ILogger<KubernetesJobService> _logger;

    public KubernetesJobService(IKubernetesClientService kubernetesClient, IKubernetesPodService kubernetesPodService, ILogger<KubernetesJobService> logger)
    {
        _kubernetesClient = kubernetesClient;
        _kubernetesPodService = kubernetesPodService;
        _logger = logger;
    }

    public async Task<V1Job> CreateJobAsync(JobRequest jobRequest)
    {
        var jobName = $"job-{jobRequest.Id.ToString().ToLower()}";
        var configMapName = $"config-{jobRequest.Id.ToString().ToLower()}";

        try
        {
            await CreateConfigMapAsync(configMapName, jobRequest.Configuration, jobRequest.Labels);

            var job = BuildJobSpec(jobName, configMapName, jobRequest);
            var createdJob = await _kubernetesClient.Client.BatchV1.CreateNamespacedJobAsync(job, "default");
            
            _logger.LogInformation("Created Kubernetes job {JobName} for request {JobId}", jobName, jobRequest.Id);
            return createdJob;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Kubernetes job for request {JobId}", jobRequest.Id);
            throw;
        }
    }

    private async Task CreateConfigMapAsync(string configMapName, Dictionary<string, string> configuration, Dictionary<string, string> labels)
    {
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta
            {
                Name = configMapName,
                Labels = new Dictionary<string, string>(labels)
                {
                    ["managed-by"] = "k8s-job-manager"
                }
            },
            Data = configuration
        };

        await _kubernetesClient.Client.CoreV1.CreateNamespacedConfigMapAsync(configMap, "default");
    }

    private V1Job BuildJobSpec(string jobName, string configMapName, JobRequest jobRequest)
    {
        var labels = new Dictionary<string, string>(jobRequest.Labels)
        {
            ["managed-by"] = "k8s-job-manager",
            ["job-id"] = jobRequest.Id.ToString()
        };

        return new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = jobName,
                Labels = labels
            },
            Spec = new V1JobSpec
            {
                BackoffLimit = 3,
                TtlSecondsAfterFinished = 300,
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "job-container",
                                Image = jobRequest.ContainerImage,
                                Command = jobRequest.Command.Count > 0 ? jobRequest.Command : null,
                                Args = jobRequest.Args.Count > 0 ? jobRequest.Args : null,
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "config-volume",
                                        MountPath = "/config"
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "config-volume",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = configMapName
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public async Task<V1Job?> GetJobAsync(string jobName, string namespaceName = "default")
    {
        try
        {
            return await _kubernetesClient.Client.BatchV1.ReadNamespacedJobAsync(jobName, namespaceName);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<V1JobStatus?> GetJobStatusAsync(string jobName, string namespaceName = "default")
    {
        var job = await GetJobAsync(jobName, namespaceName);
        return job?.Status;
    }

    public async Task DeleteJobAsync(string jobName, string namespaceName = "default")
    {
        try
        {
            await _kubernetesClient.Client.BatchV1.DeleteNamespacedJobAsync(
                jobName, 
                namespaceName,
                new V1DeleteOptions { PropagationPolicy = "Background" });
            
            var configMapName = $"config-{jobName.Replace("job-", "")}";
            await _kubernetesClient.Client.CoreV1.DeleteNamespacedConfigMapAsync(configMapName, namespaceName);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }

    public async Task<bool> IsJobCompleteAsync(string jobName, string namespaceName = "default")
    {
        var status = await GetJobStatusAsync(jobName, namespaceName);
        return status?.Succeeded > 0;
    }

    public async Task<bool> IsJobFailedAsync(string jobName, string namespaceName = "default")
    {
        var status = await GetJobStatusAsync(jobName, namespaceName);
        return status?.Failed > 0;
    }

    public async Task<List<V1Job>> GetAllJobsAsync(string namespaceName = "default")
    {
        var jobList = await _kubernetesClient.Client.BatchV1.ListNamespacedJobAsync(
            namespaceName,
            labelSelector: "managed-by=k8s-job-manager");
        
        return jobList.Items.ToList();
    }

    public async Task<List<V1Job>> GetStaleJobsAsync(string namespaceName = "default", TimeSpan? olderThan = null)
    {
        var cutoff = DateTime.UtcNow - (olderThan ?? TimeSpan.FromHours(2));
        var jobs = await GetAllJobsAsync(namespaceName);
        
        return jobs.Where(job => 
            job.Metadata.CreationTimestamp < cutoff && 
            (job.Status?.Succeeded > 0 || job.Status?.Failed > 0))
            .ToList();
    }

    public async Task<KubernetesJobInfo> ConvertToKubernetesJobInfoAsync(V1Job job)
    {
        var jobInfo = new KubernetesJobInfo
        {
            Name = job.Metadata.Name,
            Namespace = job.Metadata.NamespaceProperty,
            CreationTimestamp = job.Metadata.CreationTimestamp,
            Phase = GetJobPhase(job),
            Active = job.Status?.Active,
            Succeeded = job.Status?.Succeeded,
            Failed = job.Status?.Failed,
            StartTime = job.Status?.StartTime,
            CompletionTime = job.Status?.CompletionTime,
            Conditions = job.Status?.Conditions?.Select(c => new KubernetesCondition
            {
                Type = c.Type,
                Status = c.Status,
                LastTransitionTime = c.LastTransitionTime,
                Reason = c.Reason,
                Message = c.Message
            }).ToList() ?? new List<KubernetesCondition>()
        };

        return await Task.FromResult(jobInfo);
    }

    public async Task<KubernetesResourceInfo?> GetJobResourceInfoAsync(string jobName, string namespaceName = "default")
    {
        try
        {
            var job = await GetJobAsync(jobName, namespaceName);
            if (job == null) return null;

            var pods = await _kubernetesPodService.GetPodsForJobAsync(jobName, namespaceName);

            return new KubernetesResourceInfo
            {
                Job = await ConvertToKubernetesJobInfoAsync(job),
                Pods = await _kubernetesPodService.ConvertToKubernetesPodInfoAsync(pods)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource info for job {JobName} in namespace {Namespace}", jobName, namespaceName);
            return null;
        }
    }

    private static string GetJobPhase(V1Job job)
    {
        if (job.Status?.Succeeded > 0) return "Succeeded";
        if (job.Status?.Failed > 0) return "Failed";
        if (job.Status?.Active > 0) return "Running";
        return "Pending";
    }
}