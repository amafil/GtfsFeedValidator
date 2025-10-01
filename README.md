# GTFS Feed Validator

A .NET 8 Web API service for validating GTFS (General Transit Feed Specification) feeds using the official Google GTFS Validator tool.

## Overview

The GTFS Feed Validator is a RESTful web service that provides asynchronous validation of GTFS transit feeds. It processes uploaded GTFS files through Google's official validator and returns detailed validation reports in both JSON and HTML formats.

## Features

- **Asynchronous Processing**: Upload GTFS files and receive validation results asynchronously
- **Multiple Output Formats**: Get validation results in JSON format or download HTML reports
- **Background Processing**: Uses a background worker service for handling validation tasks
- **Persistent Storage**: Uses LiteDB for storing validation status and results
- **Dockerized**: Ready for containerized deployment
- **OpenAPI/Swagger**: Comprehensive API documentation with Swagger UI
- **Automatic Cleanup**: Removes processed files and data after 24 hours

## Architecture

### Core Components

1. **Web API Layer**
   - `Program.cs`: Application entry point and service configuration
   - `Endpoints/Endpoints.cs`: API endpoint definitions

2. **Services Layer**
   - `IGtfsFeedValidatorService`: Main validation service interface
   - `GtfsFeedValidatorService`: Implementation of validation operations
   - `IGtfsFeedValidatorWorkerService`: Background worker interface
   - `GtfsFeedValidatorWorkerService`: Background processing implementation

3. **Background Processing**
   - `GtfsValidatorWorker`: Hosted service for continuous validation processing

4. **Data Layer**
   - `GtfsFeedValidation`: Entity for tracking validation requests
   - `GtfsFeedValidationResult`: Entity for storing validation results
   - LiteDB for lightweight data persistence

5. **Models & DTOs**
   - Request/Response models for API communication
   - Output models for GTFS validator results
   - AutoMapper profiles for model mapping

### Technology Stack

- **.NET 8**: Target framework
- **ASP.NET Core**: Web API framework
- **LiteDB**: Lightweight database for data persistence
- **AutoMapper**: Object-object mapping
- **Swashbuckle.AspNetCore**: OpenAPI/Swagger documentation
- **Google GTFS Validator**: Java-based validation engine
- **Docker**: Containerization support

## API Endpoints

### POST `/start-validation`
Upload a GTFS file to start validation process.

**Request**: Multipart form data with GTFS file
**Response**: `202 Accepted` with validation ID

### GET `/validation-result/{gtfsFeedValidationId}`
Get JSON validation results for a specific validation.

**Response**: 
- `200 OK`: Validation completed with results
- `204 No Content`: Validation in progress
- `404 Not Found`: Validation not found
- `500 Internal Server Error`: Validation error

### GET `/validation-result/download/{gtfsFeedValidationId}`
Download HTML validation report.

**Response**: 
- `200 OK`: HTML file download
- `204 No Content`: Validation in progress
- `404 Not Found`: Validation not found
- `500 Internal Server Error`: Validation error

### GET `/api-status`
Get API status information including total number of validations processed.

## Configuration

The application uses the `GtfsValidatorSettings` configuration section:

```json
{
  "GtfsFeedValidatorConfiguration": {
    "ConnectionString": "path/to/database.db",
    "WorkingDirectory": "path/to/working/directory",
    "GtfsValidatorJarPath": "path/to/gtfs-validator.jar"
  }
}
```

### Configuration Properties

- **ConnectionString**: Path to LiteDB database file
- **WorkingDirectory**: Directory for temporary file storage during processing
- **GtfsValidatorJarPath**: Path to the GTFS validator JAR file

## Deployment

### Docker Deployment

The application includes a Dockerfile for containerized deployment:

```bash
# Build the image
docker build -t gtfs-feed-validator .

# Run the container
docker run -p 8080:8080 gtfs-feed-validator
```

### Prerequisites

- Java Runtime Environment (JRE) - Required for GTFS validator JAR
- GTFS Validator JAR file (gtfs-validator.5.0.1-cli.jar)

## Development

### Project Structure

```
GtfsFeedValidator/
??? Automapper/              # AutoMapper profiles
??? Configuration/           # Configuration classes
??? Database/               # Database entities
??? Endpoints/              # API endpoint definitions
??? Middleware/             # Custom middleware
??? Models/                 # Data models and DTOs
?   ??? Responses/         # API response models
?   ??? ValidatorOutput/   # GTFS validator output models
??? Services/              # Business logic services
??? Constants.cs           # Application constants
??? Program.cs             # Application entry point
??? Dockerfile             # Docker configuration
```

### Testing

The solution includes a test project `GtfsFeedValidator.Test` with:

- Unit tests using MSTest framework
- Integration tests with `Microsoft.AspNetCore.Mvc.Testing`
- FluentAssertions for readable test assertions
- Test coverage with Coverlet

### Building and Running

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Run tests
dotnet test
```

## Validation Process Flow

1. **Upload**: Client uploads GTFS file via POST `/start-validation`
2. **Enqueue**: File is stored temporarily and validation request is queued
3. **Process**: Background worker picks up the request and executes Java validator
4. **Store**: Validation results (JSON and HTML) are stored in database
5. **Retrieve**: Client polls for results via GET endpoints
6. **Cleanup**: Files and data are automatically cleaned up after 24 hours

## Status Management

The application tracks validation status through the following states:

- **Pending**: Validation request queued but not started
- **Processing**: Validation currently in progress
- **Completed**: Validation finished successfully
- **Error**: Validation failed due to an error

## Error Handling

- Custom exception handling middleware for consistent error responses
- Proper HTTP status codes for different scenarios
- Comprehensive logging for troubleshooting
- Graceful handling of validation failures

## Security Considerations

- File upload validation and size limits
- Temporary file cleanup to prevent disk space issues
- User secrets for sensitive configuration data
- HTTPS redirection in production environments

## Performance Features

- Asynchronous file processing to avoid blocking API calls
- Background worker service for scalable processing
- Automatic cleanup to manage storage usage
- Lightweight LiteDB for minimal overhead

## Monitoring and Observability

- Structured logging with ILogger
- API status endpoint for health monitoring
- Swagger UI for API exploration and testing
- Exception tracking and error logging

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]

## Support

[Add support information here]