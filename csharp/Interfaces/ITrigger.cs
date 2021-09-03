
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
        /// <param name="obj">
        /// <para>The obj.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(TContext obj);

        /// <summary>
        /// <para>
        /// Actions the obj.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="obj">
        /// <para>The obj.</para>
        /// <para></para>
        /// </param>
        public void Action(TContext obj);
    }
}
