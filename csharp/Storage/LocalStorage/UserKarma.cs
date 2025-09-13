using System;

namespace Storage.Local
{
    /// <summary>
    /// <para>
    /// Represents user karma information.
    /// </para>
    /// <para></para>
    /// </summary>
    public class UserKarma
    {
        /// <summary>
        /// <para>
        /// Gets or sets the GitHub username.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// <para>
        /// Gets or sets the karma points.
        /// </para>
        /// <para></para>
        /// </summary>
        public int KarmaPoints { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets the last updated date.
        /// </para>
        /// <para></para>
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}