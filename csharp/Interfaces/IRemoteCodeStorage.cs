using System;
using System.Collections.Generic;

namespace Interfaces
{
    public interface IRemoteCodeStorage<TIssue>
    {
        public TimeSpan MinimumInteractionInterval { get; set; }

        public IReadOnlyList<TIssue> GetIssues();

        public void CreateOrUpdateFile(string repository, string branch, IFile file);

        public void CloseIssue(TIssue issue);
    }
}
