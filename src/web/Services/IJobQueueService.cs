using K8sJobManager.Models;

namespace K8sJobManager.Services;

public interface IJobQueueService
{
    Task<JobRequest> CreateJobAsync(JobRequest jobRequest);
    Task<JobRequest?> GetJobAsync(Guid id);
    Task<List<JobRequest>> GetAllJobsAsync();
    Task<List<JobRequest>> GetJobsByStatusAsync(JobStatus status);
    Task<JobRequest> UpdateJobStatusAsync(Guid id, JobStatus status, string? errorMessage = null);
    Task<JobRequest> UpdateJobKubernetesNameAsync(Guid id, string kubernetesJobName);
    Task<List<JobRequest>> GetQueuedJobsAsync();
    Task<List<JobRequest>> GetRunningJobsAsync();
    Task<List<JobRequest>> GetStaleJobsAsync(TimeSpan staleThreshold);
    Task<JobRequest> UpdateJobWithKubernetesInfoAsync(Guid id, KubernetesResourceInfo kubernetesInfo);
}