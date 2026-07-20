using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NetJs.Compiler;
using NetJs.Translator;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeAnalysisProject = Microsoft.CodeAnalysis.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;


TextWriter originalConsole = Console.Out;
TextWriter logWriter = new StringWriter();
using var twinWriter = TextWriter.Synchronized(new DuplicityWriter(originalConsole, logWriter));
Console.SetOut(twinWriter);

string dotnetPath = (await "where dotnet".CLI()).StdOut.Trim();
string dotnetVersion = (await "dotnet --version".CLI()).StdOut.Trim();
var dotnetSDKs = (await "dotnet --list-sdks".CLI()).StdOut.Trim();
var sdks = dotnetSDKs.Split('\r').Select(e => e.Trim());
var sdk = sdks.Last();
var match = Regex.Match(sdk, @"^([^\s]+)\s+\[([^\]]+)\]");
if (!match.Success)
    throw new InvalidOperationException("Expected forat of dotnet --list-sdks is \"{version} [{path}]\"");
var sdkVersion = match.Groups[1].Value;
var sdkPath = match.Groups[2].Value; ;
var dotnetFolder = Path.GetDirectoryName(dotnetPath) + "\\";
var directory = Directory.GetCurrentDirectory();
Console.WriteLine($"Using dotnet {dotnetVersion} @ {dotnetPath}. SDK {sdkVersion} @ {sdkPath}");

var rootCommand = new RootCommand("NetJs");

var doctorCommand = new Command("doctor", "Creates csproj files from dotnet runtime and aspnetcore from official dotnet repo");
doctorCommand.Aliases.Add("--doctor");
var fileArgument = new Argument<FileInfo>("file") { Description = "Path to the doctor.libraries.xml file" };
doctorCommand.SetAction(async parseResult =>
{
    FileInfo targetedFile = parseResult.GetValue(fileArgument)!;
    if (targetedFile.Exists)
    {
        string netJsPath = "E:\\Apps\\NetJs";
        SystemPrivateCoreLibProject.Generate(netJsPath);
        var doctorFile = File.ReadAllText(args[1]);
        var doc = XElement.Parse(doctorFile); // validate XML
        var projects = doc.Elements("Project");
        var doctor = new LibraryDoctor(netJsPath);
        List<string> projectFiles = new();
        var allProjects = projects.Select(p => p.Attribute("Include")!.Value.Replace("$(DotnetGitRoot)", doctor.DotnetGitRoot));

        foreach (var project in projects)
        {
            var projectFile = await doctor.Doctor(netJsPath, project, allProjects);
            projectFiles.Add(projectFile);
        }
        var netJsAll = $@"
<Project Sdk=""Microsoft.NET.Sdk.Razor"">
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
{string.Join("\r\n", projectFiles.Except(["$(NewLibrariesProjectRoot)System.Private.CoreLib.Generators\\NetJs.System.Private.CoreLib.Generators.csproj"]).Select(e => $"    <ProjectReference Include=\"{e}\" />"))}
  </ItemGroup>
</Project>
";
        File.WriteAllText($"{netJsPath}\\libraries\\NetJs.All\\NetJs.All.csproj", netJsAll);
    }
    else
    {
        Console.WriteLine("File not found");
    }
});


