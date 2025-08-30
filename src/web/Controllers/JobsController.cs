using Microsoft.AspNetCore.Mvc;
using K8sJobManager.Models;
using K8sJobManager.Services;

namespace K8sJobManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobQueueService _jobQueueService;
    private readonly IKubernetesJobService _kubernetesJobService;

    public JobsController(IJobQueueService jobQueueService, IKubernetesJobService kubernetesJobService)
    {
        _jobQueueService = jobQueueService;
        _kubernetesJobService = kubernetesJobService;
    }

    [HttpPost]
    public async Task<ActionResult<JobRequest>> CreateJob([FromBody] CreateJobRequest request)
    {
        var jobRequest = new JobRequest
        {
            Name = request.Name,
            ContainerImage = request.ContainerImage,
            Configuration = request.Configuration,
            Labels = request.Labels,
            Command = request.Command,
            Args = request.Args
        };

        var createdJob = await _jobQueueService.CreateJobAsync(jobRequest);
        return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, createdJob);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobRequest>> GetJob(Guid id)
    {
        var job = await _jobQueueService.GetJobAsync(id);
        if (job == null)
            return NotFound();

        return job;
    }

    [HttpGet]
    public async Task<ActionResult<List<JobRequest>>> GetAllJobs([FromQuery] JobStatus? status = null)
    {
        if (status.HasValue)
            return await _jobQueueService.GetJobsByStatusAsync(status.Value);

        return await _jobQueueService.GetAllJobsAsync();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelJob(Guid id)
    {
        try
        {
            await _jobQueueService.UpdateJobStatusAsync(id, JobStatus.Cancelled);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/refresh-kubernetes-info")]
    public async Task<ActionResult<JobRequest>> RefreshKubernetesInfo(Guid id)
    {
        try
        {
            var job = await _jobQueueService.GetJobAsync(id);
            if (job == null)
                return NotFound();

            if (string.IsNullOrEmpty(job.KubernetesJobName))
                return BadRequest("Job does not have an associated Kubernetes job");

            var kubernetesInfo = await _kubernetesJobService.GetJobResourceInfoAsync(job.KubernetesJobName);
            if (kubernetesInfo != null)
            {
                await _jobQueueService.UpdateJobWithKubernetesInfoAsync(id, kubernetesInfo);
            }

            return await _jobQueueService.GetJobAsync(id) ?? job;
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, "Failed to refresh Kubernetes info");
        }
    }
}

public class CreateJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string ContainerImage { get; set; } = string.Empty;
    public Dictionary<string, string> Configuration { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<string> Command { get; set; } = new();
    public List<string> Args { get; set; } = new();
}