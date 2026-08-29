using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Options;
using NetJs.Compiler;
using NetJs.Translator;
using System;
using System.Collections.Concurrent;
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
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CodeAnalysisProject = Microsoft.CodeAnalysis.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

MSBuildLocator.RegisterDefaults();

TextWriter originalConsole = Console.Out;
TextWriter defaultLogWriter = new StringWriter();
var consoleWriter = new DuplicityWriter(originalConsole, defaultLogWriter);
using var duplicityWriter = TextWriter.Synchronized(consoleWriter);
Console.SetOut(duplicityWriter);


string dotnetPath = (await $"{(OperatingSystem.IsWindows() ? "where" : "which")} dotnet".CLI()).StdOut.Trim();
string dotnetVersion = (await "dotnet --version".CLI()).StdOut.Trim();
var dotnetSDKs = (await "dotnet --list-sdks".CLI()).StdOut.Trim();
var sdks = dotnetSDKs.Split('\r').Select(e => e.Trim());
var sdk = sdks.Last();
var match = Regex.Match(sdk, @"^([^\s]+)\s+\[([^\]]+)\]");
if (!match.Success)
    throw new InvalidOperationException("Expected format of dotnet --list-sdks is \"{version} [{path}]\"");
var sdkVersion = match.Groups[1].Value;
var sdkPath = match.Groups[2].Value; ;
var dotnetFolder = Path.GetDirectoryName(dotnetPath) + "\\";
var directory = Directory.GetCurrentDirectory();

string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetJs");
if (!Directory.Exists(dataFolder))
    Directory.CreateDirectory(dataFolder);
var tempFolder = Path.Combine(Path.GetTempPath(), "NetJs");

var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
var deSerializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

var config = new Config();
var configFile = Path.Combine(dataFolder, "config.yaml");
if (File.Exists(configFile))
{
    var yaml = File.ReadAllText(configFile);
    config = deSerializer.Deserialize<Config>(yaml);
}

Console.WriteLine();
Console.WriteLine($"Using dotnet {dotnetVersion} @ {dotnetPath}. SDK {sdkVersion} @ {sdkPath}");
Console.WriteLine($"Using Input Package: \"{config.InputPackageSource}\"");
Console.WriteLine($"Using Output Package: \"{config.OutputPackageSink}\"");
Console.WriteLine($"Using Temp Folder: \"{tempFolder}\"");
Console.WriteLine($"Using Data Folder: \"{dataFolder}\"");
Console.WriteLine();

var rootCommand = new RootCommand("NetJs");


var doctorCommand = new Command("doctor", "Creates csproj files from dotnet runtime and aspnetcore from official dotnet repo");
doctorCommand.Aliases.Add("--doctor");
var fileArgument = new Argument<FileInfo>("file") { Description = "Path to the doctor.libraries.xml file" };
doctorCommand.Add(fileArgument);
doctorCommand.SetAction(Doctor);


var watchCommand = new Command("watch", "Watch directory for file changes and build accordingly");
watchCommand.Aliases.Add("--watch");
watchCommand.SetAction(Watch);


var buildCommand = new Command("build", "Build a project");
buildCommand.Aliases.Add("--build");
var projectOption = new Option<FileInfo>("--project") { Description = "Path to the project to build" };
//projectOption.Aliases.Add("-p");
var configOption = new Option<FileInfo>("--configuration") { Description = "Build configuration. Debug/Release" };
configOption.Aliases.Add("-c");
var singleBuildOption = new Option<bool>("--single") { Description = "Build only this project" };
singleBuildOption.Aliases.Add("-s");
var forceBuildOption = new Option<bool>("--force") { Description = "Build this project even if up to date" };
forceBuildOption.Aliases.Add("-f");
var propertiesOption = new Option<Dictionary<string, string>>("--property") { Description = "Build properties", Arity = ArgumentArity.ZeroOrMore };
propertiesOption.Aliases.Add("-p");
propertiesOption.CustomParser = (result) =>
{
    var dictionary = new Dictionary<string, string>();
    foreach (var token in result.Tokens)
    {
        var parts = token.Value.Split('=', 2);
        if (parts.Length == 2)
        {
            // Overwrites if the same key is passed multiple times
            dictionary[parts[0]] = parts[1];
        }
        else
        {
            result.AddError($"Invalid format for property: '{token.Value}'. Use Key=Value.");
            return null!;
        }
    }
    return dictionary;
};
buildCommand.Options.Add(projectOption);
buildCommand.Options.Add(configOption);
buildCommand.Options.Add(singleBuildOption);
buildCommand.Options.Add(forceBuildOption);
buildCommand.Options.Add(propertiesOption);
buildCommand.SetAction(Build);

