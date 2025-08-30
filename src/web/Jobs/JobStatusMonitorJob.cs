using Quartz;
using K8sJobManager.Models;
using K8sJobManager.Services;

namespace K8sJobManager.Jobs;

[DisallowConcurrentExecution]
public class JobStatusMonitorJob : IJob
{
    private readonly IJobQueueService _jobQueueService;
    private readonly IKubernetesJobService _kubernetesJobService;
    private readonly ILogger<JobStatusMonitorJob> _logger;

    public JobStatusMonitorJob(
        IJobQueueService jobQueueService,
        IKubernetesJobService kubernetesJobService,
        ILogger<JobStatusMonitorJob> logger)
    {
        _jobQueueService = jobQueueService;
        _kubernetesJobService = kubernetesJobService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("JobStatusMonitorJob started");

        try
        {
            var runningJobs = await _jobQueueService.GetRunningJobsAsync();
            _logger.LogInformation("Monitoring {JobCount} running jobs", runningJobs.Count);

            foreach (var jobRequest in runningJobs)
            {
                try
                {
                    if (string.IsNullOrEmpty(jobRequest.KubernetesJobName))
                    {
                        _logger.LogWarning("Job {JobId} is marked as running but has no Kubernetes job name", jobRequest.Id);
                        continue;
                    }

                    var isCompleted = await _kubernetesJobService.IsJobCompleteAsync(jobRequest.KubernetesJobName);
                    var isFailed = await _kubernetesJobService.IsJobFailedAsync(jobRequest.KubernetesJobName);

                    var kubernetesInfo = await _kubernetesJobService.GetJobResourceInfoAsync(jobRequest.KubernetesJobName);
                    if (kubernetesInfo != null)
                    {
                        await _jobQueueService.UpdateJobWithKubernetesInfoAsync(jobRequest.Id, kubernetesInfo);
                    }

                    if (isCompleted)
                    {
                        await _jobQueueService.UpdateJobStatusAsync(jobRequest.Id, JobStatus.Completed);
                        _logger.LogInformation("Job {JobId} ({JobName}) completed successfully", 
                            jobRequest.Id, jobRequest.Name);
                    }
                    else if (isFailed)
                    {
                        var jobStatus = await _kubernetesJobService.GetJobStatusAsync(jobRequest.KubernetesJobName);
                        var errorMessage = jobStatus?.Conditions?.LastOrDefault()?.Message ?? "Job failed";
                        
                        await _jobQueueService.UpdateJobStatusAsync(jobRequest.Id, JobStatus.Failed, errorMessage);
                        _logger.LogWarning("Job {JobId} ({JobName}) failed: {ErrorMessage}", 
                            jobRequest.Id, jobRequest.Name, errorMessage);
                    }
                    else
                    {
                        var job = await _kubernetesJobService.GetJobAsync(jobRequest.KubernetesJobName);
                        if (job == null)
                        {
                            await _jobQueueService.UpdateJobStatusAsync(jobRequest.Id, JobStatus.Orphaned, 
                                "Kubernetes job not found - may have been deleted externally");
                            _logger.LogWarning("Job {JobId} ({JobName}) is orphaned - Kubernetes job not found", 
                                jobRequest.Id, jobRequest.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check status of job {JobId} ({JobName})", 
                        jobRequest.Id, jobRequest.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobStatusMonitorJob failed");
        }

        _logger.LogInformation("JobStatusMonitorJob completed");
    }
}