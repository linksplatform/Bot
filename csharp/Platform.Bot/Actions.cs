using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Storage.Local;
using File = System.IO.File;

namespace Platform.Bot;

public static class Actions
{
    public static async Task RemoveProjectsFromSln(string slnFilePath, string projectPattern)
    {
        // Note that string passed to regex constructor is escaping double qoutes by using double quotes.
        // Origin regex: Project.+?cpp\\.+?\"(?<projectId>\{.+?\})\"
        var cppProjectRegex = new Regex($@"Project.+?{projectPattern}\\.+?\""(?<projectId>\{{.+?\}})\""");
        var slnLines = await File.ReadAllLinesAsync(slnFilePath);
        List<string> projectIds = new();
        for (var i = 0; i < slnLines.Length; i++)
        {
            foreach (var projectId in projectIds)
            {
                if (slnLines[i].Contains(projectId))
                {
                    slnLines[i] = "";
                }
            }
            var match = cppProjectRegex.Match(slnLines[i]);
            if (!match.Success)
            {
                continue;
            }
            var currentCppProjectId = match.Groups["projectId"].Value;
            projectIds.Add(currentCppProjectId);
            slnLines[i] = "";
            // Remove EndProject
            ++i;
            slnLines[i] = "";
        }
        slnLines = slnLines.Where(line => line != "").ToArray();
        await File.WriteAllLinesAsync(slnFilePath, slnLines);
    }
}
