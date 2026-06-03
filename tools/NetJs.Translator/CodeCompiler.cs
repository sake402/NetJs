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

namespace NetJs.Translator
{
    internal class CodeCompiler
    {
        //static CodeCompiler()
        //{
        //    //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        //}

        //public CodeCompiler(string dotnetPath, string dotnetVersion, string sdkVersion)
        //{
        //    DotnetPath = dotnetPath;
        //    DotnetVersion = dotnetVersion;
        //    SDKVersion = sdkVersion;
        //}

        //string DotnetPath;
        //string SDKVersion;
        //string DotnetVersion;
        static MetadataReference[]? references;
        static MetadataReference[] References
        {
            get
            {
                return references ??= AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                {
                    //var target = a.GetCustomAttribute<TargetFrameworkAttribute>();
                    //if (target != null)
                    //{

                    //}
                    //if (!target?.FrameworkName.Contains("netstandard") ?? true)
                    //    return false;
                    return !a.IsDynamic && !string.IsNullOrEmpty(a.Location);
                }).Select(a => MetadataReference.CreateFromFile(a.Location)).ToArray();
            }
        }

        CompositeCompilationAssemblyResolver GetAssemblyResolver(string path)
        {
            return new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                //new ReferenceAssemblyPathResolver(),
                //new AppBaseCompilationAssemblyResolver(path),
                new PackageCompilationAssemblyResolver()
            });
        }

        void RecursivelyGetReferencedAssemblies(Assembly assembly, List<Assembly> _assemblies)
        {
            if (_assemblies.Contains(assembly))
                return;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var referencedAssemblies = assembly
                .GetReferencedAssemblies()
                .Select(assemblyName => assemblies.SingleOrDefault(a => a.GetName()?.FullName?.Equals(assemblyName.FullName, StringComparison.InvariantCultureIgnoreCase) ?? false))
                .Where(a => a != null)
                .ToList();
            _assemblies.AddRange(referencedAssemblies!);
            foreach (var a in referencedAssemblies)
            {
                RecursivelyGetReferencedAssemblies(a!, _assemblies);
            }
        }

        IEnumerable<Assembly> RecursivelyGetReferencedAssemblies(Assembly assembly)
        {
            List<Assembly> assemblies = new List<Assembly>();
            RecursivelyGetReferencedAssemblies(assembly, assemblies);
            return assemblies;
        }

        IEnumerable<string> GetReferencedAssembliesInternal(Assembly assembly)
        {
            AssemblyName name = assembly.GetName();
            var dll = name.Name + ".dll";
            bool NamesMatch(RuntimeLibrary runtime)
            {
                return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase) || runtime.RuntimeAssemblyGroups.Any(ag => ag.RuntimeFiles.Any(rtf => rtf.Path.EndsWith(dll, StringComparison.OrdinalIgnoreCase)));
            }
            var dependencyContext = DependencyContext.Load(assembly);
            var dependentAssemblies = new List<string>()
            {
            };
            if (dependencyContext != null)
            {
                var assemblyLibrary = dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
                var assemblyResolver = GetAssemblyResolver(Path.GetDirectoryName(assembly.Location)!);
                var assemblyFolder = Path.GetDirectoryName(assembly.Location);
                foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
                {
                    if (runtimeLibrary == assemblyLibrary)
                    {
                        continue;
                    }
                    var assemblyDll = Path.Combine(assemblyFolder!, runtimeLibrary.Name + ".dll");
                    //if (assemblyResolver.TryResolveAssemblyPaths(runtimeLibrary, dependentAssemblies)) { }
                    if (File.Exists(assemblyDll))
                    {
                        dependentAssemblies.Add(assemblyDll);
                    }
                    else
                    {
                        //assemblyResolver.TryResolveAssemblyPaths(runtimeLibrary, dependentAssemblies);
                    }
                }
                foreach (var depLibrary in dependencyContext.CompileLibraries)
                {
                    var assemblyDll = Path.Combine(assemblyFolder!, depLibrary.Name + ".dll");
                    if (assemblyResolver.TryResolveAssemblyPaths(depLibrary, dependentAssemblies)) { }
                    else if (File.Exists(assemblyDll))
                    {
                        dependentAssemblies.Add(assemblyDll);
                    }
                    else
                    {
                        assemblyResolver.TryResolveAssemblyPaths(depLibrary, dependentAssemblies);
                    }
                }
            }
            //var netCore = Directory.GetFiles(@$"{DotnetPath}packs\Microsoft.NetCore.App.Ref\{SDKVersion}\ref\net8.0", "*.dll");
            //var aspNetCore = Directory.GetFiles(@$"{DotnetPath}packs\Microsoft.AspNetCore.App.Ref\{SDKVersion}\ref\net8.0", "*.dll");
            //var netCores = netCore.Concat(aspNetCore);
            //var netStandards = Directory.GetFiles(@$"{DotnetPath}packs\NETStandard.Library.Ref\2.1.0\ref\netstandard2.1", "*.dll")
            //    .Where(ns => !netCores.Any(nc => Path.GetFileName(nc) == Path.GetFileName(ns)));
            //var netSDKs = netStandards.Concat(netCore).Concat(aspNetCore);
            var referencedAssemblies = RecursivelyGetReferencedAssemblies(assembly).Select(a => a.GetName())
                //.Where(a => !netSDKs.Select(s => Path.GetFileNameWithoutExtension(s)).Contains(a.Name))
                .Select(name => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().FullName.Equals(name.FullName)))
                .Where(a => a != null && !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a!.Location);
            return dependentAssemblies.Concat(referencedAssemblies);
            //return netSDKs.Concat(dependentAssemblies).Concat(referencedAssemblies);
        }

        ConcurrentDictionary<Assembly, IEnumerable<string>> assemblyReferences = new ConcurrentDictionary<Assembly, IEnumerable<string>>();
        public IEnumerable<string> GetReferencedAssemblies(Assembly assembly)
        {
            //if (true)//Configuration.CacheAssemblyReferences)
            //{
            return assemblyReferences.GetOrAdd(assembly, GetReferencedAssembliesInternal);
            //}
            //else
            //{
            //    return GetReferencedAssembliesInternal(assembly);
            //}
        }

        ConcurrentDictionary<Assembly, MetadataReference[]> metadataReferences = new ConcurrentDictionary<Assembly, MetadataReference[]>();

        //MetadataReference[] GetReferences(Assembly assembly)
        //{
        //    Func<Assembly, MetadataReference[]> execute = (assembly) =>
        //    {
        //        return GetReferencedAssemblies(assembly)
        //        .Select(a => MetadataReference.CreateFromFile(a)).ToArray();
        //        //return new[] { assembly.Location }
        //        //.Concat(GetReferencedAssemblies(assembly))
        //        //.Select(a => MetadataReference.CreateFromFile(a)).ToArray();
        //    };
        //    if (true)//Configuration.CacheMetadataReferences)
        //    {
        //        return metadataReferences.GetOrAdd(assembly, execute);
        //    }
        //    else
        //    {
        //        return execute(assembly);
        //    }
        //}

        //static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        //{
        //    var dll = args.Name + ".dll";
        //    bool NamesMatch(RuntimeLibrary runtime)
        //    {
        //        return string.Equals(runtime.Name, args.Name, StringComparison.OrdinalIgnoreCase) || runtime.RuntimeAssemblyGroups.Any(ag => ag.RuntimeFiles.Any(rtf => rtf.Path.EndsWith(dll, StringComparison.OrdinalIgnoreCase)));
        //    }
        //    var assembly = args.RequestingAssembly;
        //    DependencyContext dependencyContext = DependencyContext.Load(assembly);
        //    RuntimeLibrary library = dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
        //    var assemblyResolver = GetAssemblyResolver(Path.GetDirectoryName(assembly.Location));
        //    var assemblies = new List<string>();
        //    var wrapper = new CompilationLibrary(
        //            library.Type,
        //            library.Name,
        //            library.Version,
        //            library.Hash,
        //            library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
        //            library.Dependencies,
        //            library.Serviceable);
        //    assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
        //    if (assemblies.Count == 0)
        //    {

        //    }
        //    return AppDomain.CurrentDomain.Load(assemblies[0]);
        //}

        //Assembly[] GetAssemblies(string[] sourceCodes)
        //{
        //    List<Assembly> assemblies = new List<Assembly>();
        //    foreach (var source in sourceCodes)
        //    {
        //        var project = ProjectInfo.GetProjectDefinition(Path.GetDirectoryName(source)!);
        //        if (project != null)
        //        {
        //            var projectName = Path.GetFileNameWithoutExtension(project.FileName);
        //            var debugPath = Path.Combine(project.Path, "bin", "Debug", project.Type);
        //            var dll = Path.Combine(debugPath, (project.AssemblyName ?? projectName) + ".dll");
        //            if (File.Exists(dll))
        //            {
        //                var projectAssembly = Assembly.LoadFrom(dll);
        //                if (projectAssembly.Location != dll) //if assembly is already loaded, copy the deps.json file in so DependencyContext can use it
        //                {
        //                    var deps = Path.ChangeExtension(dll, "deps.json");
        //                    var dst = Path.ChangeExtension(projectAssembly.Location, "deps.json");
        //                    if (!File.Exists(dst))
        //                    {
        //                        File.Copy(deps, dst, true);
        //                    }
        //                }
        //                if (!assemblies.Contains(projectAssembly))
        //                    assemblies.Add(projectAssembly);
        //            }
        //            else
        //            {
        //                //throw new InvalidOperationException($"Cannot locate existing assembly for the project {projectName}");
        //            }
        //        }
        //    }
        //    return assemblies.Distinct().ToArray();
        //}

        //MetadataReference[] GetReferences(string[] sourceCodes)
        //{
        //    return GetAssemblies(sourceCodes).SelectMany(ass => GetReferences(ass)).ToArray();
        //}

        (MetadataReference[], string[]) GetReferencesForProject(IProject project)
        {
            //var projectDepJson = Path.GetDirectoryName(project.Path) + "/bin/Debug/netstandard2.0/" + project.AssemblyName + ".deps.json";
            ////var projectDll = Path.GetDirectoryName(project.Path) + "/bin/Debug/netstandard2.0/" + project.AssemblyName + ".dll";
            //if (!File.Exists(projectDepJson))
            //{
            //    //TODO: prebuild via dotnet so we can have deps.json
            //}
            //var reader = new DependencyContextJsonReader();
            //var dependency = reader.Read(new FileStream(projectDepJson, FileMode.Open, FileAccess.Read));
            //dependency.RuntimeLibraries.Select(lib =>
            //{
            //    if (lib.Type == "package")
            //    {
            //        var nugetPath = $"{lib.Path}/{lib.ResourceAssemblies.First().Path}";
            //        return MetadataReference.CreateFromFile(nugetPath);
            //    }
            //});
            //return GetReferences(Assembly.Load(projectDll));

            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
            var nugetPackageFolder = NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(settings);

            List<MetadataReference> refs = new List<MetadataReference>();
            List<string> symbols = new List<string>();
            var projectAsset = Path.GetDirectoryName(project.FullPath) + "/obj/project.assets.json";
            if (!File.Exists(projectAsset))
            {
                if (!project.Build())
                    throw new InvalidOperationException($"Expected projectasset.json file not found at {projectAsset} and autobuild fails. Ensure that project restore has run.");
            }
            string content = File.ReadAllText(projectAsset);
            var lockFileFormat = new LockFileFormat();
            var lockFile = lockFileFormat.Parse(content, "In Memory");
            var sortLibraries = lockFile.Libraries.ToArray();
            var model = JsonSerializer.Deserialize<ProjectAssetModel>(content);
            model!.Targets = model.Targets.ToDictionary(e => e.Key, e => e.Value.ToDictionary(ee => ee.Key.Split('/')[0], ee => ee.Value));
            var dic = model.Targets.Values.Single();
            //make sure all dependecies are complete
            IEnumerable<string> GetDependecies(string libName)
            {
                var graph = dic[libName];
                if (graph.Dependencies == null)
                    yield break;
                foreach (var d in graph.Dependencies)
                    yield return d.Key;
                foreach (var d in graph.Dependencies)
                    foreach (var deps in GetDependecies(d.Key))
                        yield return deps;
            }
            Array.Sort(sortLibraries, (a, b) =>
            {
                var aGraph = dic[a.Name];
                var bGraph = dic[b.Name];
                if (aGraph.Dependencies == null || aGraph.Dependencies.Count == 0)
                    return -1;
                if (bGraph.Dependencies == null || bGraph.Dependencies.Count == 0)
                    return 1;
                if (GetDependecies(a.Name).Contains(b.Name))
                {
                    return 1;
                }
                if (GetDependecies(b.Name).Contains(a.Name))
                {
                    return -1;
                }
                return 0;
            });
            foreach (var lib in sortLibraries)
            {
                if (lib.Type == "package")
                {
                    var nugetPath = $"{nugetPackageFolder}/{lib.Path}/{lib.Files.FirstOrDefault(e => e.EndsWith(".dll"))}";
                    if (!File.Exists(nugetPath))
                        throw new InvalidOperationException($"Expected nuget file not found at {nugetPath}");
                    refs.Add(MetadataReference.CreateFromFile(nugetPath));
                    var symbolFile = $"{nugetPackageFolder}/{lib.Path}/{lib.Files.FirstOrDefault(e => e.EndsWith(".SymbolNames.yaml"))}";
                    if (File.Exists(symbolFile))
                    {
                        symbols.Add(symbolFile);
                    }
                }
                else if (lib.Type == "project")
                {
                    var libProjectPath = Path.GetFullPath(Path.GetDirectoryName(project.FullPath) + "/" + lib.Path);
                    var libProjectFolder = Path.GetDirectoryName(libProjectPath);
                    //var config="wasm";//project.Evaluate("Configuration");
                    var binPathJs = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration")}/{project.Evaluate("TargetFramework")}/" + Path.GetFileName(lib.Name) + ".js.dll";
                    var binPath = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration")}/{project.Evaluate("TargetFramework")}/" + Path.GetFileName(lib.Name) + ".dll";
                    if (!File.Exists(binPath) && !File.Exists(binPathJs))
                        throw new InvalidOperationException($"Expected dll file not found at {binPath} or {binPathJs}. Ensure that project has built successfully.");
                    refs.Add(MetadataReference.CreateFromFile(File.Exists(binPathJs) ? binPathJs : binPath));
                    var symbolFile = libProjectFolder + $"/bin/wasm/{project.Evaluate("Configuration")}/{project.Evaluate("TargetFramework")}/" + Path.GetFileName(lib.Name) + ".SymbolNames.yaml";
                    if (File.Exists(symbolFile))
                    {
                        symbols.Add(symbolFile);
                    }
                }
            }
            return (refs.ToArray(), symbols.ToArray());
        }

        //public byte[]? Compile(params string[] sourceCodes)
        //{
        //    //Console.WriteLine($"Starting compilation of: '{file}'");

        //    //var sourceCode = fileIsCode ? file : File.ReadAllText(file);

        //    using (var peStream = new MemoryStream())
        //    {
        //        var result = GenerateCode(sourceCodes).Emit(peStream);

        //        if (!result.Success)
        //        {
        //            Console.WriteLine("Compilation done with error.");

        //            var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

        //            foreach (var diagnostic in failures)
        //            {
        //                Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
        //            }

        //            return null;
        //        }

        //        Console.WriteLine("Compilation done without any error.");

        //        peStream.Seek(0, SeekOrigin.Begin);

        //        return peStream.ToArray();
        //    }
        //}


        public IEnumerable<SyntaxTree> GetSyntaxTrees(IProject project, string[] sourceCodePath, string[]? sourceCodes)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            int index = 0;
            var constants = project.Evaluate("DefineConstants")?.Split([';'], StringSplitOptions.RemoveEmptyEntries);
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

        public CSharpCompilation GenerateCode(IProject project, string[] sourceCodePath, string[]? sourceCodes, out IEnumerable<MetadataReference> references, out IEnumerable<string> symbols)
        {
            var syntaxTrees = GetSyntaxTrees(project, sourceCodePath, sourceCodes);
            var mreferences = GetReferencesForProject(project);
            references = mreferences.Item1;
            symbols = mreferences.Item2;
            var options = new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                    allowUnsafe: true);
            return CSharpCompilation.Create(project.GetName(),
                syntaxTrees.ToArray(),
                references: mreferences.Item1,
                options: options);
        }
    }
}