using Interfaces;
using System;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the links printer trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class LinksPrinterTrigger : ITrigger<Context>
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
        public bool Condition(Context context) => context.Args[0].ToLower() == "print";

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
        public void Action(Context context) => Console.WriteLine(context.FileStorage.AllLinksToString());
    }
}
