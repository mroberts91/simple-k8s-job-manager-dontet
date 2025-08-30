using Microsoft.EntityFrameworkCore;
using K8sJobManager.Data;
using K8sJobManager.Models;

namespace K8sJobManager.Services;

public class JobQueueService : IJobQueueService
{
    private readonly JobDbContext _context;

    public JobQueueService(JobDbContext context)
    {
        _context = context;
    }

    public async Task<JobRequest> CreateJobAsync(JobRequest jobRequest)
    {
        _context.JobRequests.Add(jobRequest);
        await _context.SaveChangesAsync();
        return jobRequest;
    }

    public async Task<JobRequest?> GetJobAsync(Guid id)
    {
        return await _context.JobRequests.FindAsync(id);
    }

    public async Task<List<JobRequest>> GetAllJobsAsync()
    {
        return await _context.JobRequests.OrderByDescending(j => j.CreatedAt).ToListAsync();
    }

    public async Task<List<JobRequest>> GetJobsByStatusAsync(JobStatus status)
    {
        return await _context.JobRequests
            .Where(j => j.Status == status)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<JobRequest> UpdateJobStatusAsync(Guid id, JobStatus status, string? errorMessage = null)
    {
        var job = await _context.JobRequests.FindAsync(id);
        if (job == null)
            throw new ArgumentException($"Job with ID {id} not found");

        job.Status = status;
        if (errorMessage != null)
            job.ErrorMessage = errorMessage;

        switch (status)
        {
            case JobStatus.Running:
                job.StartedAt = DateTime.UtcNow;
                break;
            case JobStatus.Completed:
            case JobStatus.Failed:
            case JobStatus.Cancelled:
                job.CompletedAt = DateTime.UtcNow;
                break;
        }

        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<JobRequest> UpdateJobKubernetesNameAsync(Guid id, string kubernetesJobName)
    {
        var job = await _context.JobRequests.FindAsync(id);
        if (job == null)
            throw new ArgumentException($"Job with ID {id} not found");

        job.KubernetesJobName = kubernetesJobName;
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<List<JobRequest>> GetQueuedJobsAsync()
    {
        return await GetJobsByStatusAsync(JobStatus.Queued);
    }

    public async Task<List<JobRequest>> GetRunningJobsAsync()
    {
        return await GetJobsByStatusAsync(JobStatus.Running);
    }

    public async Task<List<JobRequest>> GetStaleJobsAsync(TimeSpan staleThreshold)
    {
        var cutoffTime = DateTime.UtcNow - staleThreshold;
        return await _context.JobRequests
            .Where(j => j.Status == JobStatus.Running && j.StartedAt < cutoffTime)
            .ToListAsync();
    }

    public async Task<JobRequest> UpdateJobWithKubernetesInfoAsync(Guid id, KubernetesResourceInfo kubernetesInfo)
    {
        var job = await _context.JobRequests.FindAsync(id);
        if (job == null)
            throw new ArgumentException($"Job with ID {id} not found");

        job.KubernetesInfo = kubernetesInfo;
        await _context.SaveChangesAsync();
        return job;
    }
}