var cacheCommand = new Command("cache", "Manage package cache");

var cleanCommand = new Command("clean", "Clean the package cache folder in temp folder");
cleanCommand.SetAction(CleanPackageCache);

var pullCacheCommand = new Command("pull", "Pull the project package cache from github to this PC");
pullCacheCommand.Options.Add(projectOption);
pullCacheCommand.SetAction(PullPackageCache);

cacheCommand.Add(cleanCommand);
cacheCommand.Add(pullCacheCommand);

var configCommand = new Command("config", "Manage NetJs configuration");
var nameConfigOption = new Option<string>("--name") { Description = "Configuration name", Required = true };
nameConfigOption.Aliases.Add("-n");
var valueConfigOption = new Option<string>("--value") { Description = "Configuration value", Required = true };
valueConfigOption.Aliases.Add("-v");

var getConfigCommand = new Command("get", "Get a configuration");
configCommand.Add(getConfigCommand);
getConfigCommand.SetAction(GetConfig);

var setConfigCommand = new Command("set", "Set a configuration");
setConfigCommand.SetAction(SetConfig);
configCommand.Add(setConfigCommand);

getConfigCommand.Options.Add(nameConfigOption);
setConfigCommand.Options.Add(nameConfigOption);
setConfigCommand.Options.Add(valueConfigOption);

var serveCommand = new Command("serve", "Serve a project");
var serveDirectoryOption = new Option<string>("--folder") { Description = "Folder path to serve from" };
serveDirectoryOption.Aliases.Add("-f");
var servePortOption = new Option<int>("--port") { Description = "Port to use" };
servePortOption.Aliases.Add("-p");
serveCommand.Options.Add(servePortOption);
var serveOpenBrowserOption = new Option<bool>("--open") { Description = "Open Browser" };
serveOpenBrowserOption.Aliases.Add("-o");
serveCommand.Options.Add(serveOpenBrowserOption);

serveCommand.SetAction(Serve);

rootCommand.Subcommands.Add(doctorCommand);
rootCommand.Subcommands.Add(watchCommand);
rootCommand.Subcommands.Add(buildCommand);
rootCommand.Subcommands.Add(cacheCommand);
rootCommand.Subcommands.Add(configCommand);
rootCommand.Subcommands.Add(serveCommand);

return await rootCommand.Parse(args).InvokeAsync();

Dictionary<string, string> GetBuildProperties()
{
    var globalProperties = new Dictionary<string, string>();
    //globalProperties.Add("Configuration", "Debug");
    //if (addWasmPlatform)
    //globalProperties.Add("Platform", "wasm");
    return globalProperties;
}

async Task Doctor(ParseResult parseResult)
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
}

