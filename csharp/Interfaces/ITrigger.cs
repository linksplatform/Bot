
using System.Threading.Tasks;

namespace Interfaces
{
    /// <summary>
    /// <para>
    /// Defines the trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    public interface ITrigger<TContext>
    {
        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public Task<bool> Condition(TContext context);

        /// <summary>
        /// <para>
        /// Actions the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        public Task Action(TContext context);
    }
}
