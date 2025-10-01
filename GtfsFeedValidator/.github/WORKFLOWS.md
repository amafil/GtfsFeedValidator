# GitHub Workflows Documentation

This document describes the CI/CD workflows configured for the GTFS Feed Validator project.

## Overview

The project uses GitHub Actions for continuous integration, testing, and deployment. The workflows are designed to ensure code quality, security, and reliable releases.

## Workflows

### 1. Pull Request Validation (`pr-validation.yml`)

**Trigger**: Pull requests to `main` or `develop` branches

**Purpose**: Validates pull requests before merging by running comprehensive tests and checks.

**Jobs**:
- **validate**: Main validation job that builds and tests the solution
  - Restores NuGet dependencies with caching
  - Builds the solution in Release configuration
  - Runs unit tests with code coverage collection
  - Uploads test results and coverage reports
  - Validates Docker build without creating artifacts
  
- **security-scan**: Scans for vulnerable dependencies
  - Checks for known security vulnerabilities in NuGet packages
  - Fails if critical vulnerabilities are found
  
- **code-quality**: Code quality analysis (optional SonarCloud integration)
  - Sets up SonarCloud scanner (when configured)
  - Ready for static code analysis

**Features**:
- ? Code coverage reporting in PR comments
- ? Docker build validation
- ? Security vulnerability scanning
- ? Test result artifacts (7-day retention)
- ? NuGet package caching for faster builds

**Artifacts**: None (validation only)

### 2. Release Build and Deploy (`release.yml`)

**Trigger**: 
- Git tags matching `v*.*.*` or `release-*` patterns
- Manual workflow dispatch with version input

**Purpose**: Creates production-ready releases with artifacts and deployment.

**Jobs**:
- **validate-and-test**: Validates the release candidate
  - Full test suite execution
  - Version determination from tag or input
  - Test results with 90-day retention
  
- **build-artifacts**: Creates platform-specific binaries
  - Builds self-contained executables for multiple platforms:
    - `linux-x64`, `win-x64`, `osx-x64`, `linux-arm64`
  - Downloads official GTFS Validator JAR
  - Creates compressed archives (ZIP for Windows, TAR.GZ for others)
  - Publishes as single-file executables
  
- **build-docker**: Builds and publishes Docker images
  - Multi-platform Docker images (`linux/amd64`, `linux/arm64`)
  - Publishes to GitHub Container Registry (ghcr.io)
  - Tags: version-specific and `latest`
  - Uses Docker layer caching for efficiency
  
- **create-release**: Creates GitHub release
  - Generates changelog from git commits
  - Creates release notes with installation instructions
  - Attaches all platform-specific artifacts
  - Marks as prerelease for alpha/beta/rc versions
  
- **deploy-staging**: Deploys to staging environment
  - Runs for stable releases (non-alpha/beta)
  - Template for staging deployment steps
  
- **notify**: Sends notifications about release status

**Features**:
- ? Multi-platform binary releases
- ? Docker images with multi-architecture support
- ? Automatic changelog generation
- ? GitHub Container Registry integration
- ? Staging deployment pipeline
- ? Prerelease detection

**Artifacts**: 
- Platform-specific binary archives
- Docker images in GHCR
- GitHub release with assets

### 3. Continuous Integration (`ci.yml`)

**Trigger**: 
- Pushes to `main` or `develop` branches
- Daily scheduled runs at 2 AM UTC
- Excludes documentation-only changes

**Purpose**: Continuous validation of the main codebase across multiple platforms.

**Jobs**:
- **build-and-test**: Multi-OS testing matrix
  - Tests on Ubuntu, Windows, and macOS
  - Ensures cross-platform compatibility
  - 30-day test result retention
  
- **security-analysis**: Comprehensive security checks
  - CodeQL static analysis
  - Vulnerable package detection
  - Security event reporting
  
- **docker-build**: Docker integration testing
  - Builds Docker image without pushing
  - Performs container health checks
  - Validates API endpoints
  
- **code-quality**: Code quality checks
  - Code formatting validation with `dotnet format`
  - Ready for SonarCloud integration
  
- **performance-test**: Performance validation (main branch only)
  - Template for performance test execution
  
- **dependency-review**: Dependency analysis (main branch only)
  - Reviews dependency changes
  - Lists outdated packages
  - Fails on moderate+ severity vulnerabilities
  
- **notify-status**: Build status notifications

