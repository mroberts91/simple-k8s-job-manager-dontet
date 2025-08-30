using Microsoft.EntityFrameworkCore;
using Quartz;
using CrystalQuartz.AspNetCore;
using K8sJobManager.Data;
using K8sJobManager.Services;
using K8sJobManager.Jobs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseInMemoryDatabase("JobQueue"));

builder.Services.AddScoped<IJobQueueService, JobQueueService>();
builder.Services.AddSingleton<IKubernetesClientService, KubernetesClientService>();
builder.Services.AddScoped<IKubernetesJobService, KubernetesJobService>();
builder.Services.AddScoped<IKubernetesPodService, KubernetesPodService>();

builder.Services.AddQuartz(q =>
{
    var jobLauncherKey = new JobKey("JobLauncher");
    q.AddJob<JobLauncherJob>(opts => opts.WithIdentity(jobLauncherKey));
    q.AddTrigger(opts => opts
        .ForJob(jobLauncherKey)
        .WithIdentity("JobLauncher-trigger")
        .WithCronSchedule("0/30 * * * * ?"));

    var statusMonitorKey = new JobKey("JobStatusMonitor");
    q.AddJob<JobStatusMonitorJob>(opts => opts.WithIdentity(statusMonitorKey));
    q.AddTrigger(opts => opts
        .ForJob(statusMonitorKey)
        .WithIdentity("JobStatusMonitor-trigger")
        .WithCronSchedule("0/15 * * * * ?"));

    var cleanupKey = new JobKey("JobCleanup");
    q.AddJob<JobCleanupJob>(opts => opts.WithIdentity(cleanupKey));
    q.AddTrigger(opts => opts
        .ForJob(cleanupKey)
        .WithIdentity("JobCleanup-trigger")
        .WithCronSchedule("0 0/5 * * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Register IScheduler for CrystalQuartz
builder.Services.AddSingleton<IScheduler>(serviceProvider =>
{
    var schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
    return schedulerFactory.GetScheduler().Result;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<JobDbContext>();
    context.Database.EnsureCreated();
}

app.MapOpenApi();
app.MapScalarApiReference("/api-reference");

app.UseHttpsRedirection();
app.MapControllers();

app.UseCrystalQuartz(() => app.Services.GetRequiredService<IScheduler>());

app.Run();
