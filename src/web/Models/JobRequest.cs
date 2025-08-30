namespace K8sJobManager.Models;

public class JobRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ContainerImage { get; set; } = string.Empty;
    public Dictionary<string, string> Configuration { get; set; } = new();
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? KubernetesJobName { get; set; }
    public string? ErrorMessage { get; set; }
    public int RestartCount { get; set; } = 0;
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<string> Command { get; set; } = new();
    public List<string> Args { get; set; } = new();
    public KubernetesResourceInfo? KubernetesInfo { get; set; }
}

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    Orphaned
}