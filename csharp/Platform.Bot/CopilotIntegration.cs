using System;
using System.Threading.Tasks;
using Bot.Interfaces;
using Storage;

namespace Platform.Bot
{
    /// <summary>
    /// Integration class for GitHub Copilot functionality in the bot.
    /// </summary>
    public class CopilotIntegration
    {
        private readonly IDataService _dataService;
        private readonly CopilotQueueManager _queueManager;

        /// <summary>
        /// Initializes a new instance of the CopilotIntegration.
        /// </summary>
        /// <param name="dataService">The data service for managing requests.</param>
        /// <param name="queueManager">The queue manager for processing requests.</param>
        public CopilotIntegration(IDataService dataService, CopilotQueueManager queueManager)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
        }

        /// <summary>
        /// Handles a Copilot request from a user (e.g., from Discord, VK, or other chat platforms).
        /// </summary>
        /// <param name="userId">The user ID making the request.</param>
        /// <param name="language">The programming language.</param>
        /// <param name="prompt">The code generation prompt.</param>
        /// <returns>A message indicating the request status and queue position.</returns>
        public async Task<string> HandleCopilotRequestAsync(ulong userId, string language, string prompt)
        {
            try
            {
                // Check if user has too many pending requests
                var userPendingRequests = await _dataService.GetUserPendingRequestsAsync(userId);
                if (userPendingRequests.Count >= 3) // Limit to 3 pending requests per user
                {
                    return $"You already have {userPendingRequests.Count} pending requests. Please wait for them to complete before submitting new ones.";
                }

                // Enqueue the request
                var requestId = await _dataService.EnqueueCopilotRequestAsync(userId, language, prompt, DateTime.UtcNow);
                
                // Get queue position
                var position = await _dataService.GetQueuePositionAsync(requestId);
                var queueLength = await _dataService.GetQueueLengthAsync();

                if (position == 0)
                {
                    return $"✅ Your {language} code generation request has been queued and will be processed shortly. Request ID: {requestId}";
                }
                else
                {
                    return $"✅ Your {language} code generation request has been queued. You are #{position + 1} in line out of {queueLength} requests. Request ID: {requestId}";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Error processing your request: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the status of a specific request.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="requestId">The request ID.</param>
        /// <returns>Status message for the request.</returns>
        public async Task<string> GetRequestStatusAsync(ulong userId, ulong requestId)
        {
            try
            {
                var position = await _dataService.GetQueuePositionAsync(requestId);
                
                if (position == -1)
                {
                    return "Request not found or already completed.";
                }
                else if (position == 0)
                {
                    return $"Your request #{requestId} is currently being processed.";
                }
                else
                {
                    return $"Your request #{requestId} is #{position + 1} in the queue.";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Error checking request status: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets all pending requests for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>Summary of user's pending requests.</returns>
        public async Task<string> GetUserPendingRequestsAsync(ulong userId)
        {
            try
            {
                var requests = await _dataService.GetUserPendingRequestsAsync(userId);
                
                if (requests.Count == 0)
                {
                    return "You have no pending Copilot requests.";
                }

                var response = $"📋 You have {requests.Count} pending request(s):\n";
                foreach (var request in requests)
                {
                    var position = await _dataService.GetQueuePositionAsync(request.RequestId);
                    var status = position == -1 ? "Processing" : $"#{position + 1} in queue";
                    response += $"• Request #{request.RequestId}: {request.Language} - {status}\n";
                }

                return response.TrimEnd();
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving your requests: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets general queue statistics.
        /// </summary>
        /// <returns>Queue statistics message.</returns>
        public async Task<string> GetQueueStatsAsync()
        {
            try
            {
                var stats = await _queueManager.GetStatisticsAsync();
                
                return $"""
📊 **Copilot Queue Statistics**
• Pending requests: {stats.PendingRequests}
• Processing interval: {stats.ProcessingInterval.TotalSeconds}s
• Currently processing: {(stats.IsProcessing ? "Yes" : "No")}
• Last check: {stats.LastProcessedAt:HH:mm:ss}
""";
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving queue statistics: {ex.Message}";
            }
        }

        /// <summary>
        /// Validates if the specified programming language is supported.
        /// </summary>
        /// <param name="language">The programming language to validate.</param>
        /// <returns>True if supported, false otherwise.</returns>
        public static bool IsSupportedLanguage(string language)
        {
            var supportedLanguages = new[]
            {
                "python", "py",
                "javascript", "js",
                "typescript", "ts", 
                "csharp", "c#", "cs",
                "java",
                "go",
                "rust", "rs",
                "cpp", "c++",
                "c",
                "php",
                "ruby", "rb",
                "kotlin", "kt"
            };

            return Array.Exists(supportedLanguages, lang => 
                string.Equals(lang, language, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes the programming language name for consistency.
        /// </summary>
        /// <param name="language">The input language name.</param>
        /// <returns>Normalized language name.</returns>
        public static string NormalizeLanguage(string language)
        {
            return language.ToLowerInvariant() switch
            {
                "py" => "python",
                "js" => "javascript", 
                "ts" => "typescript",
                "cs" or "c#" => "csharp",
                "rs" => "rust",
                "rb" => "ruby",
                "kt" => "kotlin",
                "c++" => "cpp",
                _ => language.ToLowerInvariant()
            };
        }
    }
}