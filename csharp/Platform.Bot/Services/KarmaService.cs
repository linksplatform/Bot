using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Threading.Tasks;
using Octokit;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Service for managing user karma.
    /// </para>
    /// <para></para>
    /// </summary>
    public class KarmaService
    {
        private readonly FileStorage _fileStorage;
        private readonly GitHubStorage _gitHubStorage;
        private const int MinimumKarmaForRewards = 10; // Default minimum karma

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="KarmaService"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="fileStorage">The file storage.</param>
        /// <param name="gitHubStorage">The GitHub storage.</param>
        public KarmaService(FileStorage fileStorage, GitHubStorage gitHubStorage)
        {
            _fileStorage = fileStorage;
            _gitHubStorage = gitHubStorage;
        }

        /// <summary>
        /// <para>
        /// Checks if a user has sufficient karma to add rewards.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if user has sufficient karma.</returns>
        public bool HasSufficientKarma(string username)
        {
            var userKarma = _fileStorage.GetUserKarma(username);
            return userKarma?.KarmaPoints >= MinimumKarmaForRewards;
        }

        /// <summary>
        /// <para>
        /// Gets or initializes user karma.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user karma.</returns>
        public UserKarma GetOrInitializeUserKarma(string username)
        {
            var karma = _fileStorage.GetUserKarma(username);
            if (karma == null)
            {
                karma = new UserKarma
                {
                    Username = username,
                    KarmaPoints = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _fileStorage.SetUserKarma(karma);
            }
            return karma;
        }

        /// <summary>
        /// <para>
        /// Updates user karma points.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="points">Points to add (can be negative).</param>
        public void UpdateUserKarma(string username, int points)
        {
            var karma = GetOrInitializeUserKarma(username);
            karma.KarmaPoints += points;
            karma.LastUpdated = DateTime.UtcNow;
            _fileStorage.SetUserKarma(karma);
        }

        /// <summary>
        /// <para>
        /// Calculates and updates user karma based on GitHub activity.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>Updated karma points.</returns>
        public async Task<int> CalculateAndUpdateKarmaFromActivity(string username)
        {
            try
            {
                // This is a simplified karma calculation based on contribution activity
                // In a real implementation, you might want to calculate based on:
                // - Number of merged pull requests
                // - Number of issues resolved
                // - Repository stars/forks
                // - Community engagement

                var karma = GetOrInitializeUserKarma(username);
                var user = await _gitHubStorage.Client.User.Get(username);
                
                // Simple karma calculation based on public repos and followers
                // This is a basic implementation - you might want to enhance it
                var calculatedKarma = (user.PublicRepos * 2) + (user.Followers / 10);
                
                // Only update if the calculated karma is higher (to prevent karma loss)
                if (calculatedKarma > karma.KarmaPoints)
                {
                    karma.KarmaPoints = calculatedKarma;
                    karma.LastUpdated = DateTime.UtcNow;
                    _fileStorage.SetUserKarma(karma);
                }
                
                return karma.KarmaPoints;
            }
            catch (Exception)
            {
                // If we can't fetch user data, return existing karma
                var karma = GetOrInitializeUserKarma(username);
                return karma.KarmaPoints;
            }
        }

        /// <summary>
        /// <para>
        /// Gets the minimum karma required for adding rewards.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>The minimum karma value.</returns>
        public int GetMinimumKarmaForRewards() => MinimumKarmaForRewards;
    }
}