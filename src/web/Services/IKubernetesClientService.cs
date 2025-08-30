using k8s;

namespace K8sJobManager.Services;

public interface IKubernetesClientService
{
    IKubernetes Client { get; }
}