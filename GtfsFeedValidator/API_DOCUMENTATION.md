# API Documentation

## Overview

The GTFS Feed Validator API provides endpoints for validating GTFS (General Transit Feed Specification) transit feeds. The API follows RESTful principles and provides asynchronous processing of validation requests.

## Base URL

- Development: `https://localhost:7xxx` (HTTPS)
- Development: `http://localhost:5xxx` (HTTP)

## Authentication

Currently, the API does not require authentication.

## Content Types

- **Request**: `multipart/form-data` for file uploads
- **Response**: `application/json` for JSON responses, `text/html` for HTML downloads

## API Endpoints

### 1. Start Validation

Uploads a GTFS file and starts the validation process.

**Endpoint**: `POST /start-validation`

**Request**:
```http
POST /start-validation HTTP/1.1
Content-Type: multipart/form-data

file: [GTFS ZIP FILE]
```

**Response**:
```http
HTTP/1.1 202 Accepted
Content-Type: application/json
Location: /validation-result

"550e8400-e29b-41d4-a716-446655440000"
```

**Response Codes**:
- `202 Accepted`: Validation request accepted and queued
- `500 Internal Server Error`: Error during validation enqueuing

---

### 2. Get Validation Result (JSON)

Retrieves the validation result in JSON format.

**Endpoint**: `GET /validation-result/{gtfsFeedValidationId}`

**Parameters**:
- `gtfsFeedValidationId` (string, required): The validation ID returned from `/start-validation`

**Response** (Completed):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "summary": {
    "validationTimeSeconds": 12.5,
    "threadCount": 4,
    "gtfsInput": {
      "gtfsFilename": "sample-feed.zip",
      "gtfsFileSizeBytes": 1048576
    },
    "validationResult": "PASSED_VALIDATION",
    "systemErrors": []
  },
  "counts": {
    "total": 150,
    "errors": 0,
    "warnings": 5,
    "infos": 145
  },
  "agencies": [
    {
      "agencyId": "AGENCY_001",
      "agencyName": "Sample Transit Authority"
    }
  ],
  "feedInfo": {
    "feedPublisherName": "Sample Publisher",
    "feedPublisherUrl": "https://example.com",
    "feedLang": "en",
    "feedVersion": "1.0"
  },
  "notices": [
    {
      "code": "INFO_001",
      "severity": "INFO",
      "totalNotices": 145,
      "sampleNotices": [
        {
          "filename": "stops.txt",
          "csvRowNumber": 1,
          "fieldName": "stop_name"
        }
      ]
    }
  ]
}
```

**Response** (In Progress):
```http
HTTP/1.1 204 No Content
```

**Response Codes**:
- `200 OK`: Validation completed, results available
- `204 No Content`: Validation still in progress
- `404 Not Found`: Validation ID not found
- `500 Internal Server Error`: Error during validation

---

### 3. Download Validation Report (HTML)

Downloads the validation report as an HTML file.

**Endpoint**: `GET /validation-result/download/{gtfsFeedValidationId}`

**Parameters**:
- `gtfsFeedValidationId` (string, required): The validation ID returned from `/start-validation`

**Response** (Completed):
```http
HTTP/1.1 200 OK
Content-Type: text/html
Content-Disposition: attachment; filename=report.html

<!DOCTYPE html>
<html>
<head>
    <title>GTFS Validation Report</title>
</head>
<body>
    <!-- HTML validation report content -->
</body>
</html>
```

**Response Codes**:
- `200 OK`: Validation completed, HTML report download
- `204 No Content`: Validation still in progress
- `404 Not Found`: Validation ID not found
- `500 Internal Server Error`: Error during validation

---

### 4. API Status

Returns the current status of the API including the total number of validations processed.

**Endpoint**: `GET /api-status`

**Response**:
```http
HTTP/1.1 200 OK
Content-Type: application/json

