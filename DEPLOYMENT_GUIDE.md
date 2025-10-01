# Deployment Guide

This guide covers different deployment scenarios for the GTFS Feed Validator service.

## Prerequisites

### System Requirements

- **.NET 8 Runtime** or SDK
- **Java Runtime Environment (JRE)** - Required for GTFS validator JAR execution
- **GTFS Validator JAR** - `gtfs-validator.5.0.1-cli.jar`

### Dependencies

- Minimum 2GB RAM (recommended 4GB for large GTFS files)
- Minimum 10GB disk space for temporary file storage
- Network access (if downloading GTFS validator JAR)

## Quick Start (Local Development)

### 1. Clone and Setup

```bash
git clone <repository-url>
cd GtfsFeedValidator/GtfsFeedValidator
```

### 2. Configure Application

Create `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "GtfsFeedValidatorConfiguration": {
    "ConnectionString": "Data/gtfs-validator.db",
    "WorkingDirectory": "WorkDir",
    "GtfsValidatorJarPath": "gtfs-validator.5.0.1-cli.jar"
  }
}
```

### 3. Download GTFS Validator

```bash
# Download the official GTFS validator JAR
wget -O gtfs-validator.5.0.1-cli.jar \
  https://github.com/MobilityData/gtfs-validator/releases/download/v5.0.1/gtfs-validator-5.0.1-cli.jar
```

### 4. Run the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The application will be available at:
- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`
- Swagger UI: `https://localhost:7xxx/swagger`

## Docker Deployment

### 1. Build Docker Image

```bash
# Ensure gtfs-validator JAR is in the project root
cp gtfs-validator.5.0.1-cli.jar ./

# Build the image
docker build -t gtfs-feed-validator .
```

### 2. Run Container

```bash
# Create directories for persistent data
mkdir -p ./data
mkdir -p ./workdir

# Run container with volume mounts
docker run -d \
  --name gtfs-validator \
  -p 8080:8080 \
  -v $(pwd)/data:/app/Data \
  -v $(pwd)/workdir:/app/WorkDir \
  -e GtfsFeedValidatorConfiguration__ConnectionString="Data/gtfs-validator.db" \
  -e GtfsFeedValidatorConfiguration__WorkingDirectory="WorkDir" \
  -e GtfsFeedValidatorConfiguration__GtfsValidatorJarPath="gtfs-validator.5.0.1-cli.jar" \
  gtfs-feed-validator
```

### 3. Docker Compose (Recommended)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  gtfs-validator:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - ./data:/app/Data
      - ./workdir:/app/WorkDir
    environment:
      - GtfsFeedValidatorConfiguration__ConnectionString=Data/gtfs-validator.db
      - GtfsFeedValidatorConfiguration__WorkingDirectory=WorkDir
      - GtfsFeedValidatorConfiguration__GtfsValidatorJarPath=gtfs-validator.5.0.1-cli.jar
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api-status"]
      interval: 30s
      timeout: 10s
      retries: 3
```

Run with:
```bash
docker-compose up -d
```

## Production Deployment

### Linux (Ubuntu/Debian)

#### 1. Install Prerequisites

```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0

# Install Java
sudo apt-get install -y default-jre

# Install Nginx (optional, for reverse proxy)
sudo apt-get install -y nginx
```

#### 2. Create Application User

```bash
sudo useradd -r -m -s /bin/false gtfsvalidator
sudo mkdir -p /opt/gtfs-validator
sudo chown gtfsvalidator:gtfsvalidator /opt/gtfs-validator
```

#### 3. Deploy Application

```bash
# Publish application
dotnet publish -c Release -o /opt/gtfs-validator

# Copy GTFS validator JAR
sudo cp gtfs-validator.5.0.1-cli.jar /opt/gtfs-validator/

# Set permissions
sudo chown -R gtfsvalidator:gtfsvalidator /opt/gtfs-validator
sudo chmod +x /opt/gtfs-validator/GtfsFeedValidator
```

#### 4. Create Systemd Service

Create `/etc/systemd/system/gtfs-validator.service`:

```ini
[Unit]
Description=GTFS Feed Validator Service
Documentation=https://github.com/yourusername/gtfs-feed-validator
After=network.target

