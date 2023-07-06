using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Storage.Remote.GitHub;

namespace Interfaces
{
    /// <summary>
    /// <para>
    /// Represents the programmer role.
    /// </para>
    /// <para></para>
    /// </summary>
    public interface ITracker<TContext>
    {
        /// <summary>
        /// <para>
        /// Starts the cancellation token.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="cancellationToken">
        /// <para>The cancellation token.</para>
        /// <para></para>
        /// </param>
        public Task Start(CancellationToken cancellationToken);
    }
}
