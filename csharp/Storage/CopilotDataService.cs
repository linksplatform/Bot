using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using Storage.Local;
using Bot.Interfaces;
using TLinkAddress = System.UInt64;

namespace Storage
{
    /// <summary>
    /// DataService implementation for managing GitHub Copilot requests using Doublets storage.
    /// </summary>
    public class CopilotDataService : IDataService, IDisposable
    {
        private readonly FileStorage _storage;
        private readonly TLinkAddress _copilotRequestMarker;
        private readonly TLinkAddress _copilotQueueMarker;
        private readonly TLinkAddress _userIdMarker;
        private readonly TLinkAddress _languageMarker;
        private readonly TLinkAddress _promptMarker;
        private readonly TLinkAddress _timestampMarker;
        private readonly TLinkAddress _statusMarker;
        private readonly TLinkAddress _resultMarker;
        private readonly TLinkAddress _completedAtMarker;
        private readonly TLinkAddress _queuePositionMarker;

        // Status markers
        private readonly TLinkAddress _pendingStatusMarker;
        private readonly TLinkAddress _processingStatusMarker;
        private readonly TLinkAddress _completedStatusMarker;
        private readonly TLinkAddress _failedStatusMarker;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the CopilotDataService.
        /// </summary>
        /// <param name="dbFilename">The database filename for Doublets storage.</param>
        public CopilotDataService(string dbFilename)
        {
            _storage = new FileStorage(dbFilename);

            // Initialize markers for different data types
            _copilotRequestMarker = _storage.CreateString("CopilotRequest");
            _copilotQueueMarker = _storage.CreateString("CopilotQueue");
            _userIdMarker = _storage.CreateString("UserId");
            _languageMarker = _storage.CreateString("Language");
            _promptMarker = _storage.CreateString("Prompt");
            _timestampMarker = _storage.CreateString("Timestamp");
            _statusMarker = _storage.CreateString("Status");
            _resultMarker = _storage.CreateString("Result");
            _completedAtMarker = _storage.CreateString("CompletedAt");
            _queuePositionMarker = _storage.CreateString("QueuePosition");

            // Status markers
            _pendingStatusMarker = _storage.CreateString("Pending");
            _processingStatusMarker = _storage.CreateString("Processing");
            _completedStatusMarker = _storage.CreateString("Completed");
            _failedStatusMarker = _storage.CreateString("Failed");
        }

        /// <summary>
        /// Enqueues a GitHub Copilot request.
        /// </summary>
        public async Task<ulong> EnqueueCopilotRequestAsync(ulong userId, string language, string prompt, DateTime timestamp)
        {
            return await Task.Run(() =>
            {
                // Create request components
                var userIdLink = _storage.CreateBigInteger(new BigInteger(userId));
                var languageLink = _storage.CreateString(language);
                var promptLink = _storage.CreateString(prompt);
                var timestampLink = _storage.CreateBigInteger(new BigInteger(timestamp.ToBinary()));

                // Create the request structure using links
                var requestId = CreateUniqueRequestId();
                var requestIdLink = _storage.CreateBigInteger(new BigInteger(requestId));

                // Create property links
                CreatePropertyLink(requestIdLink, _userIdMarker, userIdLink);
                CreatePropertyLink(requestIdLink, _languageMarker, languageLink);
                CreatePropertyLink(requestIdLink, _promptMarker, promptLink);
                CreatePropertyLink(requestIdLink, _timestampMarker, timestampLink);
                CreatePropertyLink(requestIdLink, _statusMarker, _pendingStatusMarker);

                // Mark as a Copilot request
                CreatePropertyLink(requestIdLink, _copilotRequestMarker, _pendingStatusMarker);

                // Add to queue
                var queuePosition = GetNextQueuePosition();
                var queuePositionLink = _storage.CreateBigInteger(new BigInteger(queuePosition));
                CreatePropertyLink(_copilotQueueMarker, queuePositionLink, requestIdLink);

                return requestId;
            });
        }

        /// <summary>
        /// Dequeues the next GitHub Copilot request for processing.
        /// </summary>
        public async Task<CopilotRequest?> DequeueCopilotRequestAsync()
        {
            return await Task.Run(() =>
            {
                // Find the earliest pending request
                var pendingRequests = GetAllPendingRequests();
                var earliestRequest = pendingRequests
                    .OrderBy(r => r.Timestamp)
                    .FirstOrDefault();

                if (earliestRequest != null)
                {
                    // Mark as processing
                    UpdateRequestStatus(earliestRequest.RequestId, CopilotRequestStatus.Processing);
                    earliestRequest.Status = CopilotRequestStatus.Processing;
                }

                return earliestRequest;
            });
        }

