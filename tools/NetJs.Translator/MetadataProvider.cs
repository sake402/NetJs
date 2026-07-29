using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using NuGet.ProjectModel;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Net.Http;
using System.IO.Compression;
using YamlDotNet.Serialization;

namespace NetJs.Translator
{
    public class MetadataProvider
    {
        public MetadataProvider(Config config, string dotnetPath, string dotnetVersion, string sdkPath, string sdkVersion, string dataFolder, string tempFolder, IDeserializer deserializer)
        {
            _config = config;
            DotnetPath = dotnetPath;
            DotnetVersion = dotnetVersion;
            SDKPath = sdkPath;
            SDKVersion = sdkVersion;
            this.dataFolder = dataFolder;
            this.tempFolder = tempFolder;
            this.deserializer = deserializer;
        }

        Config _config;
        string DotnetPath;
        string DotnetVersion;
        string SDKPath;
        string SDKVersion;
        string dataFolder;
        string tempFolder;
        IDeserializer deserializer;
        public string LocalPackageCacheFolder => Path.Combine(dataFolder, "Packages");
        public void CleanPackageCache()
        {
            if (Directory.Exists(LocalPackageCacheFolder))
                Directory.Delete(LocalPackageCacheFolder, true);
        }

        LockFile GetLockFile(IProject project, out string content)
        {
            var projectAsset = Path.Combine(project.BaseIntermediateOutputPath, "project.assets.json");
            if (!Path.IsPathRooted(projectAsset))
                projectAsset = Path.Combine(project.DirectoryPath, projectAsset);
            if (!File.Exists(projectAsset))
            {
                if (!project.Build())
                    throw new InvalidOperationException($"Expected projectasset.json file not found at {projectAsset} and autobuild fails. Ensure that project restore has run.");
            }
            content = File.ReadAllText(projectAsset);
            var lockFileFormat = new LockFileFormat();
            var lockFile = lockFileFormat.Parse(content, "In Memory");
            return lockFile;
        }

        HashSet<string> addedReference = new();
        async Task AddReference(IProject project, string refName, List<MetadataReference>? refs = null, List<string>? symbols = null)
        {
            if (refName.StartsWith("NetJs."))
            {

            }
            if (refName == "System.Drawing")
                refName = "System.Drawing.Primitives";
            else if (refName == "System.Net")
                refName = "System.Net.Primitives";
            if (!addedReference.Add(refName))
                return;
            //Console.WriteLine($"Adding implicit \"{refName}\"...");
            var targetFramework = project.GetTargetFramework();
            if (!targetFramework.EndsWith("-browser"))
            {
                targetFramework += "-browser";
            }
            var refPackageFolder = $"{LocalPackageCacheFolder}{Path.DirectorySeparatorChar}{targetFramework}{Path.DirectorySeparatorChar}NetJs.{refName}";
            var refPackageDll = $"{refPackageFolder}{Path.DirectorySeparatorChar}NetJs.{refName}.js.dll";
            var refPackageSymbolYaml = $"{refPackageFolder}{Path.DirectorySeparatorChar}NetJs.{refName}.Symbols.yaml";
            var refPackagePackageYaml = $"{refPackageFolder}{Path.DirectorySeparatorChar}package.yaml";
            if (!File.Exists(refPackageDll))
            {
                var packageFeedUrl = _config.InputPackageSource;// "https://raw.githubusercontent.com/sake402/NetJs/master/zpackages";
                var remoteUrl = $"{packageFeedUrl}/{targetFramework}/NetJs.{refName}.package.zip";
                Console.WriteLine($"Downloading {remoteUrl}...");
                try
                {
                    Stream zipStream;
                    if (remoteUrl.StartsWith("http"))
                        zipStream = await http.GetStreamAsync(remoteUrl);
                    else
                    {
                        if (!File.Exists(remoteUrl))
                            throw new HttpRequestException("404");
                        zipStream = new FileStream(remoteUrl, FileMode.Open, FileAccess.Read);
                    }
                    Console.WriteLine($"Extracting...");
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                    {
                        if (!Directory.Exists(refPackageFolder))
                            Directory.CreateDirectory(refPackageFolder);
                        archive.ExtractToDirectory(refPackageFolder);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"{refName} doesnt have a NetJs package yet. Skipped!");
                    if (ex.Message.Contains("404"))
                    {
                        //Creaate a dummy empty place hodel file to prevent further download attempt
                        if (!Directory.Exists(refPackageFolder))
                            Directory.CreateDirectory(refPackageFolder);
                        var fdll = File.Create(refPackageDll);
                        fdll.Flush();
                        fdll.Close();
                        var fyaml = File.Create(refPackageSymbolYaml);
                        fyaml.Flush();
                        fyaml.Close();
                    }
                    return;
                }
            }
            if (File.Exists(refPackagePackageYaml))
            {
                var yaml = File.ReadAllText(refPackagePackageYaml);
                var model = deserializer.Deserialize<PackageModel>(yaml);
                if (model.Dependencies != null)
                {
                    foreach (var package in model.Dependencies)
                    {
                        var dpackage = package;
                        if (dpackage.EndsWith(".js"))
                            dpackage = Path.GetFileNameWithoutExtension(dpackage);
                        if (dpackage.StartsWith(Constants.ProjectName + "."))
                            dpackage = dpackage.Substring((Constants.ProjectName + ".").Length);
                        await AddReference(project, dpackage, refs, symbols);
                    }
                }
            }
            if (File.Exists(refPackageDll))
            {
                if (refs != null)
                {
                    var fi = new FileInfo(refPackageDll);
                    if (fi.Length > 0)
                        refs.Add(MetadataReference.CreateFromFile(refPackageDll));
                }
            }
            else
            {
                Console.WriteLine($"No metadata file \"{refPackageDll}\"!");
            }
            if (File.Exists(refPackageSymbolYaml))
            {
                if (symbols != null)
                {
                    var fi = new FileInfo(refPackageSymbolYaml);
                    if (fi.Length > 0)
                        symbols.Add(refPackageSymbolYaml);
                }
            }
            else
            {
                Console.WriteLine($"No symbol file \"{refPackageSymbolYaml}\"!");
            }
        }

