using Interfaces;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the delete trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class DeleteTrigger : ITrigger<Context>
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
        public bool Condition(Context context) => context.Args[0].ToLower() == "delete";

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
        public void Action(Context context) => context.FileStorage.Delete(ulong.Parse(context.Args[1]));
    }
}
