using System.Collections.Generic;
using System.Threading;

namespace Interfaces
{
    interface ICodeStorage<TIssue,TClient>
    {
        public TClient client { get; set; }

        public string owner { get; set; }
        
        public IReadOnlyList<TIssue> GetIssues();

        public void CreateOrUpdateFile(string repository, string branch, IFile file);

        public void CloseIssue(TIssue issue);
    }
}
