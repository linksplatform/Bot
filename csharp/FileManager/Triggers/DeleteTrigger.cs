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
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "delete";

        /// <summary>
        /// <para>
        /// Actions the arguments.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        public void Action(Context arguments) => arguments.FileStorage.Delete(ulong.Parse(arguments.Args[1]));
    }
}