[Service]
Type=notify
User=gtfsvalidator
Group=gtfsvalidator
WorkingDirectory=/opt/gtfs-validator
ExecStart=/opt/gtfs-validator/GtfsFeedValidator
Restart=on-failure
RestartSec=5
TimeoutStopSec=90
KillMode=mixed
SyslogIdentifier=gtfs-validator
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Environment variables for configuration
Environment=GtfsFeedValidatorConfiguration__ConnectionString=/var/lib/gtfs-validator/gtfs-validator.db
Environment=GtfsFeedValidatorConfiguration__WorkingDirectory=/var/lib/gtfs-validator/workdir
Environment=GtfsFeedValidatorConfiguration__GtfsValidatorJarPath=/opt/gtfs-validator/gtfs-validator.5.0.1-cli.jar

[Install]
WantedBy=multi-user.target
```

#### 5. Create Data Directories

```bash
sudo mkdir -p /var/lib/gtfs-validator/workdir
sudo chown -R gtfsvalidator:gtfsvalidator /var/lib/gtfs-validator
```

#### 6. Start Service

```bash
sudo systemctl daemon-reload
sudo systemctl enable gtfs-validator
sudo systemctl start gtfs-validator
sudo systemctl status gtfs-validator
```

#### 7. Configure Nginx Reverse Proxy (Optional)

Create `/etc/nginx/sites-available/gtfs-validator`:

```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Increase timeout for large file uploads
        proxy_connect_timeout 600;
        proxy_send_timeout 600;
        proxy_read_timeout 600;
        send_timeout 600;
        
        # Increase max body size for file uploads
        client_max_body_size 100M;
    }
}
```

Enable site:
```bash
sudo ln -s /etc/nginx/sites-available/gtfs-validator /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Windows (IIS)

#### 1. Install Prerequisites

- Install .NET 8 Hosting Bundle
- Install Java Runtime Environment
- Enable IIS with ASP.NET Core Module

#### 2. Publish Application

```powershell
dotnet publish -c Release -o C:\inetpub\gtfs-validator
```

#### 3. Create IIS Site

```powershell
# Import WebAdministration module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "GtfsValidator" -Force
Set-ItemProperty -Path "IIS:\AppPools\GtfsValidator" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\GtfsValidator" -Name "managedRuntimeVersion" -Value ""

# Create website
New-Website -Name "GTFS Validator" -Port 80 -PhysicalPath "C:\inetpub\gtfs-validator" -ApplicationPool "GtfsValidator"
```

#### 4. Configure Application

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "GtfsFeedValidatorConfiguration": {
    "ConnectionString": "C:\\ProgramData\\GtfsValidator\\gtfs-validator.db",
    "WorkingDirectory": "C:\\ProgramData\\GtfsValidator\\WorkDir",
    "GtfsValidatorJarPath": "C:\\inetpub\\gtfs-validator\\gtfs-validator.5.0.1-cli.jar"
  }
}
```

## Kubernetes Deployment

### 1. Create Namespace

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: gtfs-validator
```

### 2. Create ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: gtfs-validator-config
  namespace: gtfs-validator
data:
  appsettings.json: |
    {
      "GtfsFeedValidatorConfiguration": {
        "ConnectionString": "/data/gtfs-validator.db",
        "WorkingDirectory": "/workdir",
        "GtfsValidatorJarPath": "/app/gtfs-validator.5.0.1-cli.jar"
      }
    }
```

### 3. Create Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gtfs-validator
  namespace: gtfs-validator
spec:
  replicas: 2
  selector:
    matchLabels:
      app: gtfs-validator
  template:
    metadata:
      labels:
        app: gtfs-validator
    spec:
      containers:
      - name: gtfs-validator
        image: gtfs-feed-validator:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        volumeMounts:
        - name: config
          mountPath: /app/appsettings.json
          subPath: appsettings.json
        - name: data
          mountPath: /data
        - name: workdir
          mountPath: /workdir
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1"
        livenessProbe:
          httpGet:
            path: /api-status
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /api-status
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config
        configMap:
          name: gtfs-validator-config
      - name: data
        persistentVolumeClaim:
          claimName: gtfs-validator-data
      - name: workdir
        emptyDir: {}
```

