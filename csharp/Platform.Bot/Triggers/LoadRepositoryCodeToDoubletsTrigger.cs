using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers;

public class LoadRepositoryCodeToDoubletsTrigger : ITrigger<GitHubCommit>
{
    private readonly GitHubStorage _githubStorage;
    private readonly FileStorage _linksStorage;
    private readonly string _organizationName;

    public LoadRepositoryCodeToDoubletsTrigger(GitHubStorage githubStorage, FileStorage linksStorage, string organizationName)
    {
        _githubStorage = githubStorage;
        _linksStorage = linksStorage;
        _organizationName = organizationName;
    }

    public async Task<bool> Condition(GitHubCommit commit)
    {
        // Trigger on every commit to default branch
        // We could add more conditions here, like only for specific file extensions, etc.
        return true;
    }

    public async Task Action(GitHubCommit commit)
    {
        try
        {
            var repository = await _githubStorage.Client.Repository.Get(commit.Repository.Id);
            
            // Create or get file set for this repository
            var fileSetName = $"{_organizationName}/{repository.Name}";
            var fileSet = _linksStorage.GetFileSet(fileSetName);
            if (fileSet == 0)
            {
                fileSet = _linksStorage.CreateFileSet(fileSetName);
                Console.WriteLine($"Created file set for repository: {fileSetName}");
            }

            // Load all repository content into Doublets store
            await LoadRepositoryContentRecursively(repository.Id, "", fileSet, repository.DefaultBranch);

            Console.WriteLine($"Repository code loaded to Doublets store: {fileSetName} (commit: {commit.Sha[..7]})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading repository code to Doublets store: {ex.Message}");
        }
    }

    private async Task LoadRepositoryContentRecursively(long repositoryId, string path, ulong fileSet, string branch)
    {
        try
        {
            var contents = await _githubStorage.GetAllContentsByRef(repositoryId, path, branch);
            
            foreach (var content in contents)
            {
                if (content.Type == ContentType.File)
                {
                    // Skip binary files and very large files
                    if (IsBinaryFile(content.Name) || content.Size > 1024 * 1024) // Skip files > 1MB
                    {
                        continue;
                    }

                    try
                    {
                        // Get file content (it's base64 encoded)
                        var fileContent = content.Content;
                        if (!string.IsNullOrEmpty(fileContent))
                        {
                            // Decode base64 content
                            var decodedContent = Encoding.UTF8.GetString(Convert.FromBase64String(fileContent));
                            
                            // Store file in Doublets store
                            var file = _linksStorage.AddFile(decodedContent);
                            _linksStorage.AddFileToSet(fileSet, file, content.Path);
                            
                            Console.WriteLine($"Loaded file: {content.Path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading file {content.Path}: {ex.Message}");
                    }
                }
                else if (content.Type == ContentType.Dir)
                {
                    // Recursively load directory contents
                    await LoadRepositoryContentRecursively(repositoryId, content.Path, fileSet, branch);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading directory {path}: {ex.Message}");
        }
    }

    private bool IsBinaryFile(string fileName)
    {
        var binaryExtensions = new[]
        {
            ".exe", ".dll", ".bin", ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", 
            ".zip", ".tar", ".gz", ".7z", ".rar", ".ico", ".svg", ".woff", ".woff2",
            ".ttf", ".otf", ".eot", ".mp3", ".mp4", ".avi", ".mov", ".wmv", ".wav",
            ".ogg", ".flac", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
        };

        var extension = System.IO.Path.GetExtension(fileName).ToLower();
        return binaryExtensions.Contains(extension);
    }
}