using System.Threading.Tasks;

namespace Interfaces
{
    /// <summary>
    /// <para>
    /// Defines a trigger that supports private messaging to users instead of public issue comments.
    /// </para>
    /// <para></para>
    /// </summary>
    public interface IPrivateMessageTrigger<TContext> : ITrigger<TContext>
    {
        /// <summary>
        /// <para>
        /// Gets the user login to send private message to.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The user login string</para>
        /// <para></para>
        /// </returns>
        string GetTargetUserLogin(TContext context);

        /// <summary>
        /// <para>
        /// Gets the subject for the private message.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The message subject</para>
        /// <para></para>
        /// </returns>
        string GetMessageSubject(TContext context);

        /// <summary>
        /// <para>
        /// Gets the private message content.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The private message content</para>
        /// <para></para>
        /// </returns>
        Task<string> GetPrivateMessageContent(TContext context);
    }
}