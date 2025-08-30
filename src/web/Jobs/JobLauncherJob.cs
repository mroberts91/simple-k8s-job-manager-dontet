using Quartz;
using K8sJobManager.Models;
using K8sJobManager.Services;

namespace K8sJobManager.Jobs;

[DisallowConcurrentExecution]
public class JobLauncherJob : IJob
{
    private readonly IJobQueueService _jobQueueService;
    private readonly IKubernetesJobService _kubernetesJobService;
    private readonly ILogger<JobLauncherJob> _logger;

    public JobLauncherJob(
        IJobQueueService jobQueueService,
        IKubernetesJobService kubernetesJobService,
        ILogger<JobLauncherJob> logger)
    {
        _jobQueueService = jobQueueService;
        _kubernetesJobService = kubernetesJobService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("JobLauncherJob started");

        try
        {
            var queuedJobs = await _jobQueueService.GetQueuedJobsAsync();
            _logger.LogInformation("Found {JobCount} queued jobs", queuedJobs.Count);

            foreach (var jobRequest in queuedJobs)
            {
                try
                {
                    _logger.LogInformation("Launching job {JobId} ({JobName})", jobRequest.Id, jobRequest.Name);
                    
                    var kubernetesJob = await _kubernetesJobService.CreateJobAsync(jobRequest);
                    
                    await _jobQueueService.UpdateJobKubernetesNameAsync(jobRequest.Id, kubernetesJob.Metadata.Name);
                    await _jobQueueService.UpdateJobStatusAsync(jobRequest.Id, JobStatus.Running);
                    
                    _logger.LogInformation("Successfully launched job {JobId} as Kubernetes job {K8sJobName}", 
                        jobRequest.Id, kubernetesJob.Metadata.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to launch job {JobId} ({JobName})", jobRequest.Id, jobRequest.Name);
                    await _jobQueueService.UpdateJobStatusAsync(jobRequest.Id, JobStatus.Failed, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobLauncherJob failed");
        }

        _logger.LogInformation("JobLauncherJob completed");
    }
}