# GitHub Copilot Queue System

This project implements a queue system for GitHub Copilot access using the Deep/Doublets associative storage system.

## Overview

The GitHub Copilot Queue System provides a scalable solution for managing code generation requests in a bot environment. It uses the Platform.Data.Doublets library to store and manage request data in an associative database.

## Architecture

### Core Components

1. **IDataService Interface** (`Interfaces/IDataService.cs`)
   - Defines the contract for data operations
   - Handles enqueuing, dequeuing, and managing copilot requests

2. **CopilotDataService** (`Storage/CopilotDataService.cs`)
   - Implementation of IDataService using Doublets storage
   - Manages request persistence and retrieval
   - Utilizes the existing FileStorage class for Doublets operations

3. **CopilotQueueManager** (`Storage/CopilotQueueManager.cs`)
   - Manages the processing workflow
   - Handles automatic request processing with configurable intervals
   - Provides cleanup of old requests

4. **CopilotQueueService** (`Storage/CopilotQueueService.cs`)
   - Background service for continuous queue processing
   - Integrates with Microsoft.Extensions.Hosting
   - Provides mock code generation for demonstration

5. **CopilotIntegration** (`Platform.Bot/CopilotIntegration.cs`)
   - Bot integration layer
   - Provides user-friendly methods for chat bots
   - Handles request validation and user limits

## Features

- **Queue Management**: FIFO queue for processing requests
- **User Limits**: Configurable limits on pending requests per user
- **Status Tracking**: Real-time status updates for requests
- **Automatic Cleanup**: Periodic cleanup of old completed requests
- **Language Support**: Support for multiple programming languages
- **Statistics**: Queue statistics and monitoring
- **Error Handling**: Comprehensive error handling and logging

## Usage

### Basic Usage

```csharp
// Initialize the data service
var dataService = new CopilotDataService("copilot_queue.db");

// Create queue manager
var queueManager = new CopilotQueueManager(dataService, logger);

// Enqueue a request
var requestId = await dataService.EnqueueCopilotRequestAsync(
    userId: 1001, 
    language: "Python", 
    prompt: "Create a function to calculate fibonacci numbers",
    timestamp: DateTime.UtcNow
);

// Check queue status
var position = await dataService.GetQueuePositionAsync(requestId);
var queueLength = await dataService.GetQueueLengthAsync();
```

### Bot Integration

```csharp
var integration = new CopilotIntegration(dataService, queueManager);

// Handle user request
var response = await integration.HandleCopilotRequestAsync(
    userId: 1001,
    language: "Python", 
    prompt: "Create a REST API endpoint"
);

// Get user's pending requests
var pendingRequests = await integration.GetUserPendingRequestsAsync(1001);
```

### Running the Demo

```bash
cd csharp/Storage
dotnet run
```

## Configuration

The queue system is highly configurable:

- **Processing Interval**: How often to check for new requests (default: 5 seconds)
- **Cleanup Interval**: How often to cleanup old requests (default: 1 hour)
- **Max Request Age**: Maximum age for completed requests (default: 24 hours)
- **User Request Limit**: Maximum pending requests per user (default: 3)

## Supported Languages

- Python
- JavaScript/TypeScript
- C#
- Java
- Go
- Rust
- C/C++
- PHP
- Ruby
- Kotlin

## Database Schema

The system uses Doublets associative storage with the following markers:

- `CopilotRequest`: Marks a link as a copilot request
- `CopilotQueue`: Manages the queue structure
- `UserId`, `Language`, `Prompt`, `Timestamp`: Request properties
- `Status`, `Result`, `CompletedAt`: Processing state
- Status markers: `Pending`, `Processing`, `Completed`, `Failed`

## Integration with Existing Systems

The queue system is designed to integrate with existing bot frameworks:

### VK Bot (Python)
The existing Python VK bot can call the C# service via HTTP API or direct process communication.

### Discord Bot
Can be integrated using the CopilotIntegration class within Discord.NET or similar frameworks.

### GitHub Bot
Can be used for automated code reviews and suggestions.

## Benefits of Using Doublets Storage

1. **Associative Model**: Natural representation of relationships between entities
2. **Scalability**: Efficient storage and retrieval of complex data structures
3. **Flexibility**: Easy to extend with new properties and relationships
4. **Performance**: Optimized for link-based operations
5. **Consistency**: ACID properties for reliable data management

## Future Enhancements

1. **Real Copilot Integration**: Replace mock responses with actual GitHub Copilot API calls
2. **Priority Queue**: Allow priority-based request processing
3. **Load Balancing**: Distribute requests across multiple processing nodes
4. **Analytics**: Advanced analytics and usage tracking
5. **Rate Limiting**: More sophisticated rate limiting strategies
6. **Webhook Integration**: Real-time notifications for request completion

## Dependencies

- Platform.Data.Doublets.Sequences: Doublets storage engine
- Microsoft.Extensions.Hosting: Background service support
- Microsoft.Extensions.Logging: Logging infrastructure
- Microsoft.Extensions.DependencyInjection: Dependency injection

## License

This project follows the same license as the main Bot repository.