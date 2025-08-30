using Microsoft.AspNetCore.Mvc;
using K8sJobManager.Models;
using K8sJobManager.Services;

namespace K8sJobManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KubernetesController : ControllerBase
{
    private readonly IKubernetesJobService _kubernetesJobService;
    private readonly IKubernetesPodService _kubernetesPodService;
    private readonly ILogger<KubernetesController> _logger;

    public KubernetesController(
        IKubernetesJobService kubernetesJobService,
        IKubernetesPodService kubernetesPodService,
        ILogger<KubernetesController> logger)
    {
        _kubernetesJobService = kubernetesJobService;
        _kubernetesPodService = kubernetesPodService;
        _logger = logger;
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<List<KubernetesJobInfo>>> GetAllKubernetesJobs([FromQuery] string namespaceName = "default")
    {
        try
        {
            var jobs = await _kubernetesJobService.GetAllJobsAsync(namespaceName);
            var jobInfos = new List<KubernetesJobInfo>();
            
            foreach (var job in jobs)
            {
                jobInfos.Add(await _kubernetesJobService.ConvertToKubernetesJobInfoAsync(job));
            }
            
            return jobInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Kubernetes jobs from namespace {Namespace}", namespaceName);
            return StatusCode(500, "Failed to retrieve Kubernetes jobs");
        }
    }

    [HttpGet("jobs/{jobName}")]
    public async Task<ActionResult<KubernetesJobInfo>> GetKubernetesJob(string jobName, [FromQuery] string namespaceName = "default")
    {
        try
        {
            var job = await _kubernetesJobService.GetJobAsync(jobName, namespaceName);
            if (job == null)
                return NotFound($"Job '{jobName}' not found in namespace '{namespaceName}'");

            return await _kubernetesJobService.ConvertToKubernetesJobInfoAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Kubernetes job {JobName} from namespace {Namespace}", jobName, namespaceName);
            return StatusCode(500, "Failed to retrieve Kubernetes job");
        }
    }

    [HttpGet("jobs/{jobName}/pods")]
    public async Task<ActionResult<List<KubernetesPodInfo>>> GetJobPods(string jobName, [FromQuery] string namespaceName = "default")
    {
        try
        {
            var pods = await _kubernetesPodService.GetPodsForJobAsync(jobName, namespaceName);
            return await _kubernetesPodService.ConvertToKubernetesPodInfoAsync(pods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pods for job {JobName} from namespace {Namespace}", jobName, namespaceName);
            return StatusCode(500, "Failed to retrieve job pods");
        }
    }

    [HttpGet("jobs/{jobName}/resource-info")]
    public async Task<ActionResult<KubernetesResourceInfo>> GetJobResourceInfo(string jobName, [FromQuery] string namespaceName = "default")
    {
        try
        {
            var resourceInfo = await _kubernetesJobService.GetJobResourceInfoAsync(jobName, namespaceName);
            if (resourceInfo == null)
                return NotFound($"Job '{jobName}' not found in namespace '{namespaceName}'");

            return resourceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource info for job {JobName} from namespace {Namespace}", jobName, namespaceName);
            return StatusCode(500, "Failed to retrieve job resource info");
        }
    }

    [HttpGet("pods")]
    public async Task<ActionResult<List<KubernetesPodInfo>>> GetAllPods([FromQuery] string namespaceName = "default")
    {
        try
        {
            var pods = await _kubernetesPodService.GetAllPodsAsync(namespaceName);
            return await _kubernetesPodService.ConvertToKubernetesPodInfoAsync(pods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pods from namespace {Namespace}", namespaceName);
            return StatusCode(500, "Failed to retrieve pods");
        }
    }

    [HttpGet("pods/{podName}")]
    public async Task<ActionResult<KubernetesPodInfo>> GetPod(string podName, [FromQuery] string namespaceName = "default")
    {
        try
        {
            var pod = await _kubernetesPodService.GetPodAsync(podName, namespaceName);
            if (pod == null)
                return NotFound($"Pod '{podName}' not found in namespace '{namespaceName}'");

            return await _kubernetesPodService.ConvertToKubernetesPodInfoAsync(pod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pod {PodName} from namespace {Namespace}", podName, namespaceName);
            return StatusCode(500, "Failed to retrieve pod");
        }
    }

    [HttpGet("pods/{podName}/logs")]
    public async Task<ActionResult<string>> GetPodLogs(
        string podName, 
        [FromQuery] string namespaceName = "default",
        [FromQuery] string? containerName = null)
    {
        try
        {
            var logs = await _kubernetesPodService.GetPodLogsAsync(podName, namespaceName, containerName);
            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for pod {PodName} from namespace {Namespace}", podName, namespaceName);
            return StatusCode(500, "Failed to retrieve pod logs");
        }
    }

    [HttpDelete("jobs/{jobName}")]
    public async Task<IActionResult> DeleteKubernetesJob(string jobName, [FromQuery] string namespaceName = "default")
    {
        try
        {
            await _kubernetesJobService.DeleteJobAsync(jobName, namespaceName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Kubernetes job {JobName} from namespace {Namespace}", jobName, namespaceName);
            return StatusCode(500, "Failed to delete Kubernetes job");
        }
    }
}