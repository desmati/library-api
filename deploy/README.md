# Library API Docker Deployment

This directory contains Docker deployment files for the Library API microservices application.

## Contents

- **Dockerfile.api** - Multi-stage Dockerfile for the Library.Api HTTP gateway
- **Dockerfile.grpc** - Multi-stage Dockerfile for the Library.Grpc service
- **docker-compose.yml** - Orchestration file for all services
- **.env.example** - Example environment variables (copy to `.env`)

## Architecture

The deployment consists of 5 services:

1. **postgres** - PostgreSQL 16 database with persistent storage
2. **seq** - Centralized logging server (Seq)
3. **jaeger** - Distributed tracing (Jaeger all-in-one)
4. **library-grpc** - Core gRPC service (handles business logic, database access)
5. **library-api** - HTTP REST API gateway (proxies requests to gRPC service)

## Quick Start

### Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+
- Minimum 2GB RAM available
- Ports 5001, 8080, 5432, 8081, 16686, 4317 available

### Setup

1. **Navigate to the deployment directory:**
   ```bash
   cd deploy
   ```

2. **Create environment file:**
   ```bash
   cp .env.example .env
   ```

3. **Edit `.env` file and update credentials** (especially for production):
   ```bash
   nano .env  # or your preferred editor
   ```

4. **Start all services:**
   ```bash
   docker-compose up -d
   ```

5. **Check service health:**
   ```bash
   docker-compose ps
   ```

6. **View logs:**
   ```bash
   # All services
   docker-compose logs -f

   # Specific service
   docker-compose logs -f library-api
   ```

## Access Points

Once deployed, access the services at:

| Service | URL | Description |
|---------|-----|-------------|
| Library API | http://localhost:8080 | REST API with Swagger UI |
| Library gRPC | http://localhost:5001 | gRPC service endpoints |
| Seq Logs | http://localhost:8081 | Centralized log viewer |
| Jaeger UI | http://localhost:16686 | Distributed tracing UI |
| PostgreSQL | localhost:5432 | Database (use credentials from .env) |

## Common Operations

### View API Documentation
```bash
# Open Swagger UI in browser
open http://localhost:8080
```

### Check Database
```bash
# Connect to PostgreSQL
docker exec -it library-postgres psql -U libuser -d libdb

# Run SQL queries
\dt  # List tables
SELECT * FROM "Books" LIMIT 10;
\q   # Quit
```

### View Logs in Seq
```bash
# Open Seq in browser
open http://localhost:8081
```

### View Traces in Jaeger
```bash
# Open Jaeger UI in browser
open http://localhost:16686
```

### Rebuild Services
```bash
# Rebuild a specific service
docker-compose build library-api

# Rebuild and restart
docker-compose up -d --build library-api
```

### Stop Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: deletes data!)
docker-compose down -v
```

### Scale Services (if needed)
```bash
# Scale API instances
docker-compose up -d --scale library-api=3
```

## Health Checks

All services implement health checks:

```bash
# Check API health
curl http://localhost:8080/health

# Check gRPC health
curl http://localhost:5001/health

# Check all service health status
docker-compose ps
```

## Database Migrations

Database migrations run automatically when the `library-grpc` service starts. This is handled in the application's `Program.cs` file.

To manually trigger migrations:
```bash
docker-compose restart library-grpc
docker-compose logs -f library-grpc
```

## Troubleshooting

### Services won't start
```bash
# Check logs
docker-compose logs

# Check specific service
docker-compose logs library-grpc

# Verify ports are available
netstat -an | grep -E "5001|8080|5432|8081|16686"
```

### Database connection issues
```bash
# Verify PostgreSQL is running
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Test connection
docker exec -it library-postgres pg_isready -U libuser
```

### gRPC connection issues
```bash
# Verify gRPC service is running
docker-compose ps library-grpc

# Check gRPC logs
docker-compose logs library-grpc

# Test gRPC health endpoint
curl http://localhost:5001/health
```

### Reset everything
```bash
# Stop and remove all containers, networks, and volumes
docker-compose down -v

# Remove images
docker-compose down --rmi all -v

# Start fresh
docker-compose up -d --build
```

## Production Considerations

### Security
- [ ] Change all default passwords in `.env`
- [ ] Use Docker secrets or external secret management
- [ ] Enable TLS/SSL for all external endpoints
- [ ] Implement rate limiting
- [ ] Use reverse proxy (nginx, traefik) for API gateway
- [ ] Scan images for vulnerabilities
- [ ] Run containers as non-root users (already configured)
- [ ] Implement network policies

### Performance
- [ ] Set resource limits in docker-compose.yml
- [ ] Configure connection pooling
- [ ] Enable response caching
- [ ] Use read replicas for database if needed
- [ ] Monitor resource usage

### Reliability
- [ ] Implement backup strategy for PostgreSQL volume
- [ ] Configure log rotation for Seq
- [ ] Set up monitoring and alerting
- [ ] Configure restart policies (already set to `unless-stopped`)
- [ ] Implement circuit breakers
- [ ] Set up automated health checks

### Observability
- [ ] Configure Seq retention policies
- [ ] Set up Jaeger storage backend (currently in-memory)
- [ ] Implement application metrics (Prometheus)
- [ ] Create dashboards (Grafana)
- [ ] Set up alerts for critical errors

### Deployment
- [ ] Use specific image tags (not `latest`)
- [ ] Implement CI/CD pipeline
- [ ] Use infrastructure as code (Terraform, Pulumi)
- [ ] Implement blue-green or canary deployments
- [ ] Test disaster recovery procedures

## Monitoring

### Check Resource Usage
```bash
# Container stats
docker stats

# Disk usage
docker system df

# Volume usage
docker volume ls
```

### View Metrics
```bash
# Container resource usage
docker-compose top

# Specific service
docker-compose top library-api
```

## Backup and Restore

### Backup PostgreSQL
```bash
# Create backup
docker exec library-postgres pg_dump -U libuser libdb > backup_$(date +%Y%m%d_%H%M%S).sql

# Or with docker-compose
docker-compose exec postgres pg_dump -U libuser libdb > backup.sql
```

### Restore PostgreSQL
```bash
# Restore from backup
cat backup.sql | docker exec -i library-postgres psql -U libuser -d libdb
```

### Backup Volumes
```bash
# Stop services
docker-compose down

# Backup volume
docker run --rm -v library-postgres-data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz /data

# Restart services
docker-compose up -d
```

## Support

For issues and questions:
- Check logs: `docker-compose logs`
- Review health checks: `docker-compose ps`
- Consult application documentation
- Check Docker and Docker Compose versions

## License

**GNU General Public License v3.0 (GPL-3.0)**

Copyright (c) 2025 Hossein Esmati (desmati@gmail.com)

This program is free software licensed under GPL-3.0. Any derivative work must also be distributed under the same GPL-3.0 license.

See the main project README.md file for full license details.
