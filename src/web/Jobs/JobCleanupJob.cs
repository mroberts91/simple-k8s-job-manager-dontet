using Quartz;
using K8sJobManager.Services;

namespace K8sJobManager.Jobs;

[DisallowConcurrentExecution]
public class JobCleanupJob : IJob
{
    private readonly IKubernetesJobService _kubernetesJobService;
    private readonly ILogger<JobCleanupJob> _logger;

    public JobCleanupJob(
        IKubernetesJobService kubernetesJobService,
        ILogger<JobCleanupJob> logger)
    {
        _kubernetesJobService = kubernetesJobService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("JobCleanupJob started");

        try
        {
            var staleThreshold = TimeSpan.FromHours(2);
            var staleJobs = await _kubernetesJobService.GetStaleJobsAsync("default", staleThreshold);
            
            _logger.LogInformation("Found {StaleJobCount} stale jobs to clean up", staleJobs.Count);

            foreach (var staleJob in staleJobs)
            {
                try
                {
                    _logger.LogInformation("Cleaning up stale Kubernetes job {JobName}", staleJob.Metadata.Name);
                    await _kubernetesJobService.DeleteJobAsync(staleJob.Metadata.Name);
                    _logger.LogInformation("Successfully cleaned up job {JobName}", staleJob.Metadata.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up stale job {JobName}", staleJob.Metadata.Name);
                }
            }

            await CleanupOrphanedResourcesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobCleanupJob failed");
        }

        _logger.LogInformation("JobCleanupJob completed");
    }

    private async Task CleanupOrphanedResourcesAsync()
    {
        try
        {
            var allJobs = await _kubernetesJobService.GetAllJobsAsync();
            var staleCutoff = DateTime.UtcNow - TimeSpan.FromDays(1);

            var orphanedJobs = allJobs.Where(job => 
                job.Metadata.CreationTimestamp < staleCutoff &&
                (job.Status?.Succeeded == null && job.Status?.Failed == null && job.Status?.Active == null))
                .ToList();

            _logger.LogInformation("Found {OrphanedJobCount} potentially orphaned jobs", orphanedJobs.Count);

            foreach (var orphanedJob in orphanedJobs)
            {
                try
                {
                    _logger.LogInformation("Cleaning up orphaned job {JobName}", orphanedJob.Metadata.Name);
                    await _kubernetesJobService.DeleteJobAsync(orphanedJob.Metadata.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up orphaned job {JobName}", orphanedJob.Metadata.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup orphaned resources");
        }
    }
}