var watchCommand = new Command("watch", "Watch directory for file changes and build accordingly");
watchCommand.Aliases.Add("--watch");
watchCommand.SetAction(async parseResult =>
{
    MSBuildLocator.RegisterDefaults();

    var workspace = MSBuildWorkspace.Create();
    await Watch(directory);

    async Task Watch(string directory)
    {
        Dictionary<string, ProjectContext> contexts = new Dictionary<string, ProjectContext>();

        async Task<IEnumerable<(CodeAnalysisProject CodeAnalysis, MsBuildProject MsBuild)>> DiscoverProjects()
        {
            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            Console.WriteLine($"Scanning for projects in \"{directory}\"...");
            var projects = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
            List<(CodeAnalysisProject, MsBuildProject)> list = new();
            foreach (var path in projects)
            {
#if DEBUG
                //Resolves "Updating 'attribute' requires restarting the application caused by AssemblyInformationalVersionAttribute" in development environment
                if (path.Contains("\\dotnet\\") || path.Contains("\\3rdparty\\") || path.Contains("\\tools\\") || path.Contains("\\_Deprecated\\"))
                    continue;
#endif
                Console.WriteLine($"Enumerating project \"{path}\"...");
                try
                {
                    var codeAnalysisProject = workspace.CurrentSolution.Projects.FirstOrDefault(f => f.FilePath == path) ?? await workspace.OpenProjectAsync(path);
                    var msProject = new MsBuildProject(path, GetBuildProperties(), null, projectCollection);
                    list.Add((codeAnalysisProject, msProject));
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
            return list;
        }

        var projects = await DiscoverProjects();

        Console.WriteLine($"\r\n{projects.Count()} projects found in {directory}!");

        foreach (var _project in projects)
        {
            var project = _project;
            FileSystemWatcher razorWatcher = new FileSystemWatcher(Path.GetDirectoryName(project.MsBuild.FullPath)!);
            razorWatcher.NotifyFilter =
                 NotifyFilters.Attributes
                 | NotifyFilters.CreationTime
                 | NotifyFilters.DirectoryName
                 | NotifyFilters.FileName
                 | NotifyFilters.LastAccess
                 | NotifyFilters.LastWrite
                | NotifyFilters.Security
                | NotifyFilters.Size
                ;
            razorWatcher.Filter = "*.razor";
            razorWatcher.IncludeSubdirectories = true;
            razorWatcher.EnableRaisingEvents = true;
            razorWatcher.Changed += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"razorWatcher.Changed: {e.FullPath}, {e.ChangeType}");
            };
            razorWatcher.Created += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"razorWatcher.Created: {e.FullPath}, {e.ChangeType}");
            };
            razorWatcher.Renamed += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"razorWatcher.Renamed: {e.FullPath}, {e.ChangeType}");
            };

            FileSystemWatcher csWatcher = new FileSystemWatcher(Path.GetDirectoryName(project.MsBuild.FullPath)!);
            csWatcher.NotifyFilter =
                 NotifyFilters.Attributes
                 | NotifyFilters.CreationTime
                 | NotifyFilters.DirectoryName
                 | NotifyFilters.FileName
                 | NotifyFilters.LastAccess
                 | NotifyFilters.LastWrite
                | NotifyFilters.Security
                | NotifyFilters.Size
                ;
            csWatcher.Filter = "*.cs";
            csWatcher.IncludeSubdirectories = true;
            csWatcher.EnableRaisingEvents = true;
            csWatcher.Changed += (s, e) =>
            {
                if (e.FullPath.EndsWith(".g.cs"))
                    return;
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csWatcher.Changed: {e.FullPath}, {e.ChangeType}");
            };
            csWatcher.Created += (s, e) =>
            {
                if (e.FullPath.EndsWith(".g.cs"))
                    return;
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csWatcher.Created: {e.FullPath}, {e.ChangeType}");
            };
            csWatcher.Renamed += (s, e) =>
            {
                if (e.FullPath.EndsWith(".g.cs"))
                    return;
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csWatcher.Renamed: {e.FullPath}, {e.ChangeType}");
            };

            FileSystemWatcher csProjWatcher = new FileSystemWatcher(Path.GetDirectoryName(project.MsBuild.FullPath)!);
            csProjWatcher.NotifyFilter =
                 NotifyFilters.Attributes
                 | NotifyFilters.CreationTime
                 | NotifyFilters.DirectoryName
                 | NotifyFilters.FileName
                 | NotifyFilters.LastAccess
                 | NotifyFilters.LastWrite
                | NotifyFilters.Security
                | NotifyFilters.Size
                ;
            csProjWatcher.Filter = "*.csproj";
            csProjWatcher.IncludeSubdirectories = true;
            csProjWatcher.EnableRaisingEvents = true;
            csProjWatcher.Changed += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csProjWatcher.Changed: {e.FullPath}, {e.ChangeType}");
            };
            csProjWatcher.Created += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csProjWatcher.Created: {e.FullPath}, {e.ChangeType}");
            };
            csProjWatcher.Renamed += (s, e) =>
            {
                TryProcessProject(project.CodeAnalysis, project.MsBuild, $"csProjWatcher.Renamed: {e.FullPath}, {e.ChangeType}");
            };


            contexts[project.MsBuild.FullPath] = new ProjectContext(razorWatcher, csWatcher);
        }

        Console.WriteLine("\r\nWaiting for changes...");
        Thread.Sleep(Timeout.InfiniteTimeSpan);

        async void TryProcessProject(CodeAnalysisProject caProject, MsBuildProject msProject, string? why)
        {
            if (why != null)
                Console.WriteLine(why);
            //lock (msProject)
            {
                var context = contexts[msProject.FullPath];
                await context.Lock.WaitAsync();
                try
                {
                    if (context.LastProcessed == DateTime.MinValue || DateTime.Now - context.LastProcessed > TimeSpan.FromSeconds(5))
                    {
                        await $"Building \"{msProject.FullPath}\"...".ProfileAsync(async () =>
                        {
                            try
                            {
                                var wProject = new ProjectWrapper(caProject, msProject);
                                var translator = new Translator(dotnetPath, dotnetVersion, sdkPath, sdkVersion, wProject, new ProjectBinOutputProvider(wProject));
                                await translator.Build();
                            }
                            catch (Exception e)
                            {
                                while (e != null)
                                {
                                    Console.WriteLine(e.GetType().FullName);
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                    e = e.InnerException!;
                                }
                            }
                        });
                        Console.WriteLine("\r\nWaiting for changes...");
                        context.LastProcessed = DateTime.Now;
                    }
                }
                finally
                {
                    context.Lock.Release();
                }
            }
        }
    }
});