        public async Task PullPackageCache(IProject project, LockFile? projectAssetJson = null, List<MetadataReference>? refs = null, List<string>? symbols = null)
        {
            addedReference.Clear();
            projectAssetJson ??= GetLockFile(project, out _);
            var dotnetFolder = Path.GetDirectoryName(DotnetPath);
            var localPackageCacheFolder = LocalPackageCacheFolder;
            if (!Directory.Exists(localPackageCacheFolder))
                Directory.CreateDirectory(localPackageCacheFolder);
            //Console.WriteLine($"Using Package Folder \"{localPackageCacheFolder}\"...");
            await AddReference(project, "System.Private.CoreLib", refs, symbols);
            await AddReference(project, "System.Private.Uri", refs, symbols);
            await AddReference(project, "System.Private.Xml", refs, symbols);
            await AddReference(project, "System.Private.Xml.Linq", refs, symbols);
            if (projectAssetJson.Targets != null)
            {
                foreach (var target in projectAssetJson.Targets)
                {
                    foreach (var lib in target.Libraries)
                    {
                        if (lib.Name != null)
                        {
                            await AddReference(project, lib.Name, refs, symbols);
                            if (lib.Dependencies != null)
                            {
                                foreach (var ilib in lib.Dependencies)
                                {
                                    await AddReference(project, ilib.Id, refs, symbols);
                                }
                            }
                            if (lib.RuntimeAssemblies != null)
                            {
                                foreach (var ilib in lib.RuntimeAssemblies)
                                {
                                    var name = Path.GetFileNameWithoutExtension(ilib.Path);
                                    await AddReference(project, name, refs, symbols);
                                }
                            }
                            if (lib.CompileTimeAssemblies != null)
                            {
                                foreach (var ilib in lib.CompileTimeAssemblies)
                                {
                                    var name = Path.GetFileNameWithoutExtension(ilib.Path);
                                    await AddReference(project, name, refs, symbols);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var frm in projectAssetJson.PackageSpec.TargetFrameworks)
            {
                foreach (var fref in frm.FrameworkReferences)
                {
                    var name = fref.Name;
                    var packBaseFolder = $"{dotnetFolder}{Path.DirectorySeparatorChar}packs{Path.DirectorySeparatorChar}{name}.Ref";
                    if (Directory.Exists(packBaseFolder))
                    {
                        var targetFramework = project.GetTargetFramework();
                        string versionPrefix = targetFramework.Replace("net", "");
                        var latestVersionFolder = Directory.GetDirectories(packBaseFolder, $"{versionPrefix}.*")
                            .Select(Path.GetFileName)
                            .OrderByDescending(v => v)
                            .FirstOrDefault();
                        if (latestVersionFolder != null)
                        {
                            var packFolder = $"{packBaseFolder}{Path.DirectorySeparatorChar}{latestVersionFolder}{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}{targetFramework}";
                            if (Directory.Exists(packFolder))
                            {
                                //bool IsTypeForwarding(string path)
                                //{
                                //    Assembly asm = Assembly.ReflectionOnlyLoadFrom(path);
                                //    // Retrieve all exported forwarded types
                                //    Type[] forwardedTypes = asm.GetForwardedTypes();

                                //    Console.WriteLine($"Assembly: {asm.FullName}");
                                //    Console.WriteLine($"Total Type Forwards: {forwardedTypes.Length}");

                                //    foreach (var type in forwardedTypes)
                                //    {
                                //        Console.WriteLine($"Forwarded Type: {type.FullName} -> Destination: {type.Assembly.FullName}");
                                //    }
                                //}
                                var implicitReferences = Directory.GetFiles(packFolder, "*.dll");
                                foreach (var reff in implicitReferences)
                                {
                                    var refName = Path.GetFileNameWithoutExtension(reff);
                                    await AddReference(project, refName, refs, symbols);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"No targeting pack at \"{packFolder}\"!");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No framework version \"{versionPrefix}\" in \"{packBaseFolder}\"!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Framework reference folder not at \"{packBaseFolder}\"!");
                    }
                }
            }
        }

        IList<LockFileTargetLibrary> Sort(IList<LockFileTargetLibrary> libraries)
        {
            var libraryMap = libraries.ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);
            var sortedLibraries = new List<LockFileTargetLibrary>();
            var visited = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase); // false = visiting, true = visited

            // 2. Local function for Depth-First Search (DFS) topological sort
            void Visit(LockFileTargetLibrary library)
            {
                if (visited.TryGetValue(library.Name, out bool isFullyVisited))
                {
                    if (!isFullyVisited)
                    {
                        // Optional: Circular dependency detected (a -> b -> a). 
                        // We break out to prevent infinite loops.
                        return;
                    }
                    return; // Already processed
                }

                // Mark as currently visiting
                visited[library.Name] = false;

                // Process all dependencies first (bottom of the chain)
                foreach (var dependency in library.Dependencies)
                {
                    if (libraryMap.TryGetValue(dependency.Id, out var dependentLibrary))
                    {
                        Visit(dependentLibrary);
                    }
                }

                // Mark as fully processed
                visited[library.Name] = true;

                // Add to the final list
                sortedLibraries.Add(library);
            }

            // 3. Execute the sort for all libraries in the target
            foreach (var library in libraries)
            {
                Visit(library);
            }
            return sortedLibraries;
        }

        HttpClient http = new HttpClient();
        async Task<(MetadataReference[], string[])> GetReferencesForProject(IProject project)
        {
            List<MetadataReference> refs = new List<MetadataReference>();
            List<string> symbols = new List<string>();

            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
            var nugetPackageFolder = NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(settings);

            var lockFile = GetLockFile(project, out var content);

            var disableImplicit = project.Evaluate("DisableImplicitFrameworkReferences").LastOrDefault();
            bool enableImplicitImport = string.IsNullOrEmpty(disableImplicit);
            if (disableImplicit?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                enableImplicitImport = false;
            }
            if (enableImplicitImport)
            {
                await PullPackageCache(project, lockFile, refs, symbols);
            }
            var target = lockFile.Targets.Count == 1 ? lockFile.Targets.Single() :
                (lockFile.Targets.SingleOrDefault(l => l.Name.Contains("-browser")) ??
                lockFile.Targets.SingleOrDefault(f => f.Name == project.GetTargetFramework()) ??
                lockFile.Targets.SingleOrDefault(f => f.Name == "netstandard2.0"))!;
            var sortLibraries = Sort(target.Libraries);
            //var sortLibraries = lockFile.Libraries.Where(e =>
            //{
            //    if (e.Type == "package" && e.HasTools)//TODO: Need better way of doing this. We targets filtering Microsoft.NET.ILLink.Tasks from Microsoft.AspNetCore.Components
            //    {
            //        return false;
            //    }
            //    return true;
            //}).ToArray();

            //var model = JsonSerializer.Deserialize<ProjectAssetModel>(content);
            //model!.Targets = model.Targets.ToDictionary(e => e.Key, e => e.Value.ToDictionary(ee => ee.Key.Split('/')[0], ee => ee.Value));
            //var dic = model.Targets.Count == 1 ? model.Targets.Values.Single() :
            //    (model.Targets.SingleOrDefault(e => e.Key.Contains("-browser")).Value ?? model.Targets.SingleOrDefault(e => e.Key == project.GetTargetFramework()).Value);
            //var dic = target.Libraries.Select()
            //var adjacencyList = sortLibraries.ToDictionary(l => l.Name, _ => new List<string>());
            //var inDegree = sortLibraries.ToDictionary(l => l.Name, _ => 0);

            //foreach (var lib in sortLibraries)
            //{
            //    var graph = dic[lib.Name];
            //    if (graph.Dependencies != null)
            //    {
            //        foreach (var dep in graph.Dependencies.Keys)
            //        {
            //            // Only map dependencies that exist in our target library list
            //            if (adjacencyList.ContainsKey(dep))
            //            {
            //                adjacencyList[dep].Add(lib.Name); // 'dep' must be loaded before 'lib.Name'
            //                inDegree[lib.Name]++;
            //            }
            //        }
            //    }
            //}

            //var queue = new Queue<string>(inDegree.Where(x => x.Value == 0).Select(x => x.Key));
            //var sortedNames = new List<string>();

            //while (queue.Count > 0)
            //{
            //    var current = queue.Dequeue();
            //    sortedNames.Add(current);

            //    foreach (var neighbor in adjacencyList[current])
            //    {
            //        inDegree[neighbor]--;
            //        if (inDegree[neighbor] == 0)
            //        {
            //            queue.Enqueue(neighbor);
            //        }
            //    }
            //}

            //if (sortedNames.Count != sortLibraries.Length)
            //{
            //    throw new InvalidOperationException("Circular dependency detected in assets file!");
            //}

            //var libraryMap = sortLibraries.ToDictionary(l => l.Name);
            //sortLibraries = sortedNames.Select(name => libraryMap[name]).ToArray();

            ////make sure all dependecies are complete
            //IEnumerable<string> GetDependecies(string libName)
            //{
            //    var graph = dic[libName];
            //    if (graph.Dependencies == null)
            //        yield break;
            //    foreach (var d in graph.Dependencies)
            //        yield return d.Key;
            //    foreach (var d in graph.Dependencies)
            //        foreach (var deps in GetDependecies(d.Key))
            //            yield return deps;
            //}
            //Array.Sort(sortLibraries, (a, b) =>
            //{
            //    if (a.Name == b.Name)
            //        return 0;
            //    var aGraph = dic[a.Name];
            //    var bGraph = dic[b.Name];
            //    if (aGraph.Dependencies == null || aGraph.Dependencies.Count == 0)
            //        return -1;
            //    if (bGraph.Dependencies == null || bGraph.Dependencies.Count == 0)
            //        return 1;
            //    if (GetDependecies(a.Name).Contains(b.Name))
            //    {
            //        return 1;
            //    }
            //    if (GetDependecies(b.Name).Contains(a.Name))
            //    {
            //        return -1;
            //    }
            //    return 0;
            //});

            foreach (var lib in sortLibraries)
            {
                var llib = lockFile.Libraries.First(e => e.Name == lib.Name);
                if (lib.Type == "package")
                {
                    await AddReference(project, lib.Name, refs, symbols);
                    //var nugetPath = $"{nugetPackageFolder}/{lib.Path}/{lib.Files.FirstOrDefault(e => e.EndsWith(".dll"))}";
                    //if (!File.Exists(nugetPath))
                    //    throw new InvalidOperationException($"Expected nuget file not found at {nugetPath}");
                    //refs.Add(MetadataReference.CreateFromFile(nugetPath));
                    //var symbolFile = $"{nugetPackageFolder}/{lib.Path}/{lib.Files.FirstOrDefault(e => e.EndsWith(".Symbols.yaml"))}";
                    //if (File.Exists(symbolFile))
                    //{
                    //    symbols.Add(symbolFile);
                    //}
                }
                else if (lib.Type == "project")
                {
                    var libName = Path.GetFileName(lib.Name)!;
                    var depProject = await project.LoadDependecy(libName);
                    //var libProjectPath = Path.GetFullPath(Path.GetDirectoryName(project.FullPath) + "/" + llib.Path);
                    //var libProjectPath = Path.GetFullPath(Path.GetDirectoryName(project.FullPath) + "/" + lib.Path);
                    //var libProjectFolder = Path.GetDirectoryName(libProjectPath);
                    //We may have override assembly name back to default for some libraries, beacuase of vs intelissense
                    if (!libName.StartsWith($"{Constants.ProjectName}."))
                        libName = Constants.ProjectName + "." + libName;
                    //var config="wasm";//project.Evaluate("Configuration");
                    //var platform = project.GetPlatform();
                    //if (platform.Equals("AnyCPU", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //    platform = "";
                    //}
                    //else
                    //{
                    //    platform = "/" + platform;
                    //}
                    var binPathJs = Path.Combine(depProject.GetOutputPath(), libName + ".js.dll");
                    var binPath = Path.Combine(depProject.GetOutputPath(), libName + ".dll");
                    var symbolFile = Path.Combine(depProject.GetOutputPath(), libName + ".Symbols.yaml");
                    //var binPathJs = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration").LastOrDefault()}/{project.Evaluate("TargetFramework").LastOrDefault()}/" + libName + ".js.dll";
                    //var binPath = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration").LastOrDefault()}/{project.Evaluate("TargetFramework").LastOrDefault()}/" + libName + ".dll";
                    if (!File.Exists(binPath) && !File.Exists(binPathJs))
                        throw new InvalidOperationException($"Expected dll file not found at {binPath} or {binPathJs}. Ensure that project has built successfully.");
                    refs.Add(MetadataReference.CreateFromFile(File.Exists(binPathJs) ? binPathJs : binPath));
                    //var symbolFile = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration").LastOrDefault()}/{project.Evaluate("TargetFramework").LastOrDefault()}/" + libName + ".Symbols.yaml";
                    if (File.Exists(symbolFile))
                    {
                        symbols.Add(symbolFile);
                    }
                }
            }

            return (refs.ToArray(), symbols.ToArray());
        }

        public IEnumerable<SyntaxTree> GetSyntaxTrees(IProject project, string[] sourceCodePath, string[]? sourceCodes, IEnumerable<string>? globalUsings)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

            if (globalUsings != null)
            {
                var globalUsingNodes = globalUsings.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
                 .WithGlobalKeyword(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)
                     .WithTrailingTrivia(SyntaxFactory.Space))
                 .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword)
                     .WithTrailingTrivia(SyntaxFactory.Space))
                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                     .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
                ).ToArray();

                var compilationUnit = SyntaxFactory.CompilationUnit().WithUsings(SyntaxFactory.List(globalUsingNodes));
                var globalUsingsTree = CSharpSyntaxTree.Create(compilationUnit).WithFilePath("GlobalUsings.cs");
                syntaxTrees.Add(globalUsingsTree);
            }

