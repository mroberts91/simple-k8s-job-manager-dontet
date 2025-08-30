using k8s.Models;
using K8sJobManager.Models;

namespace K8sJobManager.Services;

public interface IKubernetesPodService
{
    Task<List<V1Pod>> GetPodsForJobAsync(string jobName, string namespaceName = "default");
    Task<List<V1Pod>> GetAllPodsAsync(string namespaceName = "default");
    Task<V1Pod?> GetPodAsync(string podName, string namespaceName = "default");
    Task<string> GetPodLogsAsync(string podName, string namespaceName = "default", string? containerName = null);
    Task<List<KubernetesPodInfo>> ConvertToKubernetesPodInfoAsync(List<V1Pod> pods);
    Task<KubernetesPodInfo> ConvertToKubernetesPodInfoAsync(V1Pod pod);
}