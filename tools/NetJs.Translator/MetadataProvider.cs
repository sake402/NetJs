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

        public async Task PullPackageCache(IProject project, LockFile? projectAssetJson = null, LockFileTarget? target = null, List<MetadataReference>? refs = null, List<string>? symbols = null)
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
            async Task DoTarget(LockFileTarget target)
            {
                foreach (var lib in target.Libraries)
                {
                    if (lib.Type == "package")
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
            if (target != null)
            {
                await DoTarget(target);
            }
            else if (projectAssetJson.Targets != null)
            {
                foreach (var mtarget in projectAssetJson.Targets)
                {
                    await DoTarget(mtarget);
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

        public static MetadataReference? GetMetadataReference(IProject project, /*out string binPath,*/ out string binPathJs)
        {
            var libName = project.GetAssemblyName();
            binPathJs = Path.Combine(project.GetOutputPath(), libName + ".js.dll");
            //binPath = Path.Combine(project.GetOutputPath(), libName + ".dll");
            if (/*!File.Exists(binPath) && */!File.Exists(binPathJs))
                return null;
            return MetadataReference.CreateFromFile(/*File.Exists(binPathJs) ? */binPathJs /*: binPath*/);
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
            var tfm = project.GetTargetFramework();
            var otfm = tfm;
            if (tfm.EndsWith("-browser"))
                tfm = tfm.Substring(0, tfm.Length - 8);
            var target = lockFile.Targets.Count == 1 ? lockFile.Targets.Single() :
                (lockFile.Targets.SingleOrDefault(l => l.Name.Contains("-browser")) ??
                lockFile.Targets.SingleOrDefault(f => f.Name == otfm) ??
                lockFile.Targets.SingleOrDefault(f => f.Name == tfm) ??
                lockFile.Targets.SingleOrDefault(f => f.Name == "netstandard2.0"))!;
            if (enableImplicitImport)
            {
                await PullPackageCache(project, lockFile, target, refs, symbols);
            }
            var sortLibraries = Sort(target.Libraries);
            foreach (var lib in sortLibraries)
            {
                var llib = lockFile.Libraries.First(e => e.Name == lib.Name);
                if (lib.Type == "package")
                {
                    await AddReference(project, lib.Name, refs, symbols);
                }
                else if (lib.Type == "project")
                {
                    var libName = Path.GetFileName(lib.Name)!;
                    var depProject = await project.LoadDependecy(libName);
                    if (!libName.StartsWith($"{Constants.ProjectName}."))
                        libName = Constants.ProjectName + "." + libName;
                    var pref = GetMetadataReference(depProject,/* out string binPath,*/ out string binPathJs) ??
                        throw new InvalidOperationException($"Expected dll file not found at {binPathJs}. Ensure that project has built successfully.");
                    refs.Add(pref);
                    var symbolFile = Path.Combine(depProject.GetOutputPath(), libName + ".Symbols.yaml");
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
            //project.GetName();
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

            var constants = project.Evaluate("DefineConstants").LastOrDefault()?.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithPreprocessorSymbols((constants ?? Enumerable.Empty<string>()).Concat([
                "NETJS",
                "NET",
                "NET10_0_OR_GREATER",
                "NET9_0_OR_GREATER",
                "NET8_0_OR_GREATER",
                "NET7_0_OR_GREATER",
                "NET6_0_OR_GREATER",
                "NET5_0_OR_GREATER",
                "NET4_0_OR_GREATER",
                "NET3_0_OR_GREATER",
                "NETCOREAPP3_0_OR_GREATER"]));

            sourceCodePath.Select((s, i) => (s, i))
                .AsParallel()
                //.WithDegreeOfParallelism(4)
                .ForAll((path) =>
            {
                var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCodes != null ? sourceCodes[path.i] : File.ReadAllText(path.s), options, path: path.s, encoding: Encoding.UTF8);
                lock (syntaxTrees)
                    syntaxTrees.Add(parsedSyntaxTree);
            });
            //int index = 0;
            //foreach (var path in sourceCodePath)
            //{
            //    //var codeString =/* SourceText.From*/(sourceCode);
            //    var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCodes != null ? sourceCodes[index] : File.ReadAllText(path), options, path: path, encoding: Encoding.UTF8);
            //    syntaxTrees.Add(parsedSyntaxTree);
            //    index++;
            //}
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

            //Make sure we expose private and internal members from metadata, especially for name overload resolution
            var importOptionsProperty = typeof(CSharpCompilationOptions)
                .GetProperty("MetadataImportOptions", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (importOptionsProperty != null)
            {
                // The internal MetadataImportOptions enum maps exactly to these byte values:
                // Public = 0, Internal = 1, All = 2
                byte importAllMembers = 2;
                importOptionsProperty.SetValue(options, importAllMembers);
            }

            //var prop = typeof(CSharpCompilationOptions)
            //    .GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            //if (prop != null)
            //{
            //    // 0x00400000 (or 1 << 22) represents BinderFlags.IgnoreAccessibility inside Roslyn
            //    uint ignoreAccessibilityFlag = 0x00400000;
            //    prop.SetValue(options, ignoreAccessibilityFlag);
            //}
            //var name = project.GetName();
            //if (name != options.ModuleName)
            //    options = options.WithModuleName(name);
            return CSharpCompilation.Create(project.GetAssemblyName(),
                syntaxTrees.ToArray(),
                references: references?.ToArray() ?? referenceAndSymbols.Value.Item1,
                options: options);
        }
    }
}