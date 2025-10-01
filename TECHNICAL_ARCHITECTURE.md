# Technical Architecture

## System Overview

The GTFS Feed Validator is a web-based service designed to validate GTFS (General Transit Feed Specification) feeds using Google's official GTFS validator tool. The architecture follows a modern microservice pattern with asynchronous processing capabilities.

## High-Level Architecture

```
???????????????????    ????????????????????    ???????????????????
?                 ?    ?                  ?    ?                 ?
?   Client Apps   ??????   Web API        ??????  Background     ?
?                 ?    ?   Layer          ?    ?  Worker         ?
???????????????????    ????????????????????    ???????????????????
                                ?                        ?
                                ?                        ?
                       ????????????????????    ???????????????????
                       ?                  ?    ?                 ?
                       ?   LiteDB         ?    ?  File System    ?
                       ?   Database       ?    ?  (Temp Storage) ?
                       ?                  ?    ?                 ?
                       ????????????????????    ???????????????????
                                                         ?
                                                         ?
                                                ???????????????????
                                                ?                 ?
                                                ?  GTFS Validator ?
                                                ?  (Java JAR)     ?
                                                ?                 ?
                                                ???????????????????
```

## Core Components

### 1. Web API Layer

**Purpose**: Handles HTTP requests and provides RESTful endpoints for GTFS validation.

**Key Components**:
- `Program.cs`: Application bootstrap and dependency injection configuration
- `Endpoints.cs`: Route definitions and endpoint handlers
- `ExceptionHandlingMiddleware.cs`: Global exception handling

**Responsibilities**:
- File upload handling
- Request validation
- Response formatting
- API documentation (Swagger/OpenAPI)
- HTTPS redirection and security

### 2. Service Layer

**Purpose**: Contains business logic and orchestrates validation operations.

**Key Components**:

#### IGtfsFeedValidatorService / GtfsFeedValidatorService
- **StartValidationAsync()**: Handles file uploads and queues validation requests
- **GetJsonValidationResult()**: Retrieves JSON validation results
- **GetHtmlValidationResult()**: Retrieves HTML validation reports
- **GetApiStatus()**: Provides system status information

#### IGtfsFeedValidatorWorkerService / GtfsFeedValidatorWorkerService
- **ProcessQueueAsync()**: Background processing of validation queue
- Java process execution and management
- Result collection and storage
- Cleanup operations

### 3. Background Processing

**Purpose**: Handles long-running validation tasks asynchronously.

**Key Components**:
- `GtfsValidatorWorker`: Hosted service implementing `BackgroundService`
- Continuous polling for pending validations
- Process lifecycle management
- Error handling and retry logic

### 4. Data Layer

**Purpose**: Manages persistent storage of validation requests and results.

**Database Technology**: LiteDB (NoSQL document database)

**Collections**:

#### GtfsFeedValidation
```csharp
{
    Id: string,                    // Unique validation identifier
    FilePath: string,             // Path to uploaded GTFS file
    Status: string,               // Processing status
    InsertDatetime: DateTime,     // Request creation time
    StartElaboration: DateTime?,  // Processing start time
    EndElaboration: DateTime?     // Processing end time
}
```

#### GtfsFeedValidationResult
```csharp
{
    Id: string,                     // Unique result identifier
    InsertDatetime: DateTime,       // Result creation time
    GtfsFeedValidationId: string,   // Reference to validation request
    JsonValidationResult: string,   // JSON validation output
    HtmlValidationResult: string    // HTML validation report
}
```

### 5. External Dependencies

#### GTFS Validator JAR
- **Technology**: Java-based validation engine from Google/MobilityData
- **Version**: 5.0.1 (configurable)
- **Execution**: Command-line interface via Process.Start()
- **Input**: GTFS ZIP files
- **Output**: JSON report and HTML report

## Data Flow

### 1. Validation Request Flow

```
Client Upload ? API Endpoint ? Service Layer ? Database ? Queue ? Worker
```

