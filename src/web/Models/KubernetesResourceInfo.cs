namespace K8sJobManager.Models;

public class KubernetesResourceInfo
{
    public KubernetesJobInfo? Job { get; set; }
    public List<KubernetesPodInfo> Pods { get; set; } = new();
}

public class KubernetesJobInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public DateTime? CreationTimestamp { get; set; }
    public string Phase { get; set; } = string.Empty;
    public int? Active { get; set; }
    public int? Succeeded { get; set; }
    public int? Failed { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public List<KubernetesCondition> Conditions { get; set; } = new();
}

public class KubernetesPodInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public DateTime? CreationTimestamp { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string? PodIP { get; set; }
    public string? NodeName { get; set; }
    public DateTime? StartTime { get; set; }
    public List<KubernetesContainerStatus> ContainerStatuses { get; set; } = new();
    public List<KubernetesCondition> Conditions { get; set; } = new();
}

public class KubernetesContainerStatus
{
    public string Name { get; set; } = string.Empty;
    public bool Ready { get; set; }
    public int RestartCount { get; set; }
    public string Image { get; set; } = string.Empty;
    public string? ImageID { get; set; }
    public string? ContainerID { get; set; }
    public KubernetesContainerState? State { get; set; }
    public KubernetesContainerState? LastTerminationState { get; set; }
}

public class KubernetesContainerState
{
    public string State { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Reason { get; set; }
    public string? Message { get; set; }
    public int? ExitCode { get; set; }
    public int? Signal { get; set; }
}

public class KubernetesCondition
{
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastTransitionTime { get; set; }
    public string? Reason { get; set; }
    public string? Message { get; set; }
}