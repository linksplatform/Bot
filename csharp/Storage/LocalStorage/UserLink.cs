namespace Storage.Local
{
    /// <summary>
    /// <para>
    /// Represents a user link with platform and URL information.
    /// </para>
    /// <para></para>
    /// </summary>
    public class UserLink
    {
        /// <summary>
        /// <para>
        /// Gets or sets the username associated with this link.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets the platform name (e.g., "GitHub", "StackOverflow", "GitLab").
        /// </para>
        /// <para></para>
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets the URL of the user's profile on the platform.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Url { get; set; }
    }
}