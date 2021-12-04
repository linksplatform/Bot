using Storage.Local;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the context.
    /// </para>
    /// <para></para>
    /// </summary>
    public class Context
    {
        /// <summary>
        /// <para>
        /// Gets or sets the args value.
        /// </para>
        /// <para></para>
        /// </summary>
        public string[] Args { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets the file storage value.
        /// </para>
        /// <para></para>
        /// </summary>
        public FileStorage FileStorage { get; set; }
    }
}