var buildCommand = new Command("build", "Build a project");
buildCommand.Aliases.Add("--build");
var projectOption = new Option<FileInfo>("--project") { Description = "Path to the project to build" };
projectOption.Aliases.Add("-p");
buildCommand.Options.Add(projectOption);
buildCommand.SetAction(async (parseResult, cancellationToken) =>
{
    MSBuildLocator.RegisterDefaults();

    string? csProjectFile = null;
    var projectFileInfo = parseResult.GetValue(projectOption);

    if (projectFileInfo != null)
    {
        csProjectFile = projectFileInfo.FullName;
    }
    else
    {
        var projects = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
        var project = (projects.Count() == 1 ? projects.FirstOrDefault() :
        projects.Count() > 1 ? throw new InvalidOperationException($"Multiple project file found in directory {directory}. Specify the one to build using --project") :
        throw new InvalidOperationException($"No project file found in directory {directory}"));
        csProjectFile = project;
    }
    //var csProjectFile = projectFile;
    var workspace = MSBuildWorkspace.Create();
    await "Building".ProfileAsync(async () =>
    {
        await Build();
    });
    async Task Build()
    {
        var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
        var codeAnalysisProject = await workspace.OpenProjectAsync(csProjectFile!);
        var msBuildProject = new MsBuildProject(csProjectFile, GetBuildProperties(), null, projectCollection);
        var wProject = new ProjectWrapper(codeAnalysisProject, msBuildProject);
        Translator translator = default!;
        try
        {
            translator = new Translator(dotnetPath, dotnetVersion, sdkPath, sdkVersion, wProject, new ProjectBinOutputProvider(wProject));
            //translator.LogTo = logWriter;
            await translator.Build();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("BUILD SUCCESS!");
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("BUILD ERROR!!!");
            while (e != null)
            {
                Console.WriteLine(e.GetType().FullName);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                e = e.InnerException!;
            }
            throw;
        }
        finally
        {
            var logFile = Path.Combine(translator.TempFolder, Path.GetFileNameWithoutExtension(csProjectFile)!, $"__build.log.txt");
            var directory = Path.GetDirectoryName(logFile);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);
            File.WriteAllText(logFile, logWriter.ToString());
        }
    }
});

rootCommand.Subcommands.Add(doctorCommand);
rootCommand.Subcommands.Add(watchCommand);
rootCommand.Subcommands.Add(buildCommand);
return await rootCommand.Parse(args).InvokeAsync();

Dictionary<string, string> GetBuildProperties()
{
    var globalProperties = new Dictionary<string, string>();
    globalProperties.Add("Configuration", "Debug");
    globalProperties.Add("Platform", "wasm");
    return globalProperties;
}
