using k8s;
using k8s.Models;
using K8sJobManager.Models;

namespace K8sJobManager.Services;

public class KubernetesPodService : IKubernetesPodService
{
    private readonly IKubernetesClientService _kubernetesClient;
    private readonly ILogger<KubernetesPodService> _logger;

    public KubernetesPodService(IKubernetesClientService kubernetesClient, ILogger<KubernetesPodService> logger)
    {
        _kubernetesClient = kubernetesClient;
        _logger = logger;
    }

    public async Task<List<V1Pod>> GetPodsForJobAsync(string jobName, string namespaceName = "default")
    {
        try
        {
            var podList = await _kubernetesClient.Client.CoreV1.ListNamespacedPodAsync(
                namespaceName,
                labelSelector: $"job-name={jobName}");
            
            return podList.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pods for job {JobName} in namespace {Namespace}", jobName, namespaceName);
            return new List<V1Pod>();
        }
    }

    public async Task<List<V1Pod>> GetAllPodsAsync(string namespaceName = "default")
    {
        try
        {
            var podList = await _kubernetesClient.Client.CoreV1.ListNamespacedPodAsync(
                namespaceName,
                labelSelector: "managed-by=k8s-job-manager");
            
            return podList.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pods in namespace {Namespace}", namespaceName);
            return new List<V1Pod>();
        }
    }

    public async Task<V1Pod?> GetPodAsync(string podName, string namespaceName = "default")
    {
        try
        {
            return await _kubernetesClient.Client.CoreV1.ReadNamespacedPodAsync(podName, namespaceName);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pod {PodName} in namespace {Namespace}", podName, namespaceName);
            return null;
        }
    }

    public async Task<string> GetPodLogsAsync(string podName, string namespaceName = "default", string? containerName = null)
    {
        try
        {
            using var logStream = await _kubernetesClient.Client.CoreV1.ReadNamespacedPodLogAsync(
                podName, 
                namespaceName,
                container: containerName);
            
            using var reader = new StreamReader(logStream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for pod {PodName} in namespace {Namespace}", podName, namespaceName);
            return string.Empty;
        }
    }

    public async Task<List<KubernetesPodInfo>> ConvertToKubernetesPodInfoAsync(List<V1Pod> pods)
    {
        var podInfos = new List<KubernetesPodInfo>();
        foreach (var pod in pods)
        {
            podInfos.Add(await ConvertToKubernetesPodInfoAsync(pod));
        }
        return podInfos;
    }

    public async Task<KubernetesPodInfo> ConvertToKubernetesPodInfoAsync(V1Pod pod)
    {
        var podInfo = new KubernetesPodInfo
        {
            Name = pod.Metadata.Name,
            Namespace = pod.Metadata.NamespaceProperty,
            CreationTimestamp = pod.Metadata.CreationTimestamp,
            Phase = pod.Status?.Phase ?? "Unknown",
            PodIP = pod.Status?.PodIP,
            NodeName = pod.Spec?.NodeName,
            StartTime = pod.Status?.StartTime,
            Conditions = pod.Status?.Conditions?.Select(c => new KubernetesCondition
            {
                Type = c.Type,
                Status = c.Status,
                LastTransitionTime = c.LastTransitionTime,
                Reason = c.Reason,
                Message = c.Message
            }).ToList() ?? new List<KubernetesCondition>(),
            ContainerStatuses = pod.Status?.ContainerStatuses?.Select(cs => new KubernetesContainerStatus
            {
                Name = cs.Name,
                Ready = cs.Ready,
                RestartCount = cs.RestartCount,
                Image = cs.Image,
                ImageID = cs.ImageID,
                ContainerID = cs.ContainerID,
                State = ConvertContainerState(cs.State),
                LastTerminationState = ConvertContainerState(cs.LastState)
            }).ToList() ?? new List<KubernetesContainerStatus>()
        };

        return await Task.FromResult(podInfo);
    }

    private static KubernetesContainerState? ConvertContainerState(V1ContainerState? state)
    {
        if (state == null) return null;

        if (state.Running != null)
        {
            return new KubernetesContainerState
            {
                State = "Running",
                StartedAt = state.Running.StartedAt
            };
        }
        
        if (state.Terminated != null)
        {
            return new KubernetesContainerState
            {
                State = "Terminated",
                StartedAt = state.Terminated.StartedAt,
                FinishedAt = state.Terminated.FinishedAt,
                Reason = state.Terminated.Reason,
                Message = state.Terminated.Message,
                ExitCode = state.Terminated.ExitCode,
                Signal = state.Terminated.Signal
            };
        }
        
        if (state.Waiting != null)
        {
            return new KubernetesContainerState
            {
                State = "Waiting",
                Reason = state.Waiting.Reason,
                Message = state.Waiting.Message
            };
        }

        return null;
    }
}