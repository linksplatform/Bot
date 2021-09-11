using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace csharp
{
    /// <summary>
    /// <para>
    /// Represents the hello world trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Issue}"/>
    internal class HelloWorldTrigger : ITrigger<Issue>
    {
        /// <summary>
        /// <para>
        /// The git hub api.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly GitHubStorage gitHubAPI;

        /// <summary>
        /// <para>
        /// The file storage.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly FileStorage fileStorage;

        /// <summary>
        /// <para>
        /// The file set name.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly string fileSetName;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="HelloWorldTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="gitHubAPI">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileSetName">
        /// <para>A file set name.</para>
        /// <para></para>
        /// </param>
        public HelloWorldTrigger(GitHubStorage gitHubAPI, FileStorage fileStorage, string fileSetName)
        {
            this.gitHubAPI = gitHubAPI;
            this.fileStorage = fileStorage;
            this.fileSetName = fileSetName;
        }


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

        public void Action(Issue obj)
        {
            foreach (var file in fileStorage.GetFilesFromSet(fileSetName))
            {
                gitHubAPI.CreateOrUpdateFile(obj.Repository.Name, obj.Repository.DefaultBranch, file);
            }
            gitHubAPI.CloseIssue(obj);
        }


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
        public bool Condition(Issue obj) => obj.Title.ToLower() == "hello world";
    }
}
