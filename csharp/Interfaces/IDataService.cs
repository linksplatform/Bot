using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Interfaces
{
    /// <summary>
    /// Interface for data service operations, particularly for managing GitHub Copilot requests.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Enqueues a GitHub Copilot request.
        /// </summary>
        /// <param name="userId">The user ID making the request.</param>
        /// <param name="language">The programming language for the code generation.</param>
        /// <param name="prompt">The code generation prompt.</param>
        /// <param name="timestamp">When the request was made.</param>
        /// <returns>The queue position or identifier.</returns>
        Task<ulong> EnqueueCopilotRequestAsync(ulong userId, string language, string prompt, DateTime timestamp);

        /// <summary>
        /// Dequeues the next GitHub Copilot request for processing.
        /// </summary>
        /// <returns>The next copilot request, or null if queue is empty.</returns>
        Task<CopilotRequest?> DequeueCopilotRequestAsync();

        /// <summary>
        /// Gets all pending GitHub Copilot requests for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>List of pending requests for the user.</returns>
        Task<IReadOnlyList<CopilotRequest>> GetUserPendingRequestsAsync(ulong userId);

        /// <summary>
        /// Gets the queue position for a specific request.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <returns>The position in queue (0-based), or -1 if not found.</returns>
        Task<int> GetQueuePositionAsync(ulong requestId);

        /// <summary>
        /// Marks a request as completed.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="result">The generated code result.</param>
        /// <returns>True if successfully marked as completed.</returns>
        Task<bool> CompleteRequestAsync(ulong requestId, string result);

        /// <summary>
        /// Gets the total number of pending requests in the queue.
        /// </summary>
        /// <returns>Number of pending requests.</returns>
        Task<int> GetQueueLengthAsync();

        /// <summary>
        /// Cleans up old completed requests older than the specified timespan.
        /// </summary>
        /// <param name="maxAge">Maximum age for keeping completed requests.</param>
        /// <returns>Number of requests cleaned up.</returns>
        Task<int> CleanupOldRequestsAsync(TimeSpan maxAge);
    }

    /// <summary>
    /// Represents a GitHub Copilot code generation request.
    /// </summary>
    public class CopilotRequest
    {
        public ulong RequestId { get; set; }
        public ulong UserId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public CopilotRequestStatus Status { get; set; }
        public string? Result { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Status of a GitHub Copilot request.
    /// </summary>
    public enum CopilotRequestStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}