            int index = 0;
            var constants = project.Evaluate("DefineConstants").LastOrDefault()?.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in sourceCodePath)
            {
                //var codeString =/* SourceText.From*/(sourceCode);
                var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithPreprocessorSymbols(constants.Concat(["NET", "NET9_0_OR_GREATER"]));

                var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCodes != null ? sourceCodes[index] : File.ReadAllText(path), options, path: path, encoding: Encoding.UTF8);
                syntaxTrees.Add(parsedSyntaxTree);
                index++;
            }
            return syntaxTrees;
        }
        (MetadataReference[], string[])? referenceAndSymbols;
        public async Task<CSharpCompilation> CreateCompilation(IProject project,
            string[] sourceCodePath,
            string[]? sourceCodes,
            IEnumerable<string>? globalUsings,
            List<MetadataReference>? references,
            List<string>? symbols)
        {
            var syntaxTrees = GetSyntaxTrees(project, sourceCodePath, sourceCodes, globalUsings);
            referenceAndSymbols ??= await GetReferencesForProject(project);
            references?.AddRange(referenceAndSymbols.Value.Item1);
            symbols?.AddRange(referenceAndSymbols.Value.Item2);
            var options = project.CompilationOptions as CSharpCompilationOptions ?? new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                    allowUnsafe: true);
            //var name = project.GetName();
            //if (name != options.ModuleName)
            //    options = options.WithModuleName(name);
            return CSharpCompilation.Create(project.GetAssemblyName(),
                syntaxTrees.ToArray(),
                references: referenceAndSymbols.Value.Item1,
                options: options);
        }
    }
}