**Features**:
- ? Multi-OS compatibility testing
- ? CodeQL security analysis
- ? Docker health checks
- ? Code formatting validation
- ? Dependency vulnerability scanning
- ? Scheduled daily builds

**Artifacts**: Test results and coverage reports

### 4. Dependabot Configuration (`dependabot.yml`)

**Purpose**: Automated dependency management and security updates.

**Update Schedules**:
- **NuGet packages**: Weekly on Mondays
- **Docker base images**: Weekly on Tuesdays  
- **GitHub Actions**: Weekly on Wednesdays

**Features**:
- ? Separate configuration for main and test projects
- ? Major version update protection
- ? Automatic labeling and assignment
- ? Controlled PR limits to avoid spam

## Workflow Security

### Permissions
- **Contents**: Read access for code checkout
- **Packages**: Write access for container registry
- **Security Events**: Write access for CodeQL
- **Actions**: Read access for workflow execution

### Secrets Required
- `GITHUB_TOKEN`: Automatically provided (container registry, releases)
- `SONAR_TOKEN`: Required for SonarCloud integration (optional)

### Security Features
- Dependency vulnerability scanning
- CodeQL static analysis
- Container security scanning
- Secrets scanning (GitHub native)

## Branch Protection

Recommended branch protection rules for `main`:

```yaml
required_status_checks:
  strict: true
  contexts:
    - "Build and Test (ubuntu-latest)"
    - "Security Analysis" 
    - "Code Quality Analysis"
enforce_admins: true
required_pull_request_reviews:
  required_approving_review_count: 1
  dismiss_stale_reviews: true
restrictions: null
```

## Environment Setup

### Required Repository Settings

1. **Enable GitHub Actions**
2. **Container Registry Access**: Enable GitHub Container Registry
3. **Branch Protection**: Configure protection rules
4. **Secrets Management**: Add required secrets

### Optional Integrations

1. **SonarCloud**:
   ```bash
   # Add to repository secrets
   SONAR_TOKEN=<your-sonar-token>
   ```

2. **Notification Webhooks**:
   - Slack integration
   - Microsoft Teams
   - Email notifications

### Local Development

To run similar checks locally:

```bash
# Build and test
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Security scan
dotnet list package --vulnerable

# Code formatting
dotnet format --verify-no-changes

# Docker build
docker build -t gtfs-validator .
```

## Artifact Management

### Retention Policies
- **PR Validation**: 7 days
- **CI Builds**: 30 days  
- **Releases**: 90 days
- **Docker Images**: No automatic cleanup

### Storage Optimization
- NuGet package caching
- Docker layer caching
- Compressed artifact uploads

## Troubleshooting

### Common Issues

1. **Test Failures**: Check test result artifacts for detailed logs
2. **Docker Build Issues**: Ensure GTFS Validator JAR is available
3. **Security Scan Failures**: Review vulnerability reports and update packages
4. **Code Quality Issues**: Run `dotnet format` locally before pushing

### Workflow Debugging

Enable debug logging:
```yaml
env:
  ACTIONS_STEP_DEBUG: true
  ACTIONS_RUNNER_DEBUG: true
```

### Performance Optimization

- Use dependency caching
- Minimize artifact sizes
- Parallel job execution
- Conditional job execution

## Monitoring and Alerts

### GitHub Insights
- Actions tab for workflow runs
- Dependency graph for security alerts
- Code scanning alerts

### Recommended Monitoring
- Failed workflow notifications
- Security alert subscriptions  
- Dependency update notifications
- Performance regression alerts

## Best Practices

### Workflow Design
- ? Use caching for dependencies
- ? Fail fast on critical errors
- ? Separate concerns into focused jobs
- ? Use conditional execution where appropriate
- ? Implement proper error handling

### Security
- ? Minimal required permissions
- ? No hardcoded secrets
- ? Vulnerability scanning
- ? Dependency updates

### Maintenance
- ? Regular workflow updates
- ? Monitor execution times
- ? Review and update dependencies
- ? Documentation updates

## Migration Guide

### From Other CI Systems

1. **Azure DevOps**: Similar YAML syntax, adjust trigger and task names
2. **Jenkins**: Convert Jenkinsfile to GitHub Actions YAML
3. **GitLab CI**: Adapt .gitlab-ci.yml structure

### Workflow Updates

When updating workflows:
1. Test in feature branch first
2. Use workflow dispatch for manual testing
3. Monitor execution after merge
4. Update documentation accordingly