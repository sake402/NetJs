using System;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Threading;
using NetJs.Compiler;
using Project = Microsoft.Build.Evaluation.Project;
using Microsoft.Build.Locator;
using System.Collections.Generic;
using System.Linq;
using NetJs.Translator;
using System.Linq.Expressions;
using System.Globalization;
using System.Xml.Linq;

if (args.Length > 0 && args[0] == "--doctor")
{
    string dotnetJsPath = "E:\\Apps\\NetJs";
    SystemPrivateCoreLibProject.Generate(dotnetJsPath);
    var doctorFile = File.ReadAllText(args[1]);
    var doc = XElement.Parse(doctorFile); // validate XML
    var projects = doc.Elements("Project");
    var doctor = new LibraryDoctor(dotnetJsPath);
    List<string> projectFiles = new();
    foreach (var project in projects)
    {
        var projectFile = await doctor.Doctor(project);
        projectFiles.Add(projectFile);
    }
    var netJsAll = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
{string.Join("\r\n", projectFiles.Except(["$(NewLibrariesProjectRoot)System.Private.CoreLib.Generators\\NetJs.System.Private.CoreLib.Generators.csproj"]).Select(e => $"    <ProjectReference Include=\"{e}\" />"))}
  </ItemGroup>
</Project>
";
    File.WriteAllText($"{dotnetJsPath}\\libraries\\dotnetJs.All\\NetJs.All.csproj", netJsAll);
}
else if (args.Length > 0 && args[0] == "watch")
{
    MSBuildLocator.RegisterDefaults();
    var directory = Directory.GetCurrentDirectory();
    string dotnetPath = (await "where dotnet".CLI()).StdOut.Trim();
    string dotnetVersion = (await "dotnet --version".CLI()).StdOut.Trim();
    var dotnetSDKs = (await "dotnet --list-sdks".CLI()).StdOut.Trim();
    var sdks = dotnetSDKs.Split('\r').Last().Split(' ');
    var sdkVersion = sdks[0].Trim();
    var sdkPath = sdks[1].Trim('[', ']', ' ');
    var dotnetFolder = Path.GetDirectoryName(dotnetPath) + "\\";
    Console.WriteLine($"Using dotnet {dotnetVersion} @ {dotnetPath}. SDK {sdkVersion} @ {sdkPath}");

    Watch(directory);

    void Watch(string directory)
    {
        Dictionary<Project, ProjectContext> contexts = new Dictionary<Project, ProjectContext>();

        IEnumerable<Project> DiscoverProjects()
        {
            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            Console.WriteLine($"Scanning for projects in \"{directory}\"...");
            var projects = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
                .Select(path =>
                {
                    Console.WriteLine($"Enumerating project \"{path}\"...");
                    return new Project(path, GetBuildProperties(), null, projectCollection);
                })
                .ToList();
            return projects;
        }

        var projects = DiscoverProjects();

        Console.WriteLine($"\r\n{projects.Count()} projects found in {directory}!");
        //foreach (var project in projects)
        //{
        //    Console.WriteLine($"{project!.FullPath}");
        //}

        //var projectFolders = new string[] { @"E:\Apps\LivingThing\KitchenSink\BlazorJs.Core", @"E:\Apps\LivingThing\KitchenSink\BlazorJs.Sample", };


        foreach (var _project in projects)
        {
            var project = _project;
            FileSystemWatcher razorWatcher = new FileSystemWatcher(Path.GetDirectoryName(project!.FullPath)!);
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
                TryProcessProject(project);
            };
            razorWatcher.Created += (s, e) =>
            {
                TryProcessProject(project);
            };
            razorWatcher.Renamed += (s, e) =>
            {
                TryProcessProject(project);
            };

            FileSystemWatcher csWatcher = new FileSystemWatcher(Path.GetDirectoryName(project.FullPath)!);
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
                TryProcessProject(project);
            };
            csWatcher.Created += (s, e) =>
            {
                TryProcessProject(project);
            };
            csWatcher.Renamed += (s, e) =>
            {
                TryProcessProject(project);
            };

            FileSystemWatcher csProjWatcher = new FileSystemWatcher(Path.GetDirectoryName(project.FullPath)!);
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
                TryProcessProject(project);
            };
            csProjWatcher.Created += (s, e) =>
            {
                TryProcessProject(project);
            };
            csProjWatcher.Renamed += (s, e) =>
            {
                TryProcessProject(project);
            };


            contexts[project] = new ProjectContext(razorWatcher, csWatcher);
        }

        Console.WriteLine("\r\nWaiting for changes...");
        Thread.Sleep(Timeout.InfiniteTimeSpan);

        void TryProcessProject(Project project)
        {

            lock (project)
            {
                var context = contexts[project];
                if (context.LastProcessed == DateTime.MinValue || DateTime.Now - context.LastProcessed > TimeSpan.FromSeconds(5))
                {
                    try
                    {
                        var wProject = new ProjectWrapper(project);
                        Translator.Build(wProject, new ProjectBinOutputProvider(wProject));
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
                    Console.WriteLine("\r\nWaiting for changes...");
                    context.LastProcessed = DateTime.Now;
                }
            }
        }
    }
}
else if (args.Length > 0 && args[0] == "build")
{
    MSBuildLocator.RegisterDefaults();
    var directory = Directory.GetCurrentDirectory();
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

    Build();
    void Build()
    {
        var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
        var project = new Project(csProjectFile, GetBuildProperties(), null, projectCollection);
        var wProject = new ProjectWrapper(project);
        StringWriter logWriter = new StringWriter();
        var tempFolder = Path.GetTempPath() + "NetJs\\";
        try
        {
            Translator.Build(wProject, new ProjectBinOutputProvider(wProject), logTo: logWriter, tempFolder: tempFolder);
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
            var logFile = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(csProjectFile)!, $"__build.log.txt");
            var directory = Path.GetDirectoryName(logFile);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(logFile, logWriter.ToString());
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
