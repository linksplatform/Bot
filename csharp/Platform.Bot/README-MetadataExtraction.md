# Repository Metadata Extraction Feature

This document describes the new repository metadata extraction functionality added to Platform.Bot to address issue #91.

## Overview

The repository metadata extraction feature allows the GitHub bot to extract complete data and metadata about repositories in an organization. This feature helps to:

1. **Reduce API requests** - By caching comprehensive repository data locally
2. **Enable backup capabilities** - Store complete repository metadata for disaster recovery
3. **Provide statistics and analytics** - Generate insights about repository usage, activity, and trends
4. **Support offline analysis** - Work with repository data without continuous API calls

## Components

### 1. RepositoryMetadataExtractorTrigger

**Location:** `csharp/Platform.Bot/Triggers/RepositoryMetadataExtractorTrigger.cs`

The main extraction engine that:
- Retrieves all repositories for an organization
- Collects comprehensive metadata for each repository
- Extracts recent commits, pull requests, and issues
- Saves data in structured JSON format
- Runs on a configurable schedule (default: every 6 hours)

**Key Features:**
- Automatic scheduling with configurable intervals
- Comprehensive data collection including commits, PRs, and issues
- Error handling and graceful degradation
- Timestamped backups with current snapshot

### 2. RepositoryStatisticsAnalyzer

**Location:** `csharp/Platform.Bot/RepositoryStatisticsAnalyzer.cs`

Provides statistical analysis and reporting:
- Generates comprehensive organization statistics
- Analyzes language distribution
- Identifies most active and popular repositories
- Calculates activity percentages and trends
- Exports data in both JSON and human-readable formats

### 3. Command Line Interface

**Location:** `csharp/Platform.Bot/Commands/RepositoryMetadataCommand.cs`

CLI commands for manual operations:
- `metadata extract` - Manually trigger metadata extraction
- `metadata stats` - Generate statistics from extracted data
- `metadata report` - Create human-readable reports

## Usage

### Automatic Operation (Integrated with Bot)

The metadata extraction runs automatically when the bot is started with the standard parameters:

```bash
dotnet run --github-user-name "username" --github-api-token "token" --github-application-name "app"
```

The extraction trigger is integrated into the main bot loop and runs every 6 hours by default.

### Manual Operations

#### Extract Repository Metadata

```bash
dotnet run metadata extract --github-user-name "username" --github-api-token "token" --github-application-name "app" --organization "linksplatform" --output-dir "metadata"
```

#### Generate Statistics

```bash
dotnet run metadata stats --output-dir "metadata"
```

#### Generate Text Report

```bash
dotnet run metadata report --output-dir "metadata"
```

## Data Structure

### Repository Metadata

Each repository's metadata includes:

**Basic Information:**
- ID, name, full name, description
- Visibility (public/private), fork status
- Creation and update timestamps
- Size, language, topics

**Activity Metrics:**
- Stars, forks, watchers count
- Open issues and pull requests
- Recent commits (last 30 days)
- Recent activity summary

**Extended Data:**
- Recent commits with author information
- Pull request details and status
- Issue information with labels
- Repository permissions

### Statistics Output

Generated statistics include:

**Organization Overview:**
- Total repositories (public/private/archived)
- Language distribution
- Activity percentages

**Activity Analysis:**
- Most active repositories
- Most popular repositories (by stars)
- Recent activity trends

**Resource Metrics:**
- Total stars, forks, watchers
- Average repository size
- Popular topics and tags

## File Structure

The feature creates the following files in the output directory:

```
repository-metadata/
├── metadata-20240101-120000.json    # Timestamped backup
├── current-snapshot.json            # Latest data snapshot
├── last-extraction.json             # Extraction timestamp
├── statistics-report-20240101.json  # Generated statistics
├── latest-statistics.json           # Latest statistics
└── report-20240101.md              # Human-readable report
```

## Benefits

### 1. Reduced API Requests

- Bulk data extraction reduces individual API calls
- Cached data can be used for multiple operations
- Scheduled extraction minimizes real-time API usage

### 2. Backup and Recovery

- Complete repository metadata backup
- Historical data preservation
- Disaster recovery capabilities

### 3. Analytics and Insights

- Organization-wide repository analysis
- Activity trend identification
- Resource utilization metrics

### 4. Offline Capabilities

- Work with repository data without constant API access
- Faster data processing and analysis
- Reduced dependency on GitHub API availability

## Configuration

The extraction system can be configured with:

- **Organization name** - Target GitHub organization
- **Extraction interval** - How often to extract data (default: 6 hours)
- **Output directory** - Where to store extracted data and reports
- **Commit history depth** - How far back to look for recent commits (default: 30 days)

## Error Handling

The system includes comprehensive error handling:

- Graceful degradation when specific repositories are inaccessible
- Warning messages for partial data extraction
- Continuation of processing despite individual repository errors
- Detailed error logging for troubleshooting

## Integration

The metadata extraction feature is fully integrated with the existing Platform.Bot architecture:

- Uses existing GitHubStorage class for API access
- Follows the ITrigger<T> pattern for consistency
- Integrates with DateTimeTracker for scheduling
- Uses existing command line infrastructure

## Future Enhancements

Potential improvements could include:

1. **Incremental extraction** - Update only changed repositories
2. **Data compression** - Reduce storage requirements for large organizations
3. **Database integration** - Store data in structured database instead of JSON
4. **Real-time notifications** - Alert on significant changes or issues
5. **Custom metrics** - User-defined statistics and reports
6. **API endpoint** - Expose extracted data via REST API

## Example Usage

See `examples/RepositoryMetadataExample.cs` for complete usage examples demonstrating:
- Manual metadata extraction
- Statistics generation
- Report creation
- Individual repository analysis