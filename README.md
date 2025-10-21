# Library Management System

> **License Notice**
> This project includes many of my personal code snippets, templates, and tools, so I’m publishing it under a **copyleft** license.
> It is licensed under the **GNU General Public License v3.0 (GPL-3.0)** — a strong copyleft license.
> You are free to use, study, modify, and distribute this software, but any derivative work must also be released under the same license.
> See the [License](#license) section below for more details.

## Jump to: [Quick Start with the project](#quick-start-with-the-project)

<video width="640" height="480" controls>
  <source type="video/mp4" src="https://desmati.com/blog/production-grade-api-system-with-dotnet-grpc-cqrs-microservices-architecture/quick.mp4">
</video>

[Watch a quick start video here](https://desmati.com/blog/production-grade-api-system-with-dotnet-grpc-cqrs-microservices-architecture/quick.mp4)

## Purpose

A while ago, I was asked to implement an API for a library as part of an interview assignment. However, the task evolved into something much bigger and far more interesting — a **production-grade Library Management System** showcasing modern .NET architecture, distributed system design, and enterprise-level observability.

I decided to turn it into an educational project and share it.
You can read more about it in my detailed blog post:
[A Production-Grade API System with .NET, gRPC, CQRS, and Microservices Architecture](https://desmati.com/blog/production-grade-api-system-with-dotnet-grpc-cqrs-microservices-architecture)



## Architecture Overview

This project implements a complete Library Management System using **Domain-Driven Design (DDD)**, **Command Query Responsibility Segregation (CQRS)**, and **microservices** patterns in **.NET 9**, built on **Aspire**.

> **Aspire** provides tools, templates, and packages for building observable, production-ready distributed applications.
> It enables a code-first app model, unified local development tooling, and seamless deployment to any environment — cloud, Kubernetes, or on-premises.
> Learn more at [Microsoft Learn: .NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
 
**Port Reference:**
- Docker Compose: API on **8080**, gRPC on **5001**
- Development (dotnet run): API on **5032**, gRPC on **5150**
- Aspire: Ports assigned dynamically (view in Aspire Dashboard)

```
┌─────────────────────────────────────────────────────────────────┐
│                        HTTP Clients                             │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP/REST
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│             Library.Api (HTTP API Gateway)                      │
│         Docker: 8080 | Dev: 5032 | Aspire: Dynamic             │
│  • Minimal APIs with OpenAPI/Swagger                            │
│  • Request validation & mapping (Mapster)                       │
│  • Translates HTTP to gRPC calls                                │
└────────────────────────┬────────────────────────────────────────┘
                         │ gRPC (Protocol Buffers)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│             Library.Grpc (gRPC Business Service)                │
│         Docker: 5001 | Dev: 5150 | Aspire: Dynamic             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  gRPC Service Implementations                             │  │
│  │  • InventoryService  • CirculationService                 │  │
│  │  • UserActivityService                                    │  │
│  └─────────────────────┬─────────────────────────────────────┘  │
│                        │ Uses MediatR                            │
│                        ▼                                         │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │         Application Layer (CQRS)                          │  │
│  │  • Commands (Write): BorrowBook, ReturnBook               │  │
│  │  • Queries (Read): MostBorrowed, TopBorrowers, etc.       │  │
│  │  • MediatR Handlers & Pipeline Behaviors                  │  │
│  │  • FluentValidation                                       │  │
│  └─────────────────────┬─────────────────────────────────────┘  │
│                        │ Uses                                    │
│                        ▼                                         │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │         Domain Layer (Business Logic)                     │  │
│  │  • Entities: Book, User, Loan                             │  │
│  │  • Value Objects: ReadingPaceResult                       │  │
│  │  • Domain Policies: ReadingPacePolicy                     │  │
│  │  • Repository Interfaces (IBookRepository, etc.)          │  │
│  └─────────────────────▲─────────────────────────────────────┘  │
│                        │ Implements                              │
│                        │                                         │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │         Infrastructure Layer (Data Access)                │  │
│  │  • Repository Implementations                             │  │
│  │  • EF Core DbContext                                      │  │
│  │  • Query Services (optimized reads)                       │  │
│  └─────────────────────┬─────────────────────────────────────┘  │
└────────────────────────┼─────────────────────────────────────────┘
                         │ Reads/Writes
                         ▼
                  ┌──────────────┐
                  │  PostgreSQL  │
                  │  Port: 5432  │
                  └──────┬───────┘
                         ▲
                         │ Schema Setup
                         │
            ┌────────────┴──────────────┐
            │  Library.MigrationService │
            │  • EF Core Migrations     │
            │  • Data Seeding           │
            │  • Runs before app starts │
            └───────────────────────────┘

Observability Stack (External Services):
┌──────────────────┐ ┌──────────────────┐ ┌─────────────────┐
│      Seq         │ │     Jaeger       │ │    PgAdmin      │
│  Port: 8081/5341 │ │  Port: 16686     │ │ (Aspire only)   │
│  (Logging)       │ │  (Tracing)       │ │ (DB Management) │
└──────────────────┘ └──────────────────┘ └─────────────────┘
```

## Features

### Core Functionality
- **Inventory Management**: Track and analyze book borrowing patterns
  - Get most borrowed books within a time range
  - Find books frequently borrowed together
- **Circulation**: Handle book lending operations
  - Borrow books with loan tracking
  - Return books with automatic validation
- **User Activity Analytics**: Analyze user behavior
  - Identify top borrowers
  - Calculate reading pace per user

### Architecture Highlights
- **Domain-Driven Design**: Rich domain model with entities, value objects, and domain policies
- **CQRS Pattern**: Separate read and write models using MediatR
- **gRPC Communication**: High-performance internal service communication
- **HTTP API Gateway**: RESTful interface with OpenAPI documentation
- **Repository Pattern**: Clean data access abstraction
- **Validation**: FluentValidation for comprehensive input validation
- **Caching**: In-memory caching for query optimization

### Observability
- **Structured Logging**: Serilog with Seq for centralized log aggregation
- **Distributed Tracing**: OpenTelemetry with Jaeger for request tracing
- **Health Checks**: Built-in health endpoints for monitoring
- **Metrics**: Performance and business metrics collection

### Quality Assurance
- **Unit Tests**: Comprehensive domain and application layer tests
- **Integration Tests**: Infrastructure layer testing with real database
- **Functional Tests**: gRPC service contract testing
- **System Tests**: End-to-end API testing

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containerized deployment)
- [Git](https://git-scm.com/downloads)

Optional tools:
- [grpcurl](https://github.com/fullstorydev/grpcurl) - for testing gRPC endpoints
- [curl](https://curl.se/) - for testing HTTP endpoints

## Quick Start with the project

The recommended way to run the project is using .NET Aspire, which provides orchestration and observability out of the box.

### 1. Run the Aspire AppHost

```bash
dotnet run --project apphost/Library.AppHost
```

Or open the project using an updated version of Visual Studio 2022, run the project, wait for the dashboard to load, wait for all the services to display "Running" state in the dashboard, and in the end you can navigate to the API using provided URL in the dashboard.

### 2. Access the Aspire Dashboard

The dashboard URL will be displayed in the console output when the AppHost starts. Typically it's available at a port in the range **http://localhost:15000-18000** (the exact port is shown in the console).

The dashboard provides:
- Service status and health checks
- Automatic service discovery
- Centralized logging and tracing
- Resource management (PostgreSQL, Seq, Jaeger)
- PgAdmin for database management

### 3. Automatic Database Setup

When running via Aspire, the **Library.MigrationService** automatically:
- Creates the database schema using Entity Framework migrations
- Seeds sample data for demonstration purposes
- Ensures the database is ready before the gRPC service starts

### 4. Access Services

When running via Aspire, service ports are assigned dynamically. Check the Aspire Dashboard for actual URLs.

**Typical Aspire Service URLs:**
- **API Gateway (Swagger)**: Check Aspire Dashboard for the assigned port
- **gRPC Service**: Check Aspire Dashboard for the assigned port
- **Seq Logs**: http://localhost:5341 or via Aspire Dashboard
- **Jaeger Tracing**: http://localhost:16686 or via Aspire Dashboard
- **PostgreSQL**: Connection managed by Aspire (check dashboard for details)
- **PgAdmin**: Available via Aspire Dashboard

## Alternative: Docker Compose Deployment

For CI/CD pipelines, demos, or production-like environments, use Docker Compose.

### 1. Navigate to Deploy Directory

```bash
cd deploy
```

### 2. Start All Services

```bash
docker-compose up -d
```

### 3. Access Services

- **API Gateway (Swagger)**: http://localhost:8080/swagger
- **API Health Check**: http://localhost:8080/health
- **gRPC Service**: http://localhost:5001
- **gRPC Health Check**: http://localhost:5001/health
- **Seq Logs**: http://localhost:8081
- **Jaeger UI**: http://localhost:16686
- **PostgreSQL**: localhost:5432 (user: libuser, pass: h03e1n_devPass, db: libdb)

### 4. View Logs

```bash
docker-compose logs -f
```

### 5. Stop Services

```bash
docker-compose down
```

To remove volumes (including database data):

```bash
docker-compose down -v
```

## Development Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd LibraryAPI/cl
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Set Up Database

The database will be automatically created and migrated when running via Aspire or Docker Compose.

For manual setup:

```bash
# Update connection string in src/Library.Grpc/appsettings.json
dotnet ef database update --project src/Library.Infrastructure --startup-project src/Library.Grpc
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Run Tests

```bash
dotnet test
```

## Project Structure

```
LibraryAPI/cl/
├── apphost/
│   └── Library.AppHost/              # .NET Aspire orchestration host
├── src/
│   ├── Library.Domain/               # Domain layer (entities, repositories, policies)
│   ├── Library.Application/          # Application layer (CQRS, MediatR, validation)
│   ├── Library.Infrastructure/       # Infrastructure (EF Core, PostgreSQL)
│   ├── Library.Contracts/            # Protobuf contracts (gRPC)
│   ├── Library.Grpc/                 # gRPC service implementation
│   ├── Library.Api/                  # HTTP API Gateway (Minimal APIs)
│   ├── Library.MigrationService/     # Database migration and seeding service
│   ├── Library.ServiceDefaults/      # Shared service configuration
│   └── Library.Warmups/              # Coding warm-up exercises
├── tests/
│   ├── Library.UnitTests.Domain/     # Domain layer unit tests
│   ├── Library.UnitTests.Application/# Application layer unit tests
│   ├── Library.IntegrationTests.Infrastructure/ # Infrastructure integration tests
│   ├── Library.FunctionalTests.Grpc/ # gRPC functional tests
│   └── Library.SystemTests.Api/      # End-to-end API system tests
└── deploy/
    ├── docker-compose.yml            # Docker Compose orchestration
    ├── Dockerfile.api                # API Gateway Dockerfile
    ├── Dockerfile.grpc               # gRPC Service Dockerfile
    ├── .env.example                  # Environment variables template
    └── README.md                     # Docker deployment guide
```

## Testing

### Run All Tests

```bash
dotnet test
```

### Run with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Test Projects Overview

| Project | Type | Description |
|---------|------|-------------|
| **Library.UnitTests.Domain** | Unit | Tests domain entities, value objects, and policies |
| **Library.UnitTests.Application** | Unit | Tests CQRS handlers, validators, and application logic |
| **Library.IntegrationTests.Infrastructure** | Integration | Tests EF Core repositories with real PostgreSQL |
| **Library.FunctionalTests.Grpc** | Functional | Tests gRPC service contracts and behavior |
| **Library.SystemTests.Api** | System | End-to-end tests for HTTP API endpoints |

## API Endpoints

> **Note**: The examples below use Docker Compose ports (API: 8080, gRPC: 5001).
> For Aspire, check the dashboard for dynamically assigned ports.
> For development with `dotnet run`, use API: 5032 and gRPC: 5150.

### HTTP API (Docker Compose: Port 8080)

#### Inventory

**Get Most Borrowed Books**
```bash
curl -X GET "http://localhost:8080/inventory/most-borrowed?start=2024-01-01T00:00:00Z&end=2024-12-31T23:59:59Z&limit=10"
```

**Get Books Also Borrowed**
```bash
curl -X GET "http://localhost:8080/books/{bookId}/also-borrowed?start=2024-01-01T00:00:00Z&end=2024-12-31T23:59:59Z&limit=10"
```

#### Circulation

**Borrow a Book**
```bash
curl -X POST "http://localhost:8080/circulation/borrow" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "bookId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "borrowedAt": "2024-01-15T10:30:00Z"
  }'
```

**Return a Book**
```bash
curl -X POST "http://localhost:8080/circulation/return" \
  -H "Content-Type: application/json" \
  -d '{
    "loanId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "returnedAt": "2024-01-30T14:00:00Z"
  }'
```

#### User Activity

**Get Top Borrowers**
```bash
curl -X GET "http://localhost:8080/activity/top-borrowers?start=2024-01-01T00:00:00Z&end=2024-12-31T23:59:59Z&limit=10"
```

**Get User Reading Pace**
```bash
curl -X GET "http://localhost:8080/activity/users/{userId}/reading-pace?start=2024-01-01T00:00:00Z&end=2024-12-31T23:59:59Z"
```

### gRPC Endpoints (Docker Compose: Port 5001)

The gRPC service exposes reflection, allowing you to discover services using `grpcurl`:

**List Services**
```bash
grpcurl -plaintext localhost:5001 list
```

**List Methods**
```bash
grpcurl -plaintext localhost:5001 list library.contracts.inventory.v1.InventoryService
```

**Call GetMostBorrowedBooks**
```bash
grpcurl -plaintext -d '{
  "top": 10,
  "range": {
    "start": "2024-01-01T00:00:00Z",
    "end": "2024-12-31T23:59:59Z"
  }
}' localhost:5001 library.contracts.inventory.v1.InventoryService/GetMostBorrowedBooks
```

**Call BorrowBook**
```bash
grpcurl -plaintext -d '{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "bookId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "borrowedAt": "2024-01-15T10:30:00Z"
}' localhost:5001 library.contracts.circulation.v1.CirculationService/BorrowBook
```

## Observability

### Seq (Structured Logging)

**Aspire**: Access via Aspire dashboard or directly at http://localhost:5341
**Docker Compose**: http://localhost:8081

Features:
- Full-text search across all logs
- Filter by log level, timestamp, properties
- Query language for complex searches
- Dashboards and visualizations

Example query:
```
RequestPath like '/inventory%' and StatusCode >= 400
```

### Jaeger (Distributed Tracing)

**Aspire**: Access via Aspire dashboard or directly at http://localhost:16686
**Docker Compose**: http://localhost:16686

Features:
- End-to-end request tracing
- Service dependency mapping
- Performance bottleneck identification
- Error tracking across services

### PgAdmin (Database Management)

**Aspire**: Access via Aspire dashboard
**Docker Compose**: Not included in Docker Compose setup (use direct PostgreSQL connection)

Features:
- Visual database schema explorer
- Query editor and execution
- Table data viewer and editor
- Database administration tools

### Health Checks

**API Health** (Docker Compose port shown; adjust for your deployment)
```bash
curl http://localhost:8080/health
```

**gRPC Health** (Docker Compose port shown; adjust for your deployment)
```bash
curl http://localhost:5001/health
```

Response format:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    }
  }
}
```

## Warm-up Exercises

The project includes a console application with coding exercises:

### Run Exercises

```bash
dotnet run --project src/Library.Warmups
```

### Included Exercises

1. **IsPowerOfTwo**: Check if a number is a power of two using bit manipulation
2. **ReverseTitle**: Reverse a book title while preserving Unicode characters
3. **RepeatTitle**: Repeat a title N times efficiently
4. **OddIds0To100**: Generate all odd numbers from 1 to 99 using yield

These exercises demonstrate C# fundamentals, LINQ, and performance optimization techniques.

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| **Framework** | .NET | 9.0 |
| **API Gateway** | ASP.NET Core Minimal APIs | 9.0 |
| **gRPC** | Grpc.AspNetCore | Latest |
| **Database** | PostgreSQL | 16 |
| **ORM** | Entity Framework Core | 9.0 |
| **CQRS** | MediatR | Latest |
| **Validation** | FluentValidation | Latest |
| **Mapping** | Mapster | Latest |
| **Logging** | Serilog | Latest |
| **Tracing** | OpenTelemetry | Latest |
| **Log Aggregation** | Seq | Latest |
| **Trace Visualization** | Jaeger | Latest |
| **Orchestration** | .NET Aspire | 9.0 |
| **Containerization** | Docker | Latest |
| **API Documentation** | Swagger/OpenAPI | Latest |
| **Testing** | xUnit, FluentAssertions | Latest |

## Domain Model

### Entities

**Book**
- BookId (Guid)
- ISBN (string)
- Title (string)
- Author (string)
- PageCount (int)
- PublishedYear (int?)

**User**
- UserId (Guid)
- FullName (string)
- RegisteredAt (DateTime)

**Loan**
- LoanId (Guid)
- BookId (Guid)
- UserId (Guid)
- BorrowedAt (DateTime)
- ReturnedAt (DateTime?)

### Business Rules

- Books must have valid ISBN, title, author, and positive page count
- Users must have a full name and registration date
- Loans track borrowing and return dates
- A loan cannot be returned before it was borrowed
- A loan cannot be returned twice
- Reading pace is calculated as pages per day based on loan history

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See appsettings.json |
| `GrpcServices__LibraryGrpc` | gRPC service URL (API only) | Docker: http://localhost:5001<br/>Dev: http://localhost:5150 |
| `Serilog__MinimumLevel__Default` | Minimum log level | Information |
| `Serilog__WriteTo__1__Args__serverUrl` | Seq server URL | http://localhost:5341 |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry endpoint | http://localhost:4317 |
| `OTEL_SERVICE_NAME` | Service name for tracing | library-api / library-grpc |

### Aspire Configuration

Aspire automatically manages service configuration, including:
- Service discovery and URL binding
- PostgreSQL container and connection strings
- Seq container and ingestion endpoints
- Health check endpoints

### Docker Compose Configuration

Environment variables can be overridden in a `.env` file in the `deploy/` directory:

```env
POSTGRES_USER=libuser
POSTGRES_PASSWORD=h03e1n_devPass
POSTGRES_DB=libdb
POSTGRES_PORT=5432
API_PORT=8080
GRPC_PORT=5001
SEQ_UI_PORT=8081
SEQ_INGESTION_PORT=5341
JAEGER_UI_PORT=16686
LOG_LEVEL=Information
```

## Production Considerations

### Security
- [ ] Implement authentication and authorization (e.g., JWT, OAuth2)
- [ ] Enable HTTPS/TLS for all communications
- [ ] Use secrets management (Azure Key Vault, AWS Secrets Manager)
- [ ] Implement rate limiting and throttling
- [ ] Add input sanitization beyond validation
- [ ] Enable CORS with strict origin policies

### Performance
- [ ] Configure connection pooling for PostgreSQL
- [ ] Implement distributed caching (Redis)
- [ ] Add response compression
- [ ] Configure gRPC load balancing
- [ ] Optimize database indexes based on query patterns
- [ ] Implement pagination for large result sets

### Reliability
- [ ] Add circuit breakers (Polly)
- [ ] Implement retry policies with exponential backoff
- [ ] Set up health checks for all dependencies
- [ ] Configure proper timeout values
- [ ] Add dead letter queues for failed messages
- [ ] Implement graceful shutdown

### Observability
- [ ] Set up alerting rules (Prometheus, Grafana)
- [ ] Configure log retention policies
- [ ] Add business metrics dashboards
- [ ] Implement SLA monitoring
- [ ] Add application performance monitoring (APM)
- [ ] Set up error tracking (Sentry)

### Deployment
- [ ] Set up CI/CD pipelines
- [ ] Implement blue-green or canary deployments
- [ ] Configure auto-scaling rules
- [ ] Set up database backup and recovery
- [ ] Implement disaster recovery plan
- [ ] Add infrastructure as code (Terraform, Bicep)

## Troubleshooting

### Database Connection Issues

**Problem**: Cannot connect to PostgreSQL

**Solution**:
- Check that PostgreSQL container is running: `docker ps`
- Verify connection string in appsettings.json
- Ensure port 5432 is not already in use
- Check firewall settings

### gRPC Communication Errors

**Problem**: API Gateway cannot reach gRPC service

**Solution**:
- Verify gRPC service is running on port 5001
- Check that HTTP/2 is enabled (required for gRPC)
- Ensure gRPC service URL is correctly configured
- Review gRPC service logs for errors

### Aspire Dashboard Not Accessible

**Problem**: Cannot access Aspire dashboard

**Solution**:
- Ensure AppHost is running without errors (`dotnet run --project apphost/Library.AppHost`)
- Check the console output for the actual dashboard URL (port varies)
- Verify the port shown in console is not blocked by firewall
- Try clearing browser cache
- Review AppHost console output for errors or exceptions

### Docker Compose Services Not Starting

**Problem**: Services fail to start or are unhealthy

**Solution**:
- Check logs: `docker-compose logs <service-name>`
- Verify all required ports are available
- Ensure Docker Desktop has sufficient resources
- Try rebuilding images: `docker-compose build --no-cache`
- Remove volumes and restart: `docker-compose down -v && docker-compose up -d`

### Tests Failing

**Problem**: Tests fail during execution

**Solution**:
- Ensure PostgreSQL is running (for integration tests)
- Check test database connection strings
- Verify all dependencies are restored: `dotnet restore`
- Run tests with verbose logging: `dotnet test --logger "console;verbosity=detailed"`

### Port Already in Use

**Problem**: Cannot start service, port already allocated

**Solution**:
```bash
# Windows
netstat -ano | findstr :<port>
taskkill /PID <process-id> /F

# Linux/Mac
lsof -i :<port>
kill -9 <process-id>
```

## License

**GNU General Public License v3.0 (GPL-3.0)**

Copyright (c) 2025 Hossein Esmati (desmati@gmail.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.

**Important Notice**: This is a copyleft license. Any derivative work must also be distributed under the same GPL-3.0 license. You are free to use, modify, and distribute this software, but any modifications or derivative works must remain open source under GPL-3.0.

For the full license text, visit: https://www.gnu.org/licenses/gpl-3.0.en.html