42
```

**Response Codes**:
- `200 OK`: Status information returned

## Response Models

### GtfsValidatorResponse

The main validation response object containing all validation results.

```typescript
interface GtfsValidatorResponse {
  summary: SummaryResponse;
  counts: CountsResponse;
  agencies: AgencyResponse[];
  feedInfo: FeedInfoResponse;
  notices: NoticeResponse[];
}
```

### SummaryResponse

```typescript
interface SummaryResponse {
  validationTimeSeconds: number;
  threadCount: number;
  gtfsInput: {
    gtfsFilename: string;
    gtfsFileSizeBytes: number;
  };
  validationResult: string;
  systemErrors: string[];
}
```

### CountsResponse

```typescript
interface CountsResponse {
  total: number;
  errors: number;
  warnings: number;
  infos: number;
}
```

### AgencyResponse

```typescript
interface AgencyResponse {
  agencyId: string;
  agencyName: string;
}
```

### FeedInfoResponse

```typescript
interface FeedInfoResponse {
  feedPublisherName: string;
  feedPublisherUrl: string;
  feedLang: string;
  feedVersion: string;
}
```

### NoticeResponse

```typescript
interface NoticeResponse {
  code: string;
  severity: string;
  totalNotices: number;
  sampleNotices: SampleNoticeResponse[];
}
```

### SampleNoticeResponse

```typescript
interface SampleNoticeResponse {
  filename: string;
  csvRowNumber: number;
  fieldName: string;
}
```

## Error Responses

### Standard Error Response

```typescript
interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
}
```

Example:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Feed Validation Error",
  "status": 500,
  "detail": "There was an error during feed validation",
  "instance": "/validation-result/550e8400-e29b-41d4-a716-446655440000"
}
```

## Usage Examples

### JavaScript/Fetch API

```javascript
// Start validation
const formData = new FormData();
formData.append('file', gtfsFile);

const response = await fetch('/start-validation', {
  method: 'POST',
  body: formData
});

const validationId = await response.text();

// Poll for results
const pollForResults = async (id) => {
  let result;
  do {
    await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second
    result = await fetch(`/validation-result/${id}`);
  } while (result.status === 204);
  
  if (result.ok) {
    return await result.json();
  }
  throw new Error('Validation failed');
};

const validationResult = await pollForResults(validationId);
```

### cURL

```bash
# Start validation
VALIDATION_ID=$(curl -X POST \
  -F "file=@gtfs-feed.zip" \
  http://localhost:5000/start-validation)

# Check status
curl "http://localhost:5000/validation-result/$VALIDATION_ID"

# Download HTML report
curl -o report.html \
  "http://localhost:5000/validation-result/download/$VALIDATION_ID"
```

### C# HttpClient

```csharp
using var client = new HttpClient();

// Start validation
using var form = new MultipartFormDataContent();
using var fileContent = new ByteArrayContent(gtfsFileBytes);
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
form.Add(fileContent, "file", "gtfs-feed.zip");

var response = await client.PostAsync("/start-validation", form);
var validationId = await response.Content.ReadAsStringAsync();

// Poll for results
HttpResponseMessage result;
do
{
    await Task.Delay(1000);
    result = await client.GetAsync($"/validation-result/{validationId}");
} while (result.StatusCode == HttpStatusCode.NoContent);

var validationResult = await result.Content.ReadFromJsonAsync<GtfsValidatorResponse>();
```

## Rate Limiting

Currently, there are no enforced rate limits, but it's recommended to:
- Avoid uploading multiple large files simultaneously
- Poll for results no more frequently than once per second
- Clean up completed validations to free resources

## File Size Limits

- Maximum file size: Depends on server configuration (default ASP.NET Core limits apply)
- Supported formats: ZIP files containing GTFS data
- Temporary storage: Files are automatically cleaned up after 24 hours

## OpenAPI/Swagger

The API provides OpenAPI specification and Swagger UI for interactive documentation:

- **Swagger UI**: Available at `/swagger` in development and staging environments
- **OpenAPI JSON**: Available at `/swagger/v1/swagger.json`

## Validation Status Lifecycle

1. **Pending**: Request received and queued
2. **Processing**: Background worker is validating the feed
3. **Completed**: Validation finished, results available
4. **Error**: Validation failed due to system error

## Best Practices

1. **Polling**: Use exponential backoff when polling for results
2. **Error Handling**: Always check HTTP status codes and handle errors appropriately
3. **File Formats**: Ensure GTFS files are properly formatted ZIP archives
4. **Timeouts**: Implement reasonable timeouts for large file validations
5. **Storage**: Download and store results locally as they're cleaned up after 24 hours