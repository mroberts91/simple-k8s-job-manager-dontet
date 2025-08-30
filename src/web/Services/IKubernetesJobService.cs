using k8s.Models;
using K8sJobManager.Models;

namespace K8sJobManager.Services;

public interface IKubernetesJobService
{
    Task<V1Job> CreateJobAsync(JobRequest jobRequest);
    Task<V1Job?> GetJobAsync(string jobName, string namespaceName = "default");
    Task<V1JobStatus?> GetJobStatusAsync(string jobName, string namespaceName = "default");
    Task DeleteJobAsync(string jobName, string namespaceName = "default");
    Task<bool> IsJobCompleteAsync(string jobName, string namespaceName = "default");
    Task<bool> IsJobFailedAsync(string jobName, string namespaceName = "default");
    Task<List<V1Job>> GetAllJobsAsync(string namespaceName = "default");
    Task<List<V1Job>> GetStaleJobsAsync(string namespaceName = "default", TimeSpan? olderThan = null);
    Task<KubernetesJobInfo> ConvertToKubernetesJobInfoAsync(V1Job job);
    Task<KubernetesResourceInfo?> GetJobResourceInfoAsync(string jobName, string namespaceName = "default");
}