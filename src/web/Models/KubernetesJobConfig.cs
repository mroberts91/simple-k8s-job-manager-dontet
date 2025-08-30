namespace K8sJobManager.Models;

public class KubernetesJobConfig
{
    public string JobName { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default";
    public string ContainerImage { get; set; } = string.Empty;
    public List<string> Command { get; set; } = new();
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string> ConfigMapData { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
    public int BackoffLimit { get; set; } = 3;
    public int TTLSecondsAfterFinished { get; set; } = 300; // 5 minutes
    public string RestartPolicy { get; set; } = "Never";
    public ResourceRequirements? Resources { get; set; }
}

public class ResourceRequirements
{
    public Dictionary<string, string> Limits { get; set; } = new();
    public Dictionary<string, string> Requests { get; set; } = new();
}