1. **Client uploads GTFS file** via POST `/start-validation`
2. **API validates request** and accepts multipart form data
3. **Service saves file** to temporary storage
4. **Database record created** with "Pending" status
5. **Unique ID returned** to client
6. **Background worker picks up** pending validation

### 2. Processing Flow

```
Worker ? File System ? Java Process ? GTFS Validator ? Results ? Database
```

1. **Worker polls** for pending validations
2. **File extracted** to working directory
3. **Java process spawned** with validator JAR
4. **Validation executed** against GTFS data
5. **Results collected** (JSON and HTML)
6. **Database updated** with results and "Completed" status
7. **Cleanup performed** (files and old records)

### 3. Result Retrieval Flow

```
Client Request ? API Endpoint ? Service Layer ? Database ? Response
```

1. **Client polls** for results via GET `/validation-result/{id}`
2. **Service queries** database for validation status
3. **Results mapped** to response models (if available)
4. **Response returned** with appropriate HTTP status

## Technology Stack

### Runtime and Frameworks
- **.NET 8**: Target framework
- **ASP.NET Core**: Web framework
- **Minimal APIs**: Lightweight endpoint definition
- **Hosted Services**: Background processing

### Data and Persistence
- **LiteDB 5.0.21**: Embedded NoSQL database
- **File System**: Temporary file storage
- **JSON Serialization**: System.Text.Json

### Object Mapping and Configuration
- **AutoMapper 13.0.1**: Object-object mapping
- **Options Pattern**: Configuration management
- **User Secrets**: Development configuration

### Documentation and Testing
- **Swashbuckle.AspNetCore 6.7.0**: OpenAPI/Swagger
- **MSTest**: Unit testing framework
- **FluentAssertions**: Assertion library
- **AspNetCore.Mvc.Testing**: Integration testing

### External Tools
- **Java Runtime Environment**: Required for GTFS validator
- **Google GTFS Validator JAR**: Core validation engine

## Design Patterns and Principles

### 1. Dependency Injection (DI)

All services are registered in the DI container:
```csharp
builder.Services.AddScoped<IGtfsFeedValidatorService, GtfsFeedValidatorService>();
builder.Services.AddSingleton<IGtfsFeedValidatorWorkerService, GtfsFeedValidatorWorkerService>();
```

**Benefits**:
- Loose coupling between components
- Easier unit testing with mocks
- Configuration-based service resolution

### 2. Repository Pattern (Implicit)

Data access is abstracted through service interfaces:
- Database operations encapsulated in services
- Business logic separated from data access
- Easy to mock for testing

### 3. Background Service Pattern

Long-running tasks handled asynchronously:
```csharp
public class GtfsValidatorWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuous processing loop
    }
}
```

**Benefits**:
- Non-blocking API responses
- Scalable processing
- Fault tolerance and retry capabilities

### 4. Options Pattern

Configuration management using strongly-typed options:
```csharp
builder.Services.AddOptions<GtfsValidatorSettings>()
    .BindConfiguration(GtfsValidatorSettings.Path);
```

**Benefits**:
- Type-safe configuration access
- Validation of configuration values
- Hot-reload capabilities

### 5. Middleware Pattern

Cross-cutting concerns handled via middleware:
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Benefits**:
- Centralized error handling
- Request/response pipeline customization
- Separation of concerns

## Concurrency and Threading

### Thread Safety Considerations

1. **LiteDB Concurrency**: 
   - Single-writer, multiple-reader model
   - Database connections properly disposed
   - No explicit locking required

2. **File System Access**:
   - Unique directories per validation
   - Atomic file operations
   - Cleanup after processing

3. **Background Worker**:
   - Single worker instance (Singleton)
   - Sequential processing of queue
   - CancellationToken for graceful shutdown

### Asynchronous Processing

- **async/await** pattern throughout
- **Task-based** operations for I/O
- **CancellationToken** support for operations
- **ConfigureAwait(false)** where appropriate

## Security Architecture

### File Upload Security

1. **File Type Validation**: Only ZIP files accepted
2. **Size Limits**: Configurable upload size limits
3. **Temporary Storage**: Files stored in isolated directories
4. **Automatic Cleanup**: Files removed after processing

