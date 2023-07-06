using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Timestamps;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers;

public class CreateAndSaveOrganizationRepositoriesMigrationTrigger : ITrigger<DateTime?>
{
    private readonly GitHubStorage _githubStorage;

    private readonly FileStorage _linksStorage;

    private string _directoryPath;

    public CreateAndSaveOrganizationRepositoriesMigrationTrigger(GitHubStorage githubStorage, FileStorage linksStorage, string directoryPath)
    {
        _githubStorage = githubStorage;
        _linksStorage = linksStorage;
        _directoryPath = directoryPath;

    }
    public async Task<bool> Condition(DateTime? dateTime)
    {
        var allMigrations = _githubStorage.GetAllMigrations("linksplatform");
        if (allMigrations.Count == 0)
        {
            return true;
        }
        var lastMigrationTimestamp = Convert.ToDateTime(allMigrations.Last().CreatedAt);
        var timeAfterLastMigration = DateTime.Now - lastMigrationTimestamp;
        return timeAfterLastMigration.Days > 1;
    }

    public async Task Action(DateTime? dateTime)
    {
        var repositoryNames = _githubStorage.GetAllRepositories("linksplatform").Result.Select(repository => repository.Name).ToList();
        var createMigrationResult = await _githubStorage.CreateMigration("linksplatform", repositoryNames);
        if (null == createMigrationResult || createMigrationResult.State.Value == Migration.MigrationState.Failed)
        {
            Console.WriteLine("Migration is failed.");
            return;
        }
        Console.WriteLine($"Saving migration {createMigrationResult.Id}.");
        var fileName = Path.Combine(_directoryPath, $"migration_{createMigrationResult.Id}");
        await _githubStorage.SaveMigrationArchive("linksplatform", createMigrationResult.Id, fileName);
        Console.WriteLine($"Migration {createMigrationResult.Id} is saved.");
    }
}