### 4. Create Service and Ingress

```yaml
---
apiVersion: v1
kind: Service
metadata:
  name: gtfs-validator-service
  namespace: gtfs-validator
spec:
  selector:
    app: gtfs-validator
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP

---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: gtfs-validator-ingress
  namespace: gtfs-validator
  annotations:
    nginx.ingress.kubernetes.io/proxy-body-size: "100m"
spec:
  rules:
  - host: gtfs-validator.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: gtfs-validator-service
            port:
              number: 80
```

## Configuration Management

### Environment Variables

The application supports configuration via environment variables using the ASP.NET Core configuration pattern:

```bash
# Database connection
export GtfsFeedValidatorConfiguration__ConnectionString="/data/gtfs-validator.db"

# Working directory
export GtfsFeedValidatorConfiguration__WorkingDirectory="/workdir"

# GTFS Validator JAR path
export GtfsFeedValidatorConfiguration__GtfsValidatorJarPath="/app/gtfs-validator.5.0.1-cli.jar"

# ASP.NET Core environment
export ASPNETCORE_ENVIRONMENT="Production"
```

### User Secrets (Development)

For development, use user secrets for sensitive configuration:

```bash
dotnet user-secrets set "GtfsFeedValidatorConfiguration:ConnectionString" "Data/gtfs-validator.db"
```

### Configuration Sources Priority

1. Command line arguments
2. Environment variables
3. User secrets (development only)
4. `appsettings.{Environment}.json`
5. `appsettings.json`

## Health Checks and Monitoring

### Application Health

The API provides a simple health check endpoint:

```bash
curl http://localhost:8080/api-status
```

### Logging

Configure structured logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

### Metrics Integration

For production monitoring, consider integrating:

- **Application Insights** (Azure)
- **Prometheus** + Grafana
- **ELK Stack** for logging
- **Health checks** middleware

## Security Considerations

### File Upload Security

- Configure maximum file size limits
- Validate file types and content
- Use antivirus scanning for uploaded files
- Implement rate limiting

### Network Security

- Use HTTPS in production
- Configure firewall rules
- Use reverse proxy (Nginx/IIS)
- Implement API key authentication if needed

### Data Protection

- Encrypt sensitive configuration data
- Use secure storage for database files
- Implement proper backup strategies
- Regular security updates

## Troubleshooting

### Common Issues

#### Java Not Found
```
Error: Java is not installed or not in PATH
```
**Solution**: Install JRE and ensure it's in the system PATH.

#### Permission Denied
```
Error: Access to the path '/workdir' is denied
```
**Solution**: Ensure the application user has read/write permissions to working directory.

#### Database Lock
```
Error: Database is locked
```
**Solution**: Ensure only one application instance is running or use different database files.

### Log Analysis

Check application logs for detailed error information:

```bash
# Docker logs
docker logs gtfs-validator

# Systemd journal
sudo journalctl -u gtfs-validator -f

# Application logs
tail -f /var/log/gtfs-validator.log
```

## Performance Tuning

### Memory Configuration

```json
{
  "GarbageCollection": {
    "Server": true,
    "Concurrent": true
  }
}
```

### File Processing

- Use SSD storage for working directory
- Increase available memory for large GTFS files
- Configure appropriate worker thread counts
- Monitor disk usage and implement cleanup policies

## Backup and Recovery

### Database Backup

```bash
# Backup LiteDB database
cp /var/lib/gtfs-validator/gtfs-validator.db /backup/gtfs-validator-$(date +%Y%m%d).db
```

### Disaster Recovery

1. Regular database backups
2. Configuration backup
3. Application binary backup
4. Monitoring and alerting setup
5. Recovery procedure documentation