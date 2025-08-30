# Kubernetes Job Manager

A proof-of-concept ASP.NET Core application that allows users to launch Kubernetes Job resources through a queue system powered by Quartz.NET.

## Features

- **Job Queue Management**: Submit jobs to a queue that are processed asynchronously
- **Kubernetes Integration**: Automatically creates Kubernetes Jobs and ConfigMaps
- **Status Monitoring**: Tracks job status and updates accordingly
- **Automatic Cleanup**: Removes stale and orphaned Kubernetes resources
- **Web Dashboard**: CrystalQuartz dashboard for monitoring Quartz jobs
- **REST API**: Full REST API for job management

## Architecture

### Components

1. **Job Queue Service**: Manages job requests in an in-memory database
2. **Kubernetes Job Service**: Creates and manages Kubernetes Job resources
3. **Quartz Scheduled Jobs**:
   - **Job Launcher** (every 30 seconds): Processes queued jobs and creates Kubernetes resources
   - **Status Monitor** (every 15 seconds): Monitors running jobs and updates their status
   - **Cleanup Job** (every 5 minutes): Removes stale and orphaned Kubernetes resources

### Job Lifecycle

1. User submits a job via REST API
2. Job is stored in queue with `Queued` status
3. Job Launcher picks up queued jobs and creates Kubernetes Jobs + ConfigMaps
4. Job status is updated to `Running`
5. Status Monitor checks job completion and updates status to `Completed` or `Failed`
6. Cleanup Job removes old completed/failed Kubernetes resources

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Kubernetes cluster access (kubectl configured)
- Docker (for container images)

### Running the Application

1. Clone the repository
2. Navigate to the project directory:
   ```bash
   cd src/web
   ```
3. Build and run the application:
   ```bash
   dotnet run
   ```
4. The application will start on `http://localhost:5000`

### API Endpoints

- `POST /api/jobs` - Submit a new job
- `GET /api/jobs` - Get all jobs
- `GET /api/jobs?status={status}` - Get jobs by status
- `GET /api/jobs/{id}` - Get specific job
- `DELETE /api/jobs/{id}` - Cancel a job

### Monitoring

- **CrystalQuartz Dashboard**: Available at `http://localhost:5000/quartz`
- **OpenAPI/Swagger**: Available at `http://localhost:5000/openapi/v1.json` (in development)

## Example Usage

Submit a job:
```bash
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "hello-world-job",
    "containerImage": "busybox:latest",
    "command": ["sh"],
    "args": ["-c", "echo Hello from Kubernetes && sleep 30"],
    "configuration": {
      "LOG_LEVEL": "INFO"
    },
    "labels": {
      "app": "test"
    }
  }'
```

## Configuration

The application uses the following Kubernetes configuration approach:
- **In-cluster**: Uses service account when running inside Kubernetes
- **Local development**: Uses kubectl configuration from `~/.kube/config`

## Job Specifications

Each job creates:
1. **Kubernetes Job**: Runs the specified container with command/args
2. **ConfigMap**: Contains the configuration data mounted at `/config`

Default job settings:
- Namespace: `default`
- Restart Policy: `Never`
- Backoff Limit: `3`
- TTL After Finished: `300` seconds (5 minutes)

## Status Types

- `Queued`: Job submitted and waiting to be processed
- `Running`: Kubernetes Job has been created and is executing
- `Completed`: Job finished successfully
- `Failed`: Job failed to complete
- `Cancelled`: Job was cancelled by user
- `Orphaned`: Kubernetes Job was deleted externally but still tracked

## Development

### Project Structure

```
src/web/
├── Controllers/        # REST API controllers
├── Data/              # Entity Framework DbContext
├── Jobs/              # Quartz.NET job implementations
├── Models/            # Data models and DTOs
├── Services/          # Business logic services
└── Program.cs         # Application startup configuration
```

### Extending the Application

To add new functionality:

1. **New job types**: Extend `JobRequest` model and update `KubernetesJobService`
2. **Additional monitoring**: Create new Quartz jobs in the `Jobs/` directory
3. **Different storage**: Replace `JobDbContext` with persistent storage
4. **Authentication**: Add authentication middleware and update controllers

## Troubleshooting

### Common Issues

1. **Kubernetes connection errors**: Ensure kubectl is configured correctly
2. **Job creation failures**: Check Kubernetes cluster resources and permissions
3. **Quartz jobs not running**: Check application logs for scheduling errors

### Logs

The application uses structured logging. Check console output for:
- Job queue processing
- Kubernetes resource creation/deletion
- Quartz job execution status