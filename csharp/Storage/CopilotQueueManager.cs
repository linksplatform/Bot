using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Bot.Interfaces;

namespace Storage
{
    /// <summary>
    /// Manages the GitHub Copilot request queue processing.
    /// </summary>
    public class CopilotQueueManager : IDisposable
    {
        private readonly IDataService _dataService;
        private readonly ILogger<CopilotQueueManager>? _logger;
        private readonly Timer _processingTimer;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private bool _disposed = false;

        /// <summary>
        /// Event triggered when a request is ready to be processed.
        /// </summary>
        public event Func<CopilotRequest, Task<string>>? RequestProcessor;

        /// <summary>
        /// Gets the processing interval for checking the queue.
        /// </summary>
        public TimeSpan ProcessingInterval { get; }

        /// <summary>
        /// Gets the cleanup interval for removing old requests.
        /// </summary>
        public TimeSpan CleanupInterval { get; }

        /// <summary>
        /// Gets the maximum age for completed requests before cleanup.
        /// </summary>
        public TimeSpan MaxRequestAge { get; }

        /// <summary>
        /// Initializes a new instance of the CopilotQueueManager.
        /// </summary>
        /// <param name="dataService">The data service for queue operations.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <param name="processingInterval">How often to check for new requests (default: 5 seconds).</param>
        /// <param name="cleanupInterval">How often to cleanup old requests (default: 1 hour).</param>
        /// <param name="maxRequestAge">Maximum age for completed requests (default: 24 hours).</param>
        public CopilotQueueManager(
            IDataService dataService,
            ILogger<CopilotQueueManager>? logger = null,
            TimeSpan? processingInterval = null,
            TimeSpan? cleanupInterval = null,
            TimeSpan? maxRequestAge = null)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = logger;
            ProcessingInterval = processingInterval ?? TimeSpan.FromSeconds(5);
            CleanupInterval = cleanupInterval ?? TimeSpan.FromHours(1);
            MaxRequestAge = maxRequestAge ?? TimeSpan.FromDays(1);

            // Start the processing timer
            _processingTimer = new Timer(ProcessQueueCallback, null, ProcessingInterval, ProcessingInterval);
            
            // Start the cleanup timer
            _cleanupTimer = new Timer(CleanupCallback, null, CleanupInterval, CleanupInterval);

            _logger?.LogInformation(
                "CopilotQueueManager initialized with ProcessingInterval={ProcessingInterval}, " +
                "CleanupInterval={CleanupInterval}, MaxRequestAge={MaxRequestAge}",
                ProcessingInterval, CleanupInterval, MaxRequestAge);
        }

        /// <summary>
        /// Starts the queue manager.
        /// </summary>
        public void Start()
        {
            _logger?.LogInformation("CopilotQueueManager started");
        }

        /// <summary>
        /// Stops the queue manager.
        /// </summary>
        public void Stop()
        {
            _processingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _logger?.LogInformation("CopilotQueueManager stopped");
        }

        /// <summary>
        /// Gets statistics about the current queue state.
        /// </summary>
        public async Task<QueueStatistics> GetStatisticsAsync()
        {
            var queueLength = await _dataService.GetQueueLengthAsync();
            return new QueueStatistics
            {
                PendingRequests = queueLength,
                ProcessingInterval = ProcessingInterval,
                LastProcessedAt = DateTime.UtcNow, // This would be tracked properly in a full implementation
                IsProcessing = _processingLock.CurrentCount == 0
            };
        }

        private async void ProcessQueueCallback(object? state)
        {
            if (_disposed) return;

            try
            {
                await _processingLock.WaitAsync();

                var request = await _dataService.DequeueCopilotRequestAsync();
                if (request != null)
                {
                    _logger?.LogInformation(
                        "Processing Copilot request {RequestId} for user {UserId} with language {Language}",
                        request.RequestId, request.UserId, request.Language);

                    await ProcessRequestAsync(request);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing queue");
            }
            finally
            {
                _processingLock.Release();
            }
        }

        private async Task ProcessRequestAsync(CopilotRequest request)
        {
            try
            {
                if (RequestProcessor != null)
                {
                    var result = await RequestProcessor(request);
                    await _dataService.CompleteRequestAsync(request.RequestId, result);
                    
                    _logger?.LogInformation(
                        "Successfully completed Copilot request {RequestId} for user {UserId}",
                        request.RequestId, request.UserId);
                }
                else
                {
                    // If no processor is registered, mark as failed
                    await _dataService.CompleteRequestAsync(request.RequestId, "No processor available");
                    _logger?.LogWarning(
                        "No RequestProcessor registered, marking request {RequestId} as failed",
                        request.RequestId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, 
                    "Error processing Copilot request {RequestId} for user {UserId}",
                    request.RequestId, request.UserId);

                // Mark as failed
                await _dataService.CompleteRequestAsync(request.RequestId, $"Error: {ex.Message}");
            }
        }

        private async void CleanupCallback(object? state)
        {
            if (_disposed) return;

            try
            {
                var cleanedCount = await _dataService.CleanupOldRequestsAsync(MaxRequestAge);
                if (cleanedCount > 0)
                {
                    _logger?.LogInformation("Cleaned up {CleanedCount} old requests", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during cleanup");
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Stop();
                _processingTimer?.Dispose();
                _cleanupTimer?.Dispose();
                _processingLock?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics about the current state of the Copilot queue.
    /// </summary>
    public class QueueStatistics
    {
        public int PendingRequests { get; set; }
        public TimeSpan ProcessingInterval { get; set; }
        public DateTime LastProcessedAt { get; set; }
        public bool IsProcessing { get; set; }
    }
}