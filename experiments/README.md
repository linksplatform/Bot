# Code Duplication Detection Bot - Experiments

This directory contains experiments and examples for testing the code duplication detection functionality.

## Features Implemented

### 1. Code Duplication Detection Service
- **Location**: `csharp/Platform.Bot/Services/CodeDuplicationAnalysisService.cs`
- **Purpose**: Analyzes repositories for duplicate code fragments
- **Features**:
  - Extracts code fragments from multiple programming languages (.cs, .js, .ts, .py, .java, etc.)
  - Computes normalized hashes for similarity detection
  - Groups duplicated code fragments
  - Suggests method names for refactoring

### 2. Code Duplication Detection Trigger
- **Location**: `csharp/Platform.Bot/Triggers/CodeDuplicationDetectionTrigger.cs`
- **Purpose**: Responds to issues requesting code duplication analysis
- **Features**:
  - Triggered by issues containing "find repeated code"
  - Creates pull requests for each significant duplication
  - Posts analysis results as issue comments
  - Asks reviewers for feedback on method names and placement

### 3. Branch Monitoring Trigger
- **Location**: `csharp/Platform.Bot/Triggers/CodeDuplicationBranchMonitorTrigger.cs` 
- **Purpose**: Monitors pull requests created for duplication fixes
- **Features**:
  - Detects when the default branch is updated
  - Notifies about potential conflicts
  - Tracks pull request status

### 4. Helper Service
- **Location**: `csharp/Platform.Bot/Services/FileStorageHelperService.cs`
- **Purpose**: Provides simple file storage for tracking bot state
- **Features**:
  - Stores duplication information
  - Tracks branch update timestamps
  - Persists pull request metadata

## How It Works

1. **Issue Creation**: Create an issue with title containing "find repeated code"
2. **Analysis**: Bot analyzes the repository for code duplications
3. **Pull Request Creation**: Bot creates pull requests for top duplications
4. **Monitoring**: Bot monitors pull requests for branch updates
5. **Feedback**: Bot asks for reviewer input on refactoring decisions

## Example Issue Title
```
Find the most repeated code fragments code and AST snapshot of a repository
```

## Integration Points

The bot integrates with:
- GitHub Issues API (for triggering analysis)
- GitHub Pull Requests API (for creating fixes)
- GitHub Repository Content API (for code analysis)
- Local file storage (for state tracking)

## Configuration

The bot uses the existing Platform.Bot architecture and is automatically enabled when the bot starts with the new triggers included.