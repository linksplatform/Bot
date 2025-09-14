using System;
using System.Collections.Generic;
using System.Linq;

namespace Storage.Local
{
    /// <summary>
    /// <para>
    /// Manages the whitelist of supported domains for user links.
    /// </para>
    /// <para></para>
    /// </summary>
    public static class DomainWhitelist
    {
        /// <summary>
        /// <para>
        /// Dictionary mapping platform names to their allowed domain patterns.
        /// </para>
        /// <para></para>
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> AllowedDomains = new()
        {
            ["GitHub"] = new HashSet<string> { "github.com" },
            ["StackOverflow"] = new HashSet<string> { "stackoverflow.com", "serverfault.com", "superuser.com", "askubuntu.com", "mathoverflow.net" },
            ["GitLab"] = new HashSet<string> { "gitlab.com" }
        };

        /// <summary>
        /// <para>
        /// Gets all supported platform names.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>A collection of supported platform names.</para>
        /// <para></para>
        /// </returns>
        public static IEnumerable<string> GetSupportedPlatforms() => AllowedDomains.Keys;

        /// <summary>
        /// <para>
        /// Validates if a URL is allowed for the specified platform.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="platform">
        /// <para>The platform name.</para>
        /// <para></para>
        /// </param>
        /// <param name="url">
        /// <para>The URL to validate.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the URL is allowed for the platform, false otherwise.</para>
        /// <para></para>
        /// </returns>
        public static bool IsUrlAllowed(string platform, string url)
        {
            if (!AllowedDomains.ContainsKey(platform))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            var domain = uri.Host.ToLowerInvariant();
            return AllowedDomains[platform].Any(allowedDomain => 
                domain == allowedDomain || domain.EndsWith("." + allowedDomain));
        }

        /// <summary>
        /// <para>
        /// Gets the allowed domains for a specific platform.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="platform">
        /// <para>The platform name.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A collection of allowed domain patterns for the platform.</para>
        /// <para></para>
        /// </returns>
        public static IEnumerable<string> GetAllowedDomains(string platform)
        {
            return AllowedDomains.ContainsKey(platform) 
                ? AllowedDomains[platform] 
                : Enumerable.Empty<string>();
        }
    }
}