### Data Protection

1. **No Sensitive Data Storage**: Only file paths and results stored
2. **Temporary Data Lifecycle**: 24-hour retention policy
3. **Process Isolation**: Java processes run in separate memory space

### API Security

1. **HTTPS Enforcement**: Redirect HTTP to HTTPS in production
2. **CORS Configuration**: Cross-origin request handling
3. **Input Validation**: Request model validation
4. **Error Information Limiting**: No sensitive data in error responses

## Scalability Considerations

### Horizontal Scaling Challenges

1. **Shared File System**: Multiple instances need shared storage
2. **Database Locking**: LiteDB not suitable for multiple writers
3. **Process Coordination**: Worker coordination across instances

### Recommended Scaling Approaches

1. **Vertical Scaling**: Increase memory and CPU for single instance
2. **Load Balancing**: Use sticky sessions or shared storage
3. **Database Migration**: Consider SQL Server/PostgreSQL for multi-instance
4. **Message Queue**: Replace database queue with Redis/RabbitMQ

### Performance Optimizations

1. **Async Processing**: Non-blocking I/O operations
2. **Memory Management**: Proper disposal of resources
3. **File System Optimization**: SSD storage for working directory
4. **Database Indexing**: Proper indexes on query columns

## Error Handling Strategy

### Exception Hierarchy

1. **System Exceptions**: Infrastructure-related errors
2. **Validation Exceptions**: Business rule violations
3. **External Process Exceptions**: Java validator failures

### Error Response Strategy

```csharp
// Global exception middleware
public class ExceptionHandlingMiddleware
{
    // Catches all unhandled exceptions
    // Returns consistent error responses
    // Logs detailed error information
}
```

### Logging Strategy

1. **Structured Logging**: JSON-formatted log entries
2. **Log Levels**: Appropriate use of Debug/Info/Warning/Error
3. **Context Information**: Include correlation IDs and request details
4. **External Integration**: Ready for Application Insights, Serilog, etc.

## Configuration Management

### Configuration Sources

1. **appsettings.json**: Base configuration
2. **appsettings.{Environment}.json**: Environment-specific settings
3. **Environment Variables**: Container/deployment configuration
4. **User Secrets**: Development-only sensitive data
5. **Command Line Arguments**: Runtime overrides

### Configuration Schema

```csharp
public class GtfsValidatorSettings
{
    public string? ConnectionString { get; set; }     // Database path
    public string? WorkingDirectory { get; set; }     // Temp file storage
    public string? GtfsValidatorJarPath { get; set; } // Java JAR path
}
```

## Monitoring and Observability

### Health Checks

- **API Status Endpoint**: `/api-status` returns processing statistics
- **Application Health**: Basic health check for monitoring systems
- **Database Health**: Implicit through successful operations

### Metrics Collection Points

1. **Request Metrics**: Response times, status codes, throughput
2. **Processing Metrics**: Queue depth, processing times, success/failure rates
3. **Resource Metrics**: Memory usage, disk space, CPU utilization
4. **Business Metrics**: Total validations, average processing time

### Logging Integration Points

1. **Request Logging**: HTTP requests and responses
2. **Business Operations**: Validation start/completion events
3. **Error Logging**: Exceptions and failures
4. **Performance Logging**: Slow operations and resource usage

## Future Architecture Considerations

### Potential Improvements

1. **Message Queue Integration**: Replace database polling with message queue
2. **Distributed Storage**: Move to cloud storage for file handling
3. **Microservices Split**: Separate API and processing services
4. **Caching Layer**: Add Redis for frequently accessed results
5. **Event Sourcing**: Track all state changes for audit purposes

### Technology Migration Paths

1. **Database**: LiteDB ? PostgreSQL/SQL Server for scalability
2. **File Storage**: Local files ? Azure Blob/AWS S3
3. **Processing**: Background service ? Azure Functions/AWS Lambda
4. **Messaging**: Database polling ? Service Bus/RabbitMQ
5. **Orchestration**: Manual deployment ? Kubernetes/Container Apps