async Task Watch(ParseResult parseResult)
{
    var workspace = MSBuildWorkspace.Create();
    var buildProperties = GetBuildProperties();
    var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
    await Watch(directory);

    async Task Watch(string directory)
    {
        Dictionary<string, ProjectContext> contexts = new Dictionary<string, ProjectContext>();

        async Task<IEnumerable<(CodeAnalysisProject CodeAnalysis, MsBuildProject MsBuild)>> DiscoverProjects()
        {
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
                    var msProject = new MsBuildProject(path, buildProperties, null, projectCollection);
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
                                var wProject = new ProjectWrapper(workspace, projectCollection, caProject, msProject, buildProperties);
                                var translator = new Translator(config, dotnetPath, dotnetVersion, sdkPath, sdkVersion, dataFolder, tempFolder, wProject, new ProjectBinOutputProvider(wProject));
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

async Task Build(ParseResult parseResult, CancellationToken cancellationToken)
{
    string? csProjectFile = null;
    var projectFileInfo = parseResult.GetValue(projectOption);
    var singleBuild = parseResult.GetValue(singleBuildOption);
    var forceBuild = parseResult.GetValue(forceBuildOption);
    var buildProperties = parseResult.GetValue(propertiesOption);
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
    var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
    Dictionary<string, TaskCompletionSource<Exception?>> building = new();
    SemaphoreSlim slock = new(1);
    SemaphoreSlim maxParallelBuild = new(4);
    await "Building".ProfileAsync(async () =>
    {
        var exception = await BuildWithDependencies(csProjectFile!, singleBuild, forceBuild);
        if (exception != null)
            throw exception;
    });
    async Task<Exception?> BuildWithDependencies(string csProjectFile, bool singleBuild, bool forceBuild)
    {
        var projectFolder = Path.GetDirectoryName(csProjectFile)!;
        await slock.WaitAsync();
        if (building.TryGetValue(csProjectFile, out var tcs))
        {
            slock.Release();
            return await tcs.Task;
        }
        tcs = new TaskCompletionSource<Exception?>();
        building.Add(csProjectFile, tcs);
        var codeAnalysisProject = workspace.CurrentSolution.Projects.FirstOrDefault(f => f.FilePath?.Equals(csProjectFile, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? await workspace.OpenProjectAsync(csProjectFile!);
        slock.Release();
        Exception? result = null;
        do
        {
            var logDestination = defaultLogWriter;
            if (!singleBuild)
            {
                logDestination = new StringWriter();
                consoleWriter.AsyncLocalWriter.Value = logDestination;
            }
            var msBuildProject = new MsBuildProject(csProjectFile, buildProperties, null, projectCollection);
            bool dependencyBuilt = false;
            if (!singleBuild)
            {
                var csProjectDependencies = msBuildProject.Items.Where(i => i.ItemType == "ProjectReference" && !i.GetMetadataValue("OutputItemType").Equals("Analyzer", StringComparison.InvariantCultureIgnoreCase) && !i.GetMetadataValue("ReferenceOutputAssembly").Equals("false", StringComparison.InvariantCultureIgnoreCase)).Select(e => e.EvaluatedInclude);
                var tasks = new List<Task<Exception?>>();
                foreach (var _csProj in csProjectDependencies)
                {
                    var csProj = _csProj;
                    if (!Path.IsPathRooted(csProj))
                        csProj = Path.GetFullPath(Path.Combine(projectFolder, csProj));
                    var task = BuildWithDependencies(csProj!, singleBuild, forceBuild);
                    //if (!task.IsCompleted)
                    //{
                    //    dependencyBuilt = true;
                    //}
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
                var analyzerProjectDependencies = msBuildProject.Items.Where(i => i.ItemType == "ProjectReference" && i.GetMetadataValue("OutputItemType").Equals("Analyzer", StringComparison.InvariantCultureIgnoreCase)).Select(e => e.EvaluatedInclude);
                //if this project has a source generator, we need to do dotnet build on it first to make sure source generator run and generate their files
                //We know we are not invoked by dotnet itself as it passes --single through our Directory.Build.props
                if (analyzerProjectDependencies.Any())
                {
                    //Console.WriteLine();
                    Console.WriteLine($"Doing dotnet build \"{csProjectFile}\" to run source generator");
                    var c = await $"cd \"{projectFolder}\" && dotnet build /p:NoNetJs=true /p:EmitCompilerGeneratedFiles=true".CLI();
                    Console.WriteLine($"Done dotnet build \"{csProjectFile}\"");
                    if (c.ExitCode != 0)
                    {
                        result = new Exception(c.StdOut);
                        break;
                    }
                }
                var taskExceptions = tasks.Where(e => e.Exception != null).Select(e => e.Exception!);
                var exceptions = tasks.Where(e => e.Result != null).Select(e => e.Result!);
                if (taskExceptions.Any() || exceptions.Any())
                {
                    result = new AggregateException(taskExceptions.Concat(exceptions).ToArray());
                    break;
                }
            }
            //var sourceFiles = msBuildProject.Items.Where(i => i.ItemType == "Compile").Select(e =>
            //{
            //    var include = e.EvaluatedInclude;
            //    if (Path.IsPathRooted(include))
            //        return include;
            //    return Path.Join(projectFolder, include);
            //});
            var wProject = new ProjectWrapper(workspace, projectCollection, codeAnalysisProject, msBuildProject, buildProperties);
            var sources = wProject.GetSourceFiles();
            var objFolder = msBuildProject.Evaluate("IntermediateOutputPath");
            var timeStampFile = $"{objFolder}{Path.DirectorySeparatorChar}NetJsBuild.timestamp";
            if (!Path.IsPathRooted(timeStampFile))
                timeStampFile = Path.Combine(projectFolder, timeStampFile);
            DateTime? lastBuildTime = null;
            if (File.Exists(timeStampFile))
            {
                lastBuildTime = new FileInfo(timeStampFile).LastWriteTime;
            }
            if (forceBuild /*|| dependencyBuilt*/ || lastBuildTime == null || sources.Any(s => new FileInfo(s).LastWriteTime > lastBuildTime))
            {
                Translator translator = default!;
                try
                {
                    await maxParallelBuild.WaitAsync();
                    Console.WriteLine($"Building \"{csProjectFile}\"");
                    translator = new Translator(config, dotnetPath, dotnetVersion, sdkPath, sdkVersion, dataFolder, tempFolder, wProject, new ProjectBinOutputProvider(wProject));
                    //translator.LogTo = logWriter;
                    if (!await translator.Build())
                    {
                        result = new Exception("Build Failed");
                        break;
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"BUILD \"{csProjectFile}\" SUCCESS!");
                    if (!File.Exists(timeStampFile))
                    {
                        var dir = Path.GetDirectoryName(timeStampFile);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        var fs = File.Create(timeStampFile);
                        fs.Flush();
                        fs.Close();
                    }
                    var fi = new FileInfo(timeStampFile);
                    fi.LastWriteTime = DateTime.Now;
                }
                catch (Exception e)
                {
                    var ex = e;
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"BUILD \"{csProjectFile}\" ERROR!!!");
                    while (e != null)
                    {
                        Console.WriteLine(e.GetType().FullName);
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        e = e.InnerException!;
                    }
                    result = ex;
                }
                finally
                {
                    maxParallelBuild.Release();
                    var logFile = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(csProjectFile)!, $"__build.log.txt");
                    var directory = Path.GetDirectoryName(logFile);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory!);
                    File.WriteAllText(logFile, logDestination.ToString() + "\r\n" + result?.Message + "\r\n" + result?.StackTrace);
                }
            }
            else
            {
                Console.WriteLine($"Build \"{csProjectFile}\" up to date!");
                Console.WriteLine($"BUILD \"{csProjectFile}\" SUCCESS!");
            }
        } while (false);
        tcs.SetResult(result);
        return result;
    }
}

void CleanPackageCache(ParseResult parseResult)
{
    var deSerializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    var metadataProvider = new MetadataProvider(config, dotnetPath, dotnetVersion, sdkPath, sdkVersion, dataFolder, tempFolder, deSerializer);
    metadataProvider.CleanPackageCache();
}


async Task PullPackageCache(ParseResult parseResult)
{
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
    var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
    var codeAnalysisProject = await workspace.OpenProjectAsync(csProjectFile!);
    var msBuildProject = new MsBuildProject(csProjectFile, GetBuildProperties(), null, projectCollection);
    var wProject = new ProjectWrapper(workspace, projectCollection, codeAnalysisProject, msBuildProject, null);

    var metadataProvider = new MetadataProvider(config, dotnetPath, dotnetVersion, sdkPath, sdkVersion, dataFolder, tempFolder, deSerializer);
    await metadataProvider.PullPackageCache(wProject);
}

void SetConfig(ParseResult parseResult)
{
    var name = parseResult.GetValue(nameConfigOption) ?? "";
    var value = parseResult.GetValue(valueConfigOption) ?? "";
    if (name.Equals(nameof(config.InputPackageSource), StringComparison.InvariantCultureIgnoreCase))
    {
        if (value == "@remote")
            value = "https://raw.githubusercontent.com/sake402/NetJs/master/zpackages";
        config.InputPackageSource = value;
    }
    else if (name.Equals(nameof(config.OutputPackageSink), StringComparison.InvariantCultureIgnoreCase))
    {
        config.OutputPackageSink = value;
    }
    var yaml = serializer.Serialize(config);
    File.WriteAllText(configFile, yaml);
    Console.WriteLine("Done!");
}

void GetConfig(ParseResult parseResult)
{
    var name = parseResult.GetValue(nameConfigOption) ?? "";
    if (name.Equals(nameof(config.InputPackageSource), StringComparison.InvariantCultureIgnoreCase))
        Console.WriteLine(config.InputPackageSource);
    else if (name.Equals(nameof(config.OutputPackageSink), StringComparison.InvariantCultureIgnoreCase))
        Console.WriteLine(config.OutputPackageSink);
    Console.WriteLine("Done!");
}

Task Serve(ParseResult parseResult)
{
    int port = parseResult.GetValue(servePortOption);
    bool open = parseResult.GetValue(serveOpenBrowserOption);
    var folder = parseResult.GetValue(serveDirectoryOption);
    if (folder == null)
    {
        folder = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly);
        if (files.Any())
        {
            var indexHtml = Directory.GetFiles(folder, "NetJs.Boot.js", SearchOption.AllDirectories)
                .Where(e => (e.Contains("\\Debug\\") || e.Contains("/Debug/")) && (e.Contains("\\wwwroot\\") || e.Contains("/wwwroot/")))
                .OrderBy(e => new FileInfo(e).LastWriteTime)
                .LastOrDefault();
            if (indexHtml != null)
            {
                folder = Path.GetDirectoryName(indexHtml);
            }
        }
    }
    Console.WriteLine($"Serving from \"{folder}\"");
    var server = new McMaster.DotNet.Serve.SimpleServer(new McMaster.DotNet.Serve.CommandLineOptions()
    {
        Port = port == 0 ? null : port,
        OpenBrowser = (open, "")
    }, PhysicalConsole.Singleton, folder);
    return server.RunAsync(CancellationToken.None);
}