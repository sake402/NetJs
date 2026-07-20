using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NetJs.Compiler;
using NetJs.Translator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeAnalysisProject = Microsoft.CodeAnalysis.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

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

if (args.Length > 0 && args[0] == "--doctor")
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
else if (args.Length > 0 && args[0] == "watch")
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
        //foreach (var project in projects)
        //{
        //    Console.WriteLine($"{project!.FullPath}");
        //}

        //var projectFolders = new string[] { @"E:\Apps\LivingThing\KitchenSink\BlazorJs.Core", @"E:\Apps\LivingThing\KitchenSink\BlazorJs.Sample", };


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
                        await "Building".ProfileAsync(async () =>
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
}
else if (args.Length > 0 && args[0] == "build")
{
    MSBuildLocator.RegisterDefaults();
    var projects = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
    var projectIndex = args.IndexOf("--project");
    string? projectFile = null;
    if (projectIndex > 0)
    {
        projectFile = args[projectIndex + 1];
    }
    var csProjectFile = projectFile ??
        (projects.Count() == 1 ? projects.FirstOrDefault() :
        projects.Count() > 1 ? throw new InvalidOperationException($"Multiple project file found in directory {directory}. Specify the one to build using --project") :
        throw new InvalidOperationException($"No project file found in directory {directory}"));

    var workspace = MSBuildWorkspace.Create();
    await Build();
    async Task Build()
    {
        var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
        var codeAnalysisProject = await workspace.OpenProjectAsync(csProjectFile!);
        var msBuildProject = new MsBuildProject(csProjectFile, GetBuildProperties(), null, projectCollection);
        var wProject = new ProjectWrapper(codeAnalysisProject, msBuildProject);
        var originalWriter = new StringWriter();
        var logWriter = TextWriter.Synchronized(originalWriter);
        Translator translator = default!;
        try
        {
            translator = new Translator(dotnetPath, dotnetVersion, sdkPath, sdkVersion, wProject, new ProjectBinOutputProvider(wProject));
            translator.LogTo = logWriter;
            await translator.Build();
            logWriter.WriteLine("BUILD SUCCESS!");
        }
        catch (Exception e)
        {
            logWriter.WriteLine();
            logWriter.WriteLine();
            logWriter.WriteLine("BUILD ERROR!!!");
            while (e != null)
            {
                logWriter.WriteLine(e.GetType().FullName);
                logWriter.WriteLine(e.Message);
                logWriter.WriteLine(e.StackTrace);
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
            File.WriteAllText(logFile, originalWriter.ToString());
        }
    }
}

Dictionary<string, string> GetBuildProperties()
{
    var globalProperties = new Dictionary<string, string>();
    globalProperties.Add("Configuration", "Debug");
    globalProperties.Add("Platform", "wasm");
    return globalProperties;
}


struct Foo()
{
    public static Foo operator ++(Foo a)
    {
        return new Foo();
    }
    public static Foo operator +(Foo a, int i)
    {
        return new Foo();
    }
}
