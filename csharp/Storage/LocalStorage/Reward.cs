using System;

namespace Storage.Local
{
    /// <summary>
    /// <para>
    /// Represents a reward linked to an issue.
    /// </para>
    /// <para></para>
    /// </summary>
    public class Reward
    {
        /// <summary>
        /// <para>
        /// Gets or sets the issue URL.
        /// </para>
        /// <para></para>
        /// </summary>
        public string IssueUrl { get; set; } = string.Empty;

        /// <summary>
        /// <para>
        /// Gets or sets the reward description.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// <para>
        /// Gets or sets the user who added the reward.
        /// </para>
        /// <para></para>
        /// </summary>
        public string AddedBy { get; set; } = string.Empty;

        /// <summary>
        /// <para>
        /// Gets or sets the date when the reward was added.
        /// </para>
        /// <para></para>
        /// </summary>
        public DateTime AddedDate { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets whether the reward is still active.
        /// </para>
        /// <para></para>
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}