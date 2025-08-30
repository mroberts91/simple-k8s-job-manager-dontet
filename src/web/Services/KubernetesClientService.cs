using k8s;

namespace K8sJobManager.Services;

public class KubernetesClientService : IKubernetesClientService
{
    public IKubernetes Client { get; }

    public KubernetesClientService(IConfiguration configuration, ILogger<KubernetesClientService> logger)
    {
        try
        {
            KubernetesClientConfiguration config;
            
            if (KubernetesClientConfiguration.IsInCluster())
            {
                logger.LogInformation("Using in-cluster Kubernetes configuration");
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            else
            {
                logger.LogInformation("Using kubectl configuration from local machine");
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

            Client = new Kubernetes(config);
            logger.LogInformation("Successfully initialized Kubernetes client");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Kubernetes client. Ensure kubectl is configured or running in cluster with proper service account.");
            throw;
        }
    }
}