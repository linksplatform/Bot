using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace TraderBot.Examples
{
    /// <summary>
    /// This example demonstrates how the tracking ID logging would work
    /// when the Tinkoff API returns the x-tracking-id header
    /// </summary>
    public class TrackingIdExample
    {
        private readonly ILogger<TrackingIdExample> _logger;

        public TrackingIdExample(ILogger<TrackingIdExample> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Example of how tracking ID is extracted and logged
        /// This simulates what happens in the actual PlaceBuyOrder/PlaceSellOrder/CancelOrder methods
        /// </summary>
        public void SimulateTrackingIdLogging()
        {
            // Simulate a response with x-tracking-id header
            var metadata = new Metadata
            {
                { "x-tracking-id", "550e8400-e29b-41d4-a716-446655440000" }
            };

            // Extract tracking ID (as done in the actual implementation)
            var trackingId = metadata.GetValue("x-tracking-id");

            // Log the tracking ID (as done in the actual implementation)
            _logger.LogInformation($"Buy order placed: [order response details]");
            if (!string.IsNullOrEmpty(trackingId))
            {
                _logger.LogInformation($"Tinkoff request tracking ID: {trackingId}");
            }

            Console.WriteLine($"Tracking ID logged: {trackingId}");
        }
    }
}