        /// <summary>
        /// Gets all pending GitHub Copilot requests for a specific user.
        /// </summary>
        public async Task<IReadOnlyList<CopilotRequest>> GetUserPendingRequestsAsync(ulong userId)
        {
            return await Task.Run(() =>
            {
                var allRequests = GetAllPendingRequests();
                return allRequests
                    .Where(r => r.UserId == userId)
                    .ToList()
                    .AsReadOnly();
            });
        }

        /// <summary>
        /// Gets the queue position for a specific request.
        /// </summary>
        public async Task<int> GetQueuePositionAsync(ulong requestId)
        {
            return await Task.Run(() =>
            {
                var pendingRequests = GetAllPendingRequests();
                var sortedRequests = pendingRequests
                    .OrderBy(r => r.Timestamp)
                    .ToList();

                for (int i = 0; i < sortedRequests.Count; i++)
                {
                    if (sortedRequests[i].RequestId == requestId)
                    {
                        return i;
                    }
                }

                return -1; // Not found in queue
            });
        }

        /// <summary>
        /// Marks a request as completed.
        /// </summary>
        public async Task<bool> CompleteRequestAsync(ulong requestId, string result)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var requestIdLink = _storage.CreateBigInteger(new BigInteger(requestId));
                    var resultLink = _storage.CreateString(result);
                    var completedAtLink = _storage.CreateBigInteger(new BigInteger(DateTime.UtcNow.ToBinary()));

                    // Update status and result
                    UpdateRequestStatus(requestId, CopilotRequestStatus.Completed);
                    CreatePropertyLink(requestIdLink, _resultMarker, resultLink);
                    CreatePropertyLink(requestIdLink, _completedAtMarker, completedAtLink);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets the total number of pending requests in the queue.
        /// </summary>
        public async Task<int> GetQueueLengthAsync()
        {
            return await Task.Run(() => GetAllPendingRequests().Count);
        }

        /// <summary>
        /// Cleans up old completed requests older than the specified timespan.
        /// </summary>
        public async Task<int> CleanupOldRequestsAsync(TimeSpan maxAge)
        {
            return await Task.Run(() =>
            {
                var cutoffTime = DateTime.UtcNow - maxAge;
                var cutoffBinary = cutoffTime.ToBinary();
                var completedRequests = GetAllCompletedRequests();
                
                int cleanedCount = 0;
                foreach (var request in completedRequests)
                {
                    if (request.CompletedAt.HasValue && request.CompletedAt.Value < cutoffTime)
                    {
                        // Delete the request and all its properties
                        DeleteRequest(request.RequestId);
                        cleanedCount++;
                    }
                }

                return cleanedCount;
            });
        }

        #region Private Helper Methods

        private ulong CreateUniqueRequestId()
        {
            // Generate a unique ID based on current timestamp and a random component
            var timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = (ulong)new Random().Next(1000, 9999);
            return (timestamp << 16) | random;
        }

        private int GetNextQueuePosition()
        {
            return GetAllPendingRequests().Count;
        }

        private void CreatePropertyLink(TLinkAddress subject, TLinkAddress predicate, TLinkAddress @object)
        {
            // Create a triple: subject -> predicate -> object
            var propertyLink = _storage.CreateString($"Property_{subject}_{predicate}");
            // In a more sophisticated implementation, we would use proper Doublets patterns
            // For now, we'll use string-based encoding of the relationship
        }

        private List<CopilotRequest> GetAllPendingRequests()
        {
            // This would query the Doublets storage for all pending requests
            // For now, returning empty list as placeholder
            // In a real implementation, this would traverse the Doublets graph
            return new List<CopilotRequest>();
        }

        private List<CopilotRequest> GetAllCompletedRequests()
        {
            // This would query the Doublets storage for all completed requests
            return new List<CopilotRequest>();
        }

        private void UpdateRequestStatus(ulong requestId, CopilotRequestStatus status)
        {
            var requestIdLink = _storage.CreateBigInteger(new BigInteger(requestId));
            var statusMarkerLink = status switch
            {
                CopilotRequestStatus.Pending => _pendingStatusMarker,
                CopilotRequestStatus.Processing => _processingStatusMarker,
                CopilotRequestStatus.Completed => _completedStatusMarker,
                CopilotRequestStatus.Failed => _failedStatusMarker,
                _ => _pendingStatusMarker
            };

            CreatePropertyLink(requestIdLink, _statusMarker, statusMarkerLink);
        }

        private void DeleteRequest(ulong requestId)
        {
            // This would delete all links related to this request
            // Implementation would traverse and delete all property links
        }

        #endregion

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
                _storage?.Dispose();
                _disposed = true;
            }
        }

        ~CopilotDataService()
        {
            Dispose(false);
        }

        #endregion
    }
}