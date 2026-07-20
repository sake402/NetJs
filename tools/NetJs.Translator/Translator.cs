using NetJs.Translator.CSharpToJavascript;
using NetJs.Translator.RazorToCSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using LivingThing.Core.Frameworks.Common.OneOf;
using System.IO.Compression;
using System.Linq.Expressions;

namespace NetJs.Translator
{
    public partial class Translator
    {
        string dotnetPath;
        string dotnetVersion;
        string dotnetSdkPath;
        string dotnetSdkVersion;
        IProject project;
        IProjectOutputProvider output;
        Random random;
        public IEnumerable<IIncrementalGenerator>? SourceGenerators { get; set; }
        //public TextWriter? LogTo { get; set; }
        public string TempFolder { get; set; }

        CodeCompiler compiler = default!;
        CSharpCompilation csCompilation = default!;

        IEnumerable<string> globalUsings = Enumerable.Empty<string>();
        IEnumerable<string> sourceFiles = Enumerable.Empty<string>();
        IEnumerable<string> contentFiles = Enumerable.Empty<string>();
        IEnumerable<string> linkerFiles = Enumerable.Empty<string>();
        IEnumerable<string> embeddedFiles = Enumerable.Empty<string>();
        string wwwrootFolder;
        IEnumerable<string> wwwrootFiles = Enumerable.Empty<string>();
        string? indexFile;
        bool isRazorProject;
        bool isRCL;

        ISerializer serializer;
        IDeserializer deSerializer;

        //ResXResourceReader;
        bool isSystemPrivateCoreLib;
        string projectTempFolder;

        public Translator(
            string dotnetPath,
            string dotnetVersion,
            string dotnetSdkPath,
            string dotnetSdkVersion,
            IProject project,
            IProjectOutputProvider output)
        {
            this.dotnetPath = dotnetPath;
            this.dotnetVersion = dotnetVersion;
            this.dotnetSdkPath = dotnetSdkPath;
            this.dotnetSdkVersion = dotnetSdkVersion;
            this.project = project;
            this.output = output;
            TempFolder = Path.GetTempPath() + "NetJs";
            random = new Random();
            globalUsings = project.GetGlobalUsings();
            sourceFiles = project.GetSourceFiles();
            contentFiles = project.GetContentFiles();
            linkerFiles = project.GetLinkerFiles();
            embeddedFiles = project.GetEmbeddedFiles();
            serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            deSerializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            isSystemPrivateCoreLib = project.GetAssemblyName() == "NetJs.System.Private.CoreLib";
            projectTempFolder = Path.Combine(TempFolder, project.GetName());
            wwwrootFolder = project.GetFolder() + Path.DirectorySeparatorChar + "wwwroot" + Path.DirectorySeparatorChar;
            wwwrootFiles = contentFiles.Where(e => e.StartsWith(wwwrootFolder)).ToList();
            indexFile = wwwrootFiles.SingleOrDefault(e => e.EndsWith("wwwroot" + Path.DirectorySeparatorChar + "index.html"));
            isRazorProject = project.SDK.EndsWith(".Razor");
            isRCL = isRazorProject && indexFile == null;
        }

        List<MetadataReference> sortedReferences = new();
        List<string> symbolFiles = new();
        SyntaxTree[] syntaxTrees = Array.Empty<SyntaxTree>();
        (string FilePath, string Source)[] replacements = Array.Empty<(string, string)>();

        MemoryStream dllStream = new MemoryStream();
        MemoryStream pdbStream = new MemoryStream();
        MemoryStream docStream = new MemoryStream();
        EmitResult emitResult = default!;

        GlobalCompilationVisitor global = default!;
        ReflectionMetadataBuilder metadataBuilder = default!;

        List<OneOf<(string, string), (string, Stream)>> packages = new();
        List<string> sortedOutputtedJsFiles = new();
        List<Task> pendingTask = new();

        void Clean()
        {
            //Delete all existing files first, we want to check for filename duplicates since this is a flat directory structure
            if (!Directory.Exists(projectTempFolder))
                Directory.CreateDirectory(projectTempFolder);

            $"Cleaning".Profile(() =>
            {
                var files = Directory.GetFiles(projectTempFolder);
                foreach (var f in files)
                    File.Delete(f);
            });
        }

        Task PreBuildSyntaxTree()
        {
            var csFiles = sourceFiles.Where(e => e.EndsWith(".cs")).ToList();
            return $"Prebuilding Syntax Tree".ProfileAsync(async () =>
            {
                csCompilation = await compiler.GenerateCode(project, csFiles.ToArray(), null, globalUsings, sortedReferences, null);
                syntaxTrees = csCompilation.SyntaxTrees.ToArray();
            });
        }

        Task RewiteFirstPass()
        {
            return $"Rewriting.1".ProfileAsync(async () =>
            {
                Parallel.ForEach(syntaxTrees.Select((tree, i) => (tree, i)), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, tree =>
                {
                    var visitor = new FirstPassRewriter();
                    var newTree = (((CSharpSyntaxNode)tree.tree.GetRoot()).Accept(visitor))
                    !.SyntaxTree
                    .WithFilePath(tree.tree.FilePath);
                    syntaxTrees[tree.i] = newTree;
                });
                sortedReferences.Clear();
                csCompilation = await compiler.GenerateCode(project, syntaxTrees.Select(c => c.FilePath).ToArray(), syntaxTrees.Select(c => c.GetText().ToString()).ToArray(), null, sortedReferences, null);
                syntaxTrees = csCompilation.SyntaxTrees.ToArray();
            });
        }

        Task RewiteSecondPass()
        {
            return $"Rewriting.2".ProfileAsync(async () =>
            {
                var partialClassGroupings = syntaxTrees
                    .SelectMany(s => s.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                    .GroupBy(t => t.CreateFullMemberName()!)
                    .ToDictionary(e => e.Key, e => e.ToList());
                Parallel.ForEach(syntaxTrees.Select((tree, i) => (tree, i)), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, tree =>
                {
                    var visitor = new SecondPassRewriter(csCompilation, tree.tree, partialClassGroupings);
                    var newTree = (((CSharpSyntaxNode)tree.tree.GetRoot()).Accept(visitor))
                    !.SyntaxTree
                    .WithFilePath(tree.tree.FilePath);
                    var typesInTree = newTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>();
                    var sourceCode = newTree.GetText().ToString();
                    foreach (var t in typesInTree)
                    {
                        var name = t.CreateFullMemberName();
                        var partials = partialClassGroupings[name];
                        foreach (var partial in partials)
                        {
                            if (partial.HasAnyAttribute([typeof(VerbatimReplacementAttribute).FullName], out var atts))
                            {
                                var att = atts.Single().Value;
                                foreach (var a in att)
                                {
                                    var pattern = (a.ArgumentList!.Arguments[0].Expression as LiteralExpressionSyntax)?.Token.ValueText;
                                    var replace = (a.ArgumentList!.Arguments[1].Expression as LiteralExpressionSyntax)?.Token.ValueText;
                                    if (pattern != null && replace != null)
                                    {
                                        sourceCode = sourceCode.Replace(pattern, replace);
                                    }
                                }
                            }
                        }
                    }
                    //var path = project.DirectoryPath.GetRelativePath(newTree.FilePath);
                    //var tempFile = Path.Combine(TempFolder, path);
                    var tempFile = Path.Combine(projectTempFolder, Path.GetFileName(newTree.FilePath));
                    int ix = 1;
                    lock (typeof(Translator))
                    {
                        var iTempFile = tempFile;
                        while (File.Exists(iTempFile))
                        {
                            var ext = Path.GetExtension(tempFile);
                            var dir = Path.GetDirectoryName(tempFile);
                            var fileName = Path.GetFileNameWithoutExtension(tempFile);
                            iTempFile = dir + "/" + fileName + ix.ToString() + ext;
                            ix++;
                        }
                        tempFile = iTempFile;
                        var directory = Path.GetDirectoryName(tempFile);
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);
                        File.WriteAllText(tempFile, sourceCode);
                    }
                    replacements[tree.i] = (tempFile, sourceCode);
                });
            });

        }

        Task ReBuildSyntaxTree()
        {
            return $"Rebuilding Syntax Tree".ProfileAsync(async () =>
            {
                sortedReferences.Clear();
                symbolFiles.Clear();
                csCompilation = await compiler.GenerateCode(project, replacements.Select(s => s.FilePath).ToArray(), replacements.Select(s => s.Source).ToArray(), null, sortedReferences, symbolFiles);
                //var errors = csCompilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error);
            });
        }

        void RunSourceGenerators()
        {
            if (SourceGenerators != null)
            {
                $"Running source generators".Profile(() =>
                {
                    var genDriver = CSharpGeneratorDriver.Create(SourceGenerators.ToArray());
                    genDriver.RunGeneratorsAndUpdateCompilation(csCompilation, out var newCompilation, out var diagnostics);
                    csCompilation = (CSharpCompilation)newCompilation;
                });
            }
        }

        bool EmitDll()
        {
            $"Emit dll".Profile(() =>
            {
                emitResult = csCompilation.Emit(dllStream, pdbStream, docStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb));
            });

            if (!emitResult.Success)
            {
                Console.WriteLine("Compilation failed!");
                foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    Console.Error.WriteLine(diagnostic.ToString());
                }
                return false;
            }

            return true;
        }

        void PrepareToTranspile()
        {
            $"Preparing to transpile".Profile(() =>
            {
                var importedNames = symbolFiles.Select(s => deSerializer.Deserialize<SymbolDescriptor>(File.ReadAllText(s))).ToList();
                global = new GlobalCompilationVisitor(csCompilation, project, isSystemPrivateCoreLib, importedNames);
                metadataBuilder = new ReflectionMetadataBuilder(global, isSystemPrivateCoreLib, contentFiles.Where(e => e.EndsWith(".resx")).ToArray(), embeddedFiles.ToArray());
                metadataBuilder.InitializeForAssembly(csCompilation.Assembly);
                global.Reflection = metadataBuilder;
            });
        }

        void Transpile()
        {
            //var partialClassGroupings = csCompilation.SyntaxTrees
            //    .SelectMany(s => s.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(e => e.Parent is not BaseTypeDeclarationSyntax))
            //    .GroupBy(t => t.CreateFullMemberName()!)
            //    .ToDictionary(e => e.Key, e => e.ToList());
            //Parallel.ForEach(partialClassGroupings.Select((partial, i) => (partial, i)), new ParallelOptions { MaxDegreeOfParallelism = 1/* Environment.ProcessorCount*/ }, (partial) =>
            //{
            //    var tree = partial.partial.Value.First().SyntaxTree;
            //    $"{partial.i + 1}/{partialClassGroupings.Count}. Transpiling \"{tree.FilePath}\"".Profile(() =>
            //    {
            //        var visitor = new TranslatorSyntaxVisitor(global, tree);
            //        ((CSharpSyntaxNode)tree.GetRoot()).Accept(visitor);
            //        global.Visitors[tree] = visitor;
            //    });
            //});
            Parallel.ForEach(csCompilation.SyntaxTrees.Select((tree, i) => (tree, i)), new ParallelOptions
            {
#if DEBUG
                MaxDegreeOfParallelism = 1
#else
                MaxDegreeOfParallelism = Environment.ProcessorCount
#endif
            }, (tree) =>
            {
                $"{tree.i + 1}/{csCompilation.SyntaxTrees.Length}. Transpiling \"{tree.tree.FilePath}\"".Profile(() =>
                {
                    var visitor = new TranslatorSyntaxVisitor(global, tree.tree);
                    ((CSharpSyntaxNode)tree.tree.GetRoot()).Accept(visitor);
                    lock (global.Visitors)
                    {
                        global.Visitors[tree.tree] = visitor;
                    }
                });
            });
        }

        void WriteOwnMetadataToDisk()
        {
            //output the dll and pdb
            dllStream.Position = 0;
            pdbStream.Position = 0;
            docStream.Position = 0;
            pendingTask.Add(output.Output(global, project.GetName() + ".js.dll", dllStream));
            pendingTask.Add(output.Output(global, project.GetName() + ".js.pdb", pdbStream));
            pendingTask.Add(output.Output(global, project.GetName() + ".js.xml", docStream));

            packages.Add((project.GetName() + ".js.dll", dllStream));
            packages.Add((project.GetName() + ".js.pdb", pdbStream));
            packages.Add((project.GetName() + ".js.xml", docStream));
            packages.Add(("package.yaml", new MemoryStream(Encoding.UTF8.GetBytes(serializer.Serialize(new PackageModel()
            {
                Dependencies = sortedReferences.Select(s =>
                {
                    var ret = Path.GetFileNameWithoutExtension(s.Display);
                    if (ret.EndsWith(".js"))
                        ret = Path.GetFileNameWithoutExtension(ret);
                    return ret;
                })!
            })))));
        }

        void CopyDependencies()
        {
            //copy the wwwroot folder in every reference over to this wwwroot folder
            foreach (var _ref in sortedReferences)
            {
                var refFolder = Path.GetDirectoryName(_ref.Display);
                if (Directory.Exists(refFolder))
                {
                    var files = Directory.EnumerateFiles(refFolder, "*.*", SearchOption.AllDirectories).ToList();
                    var projectAssemblyName = Path.GetFileName(_ref.Display);
                    if (projectAssemblyName.EndsWith(".dll"))
                        projectAssemblyName = Path.GetFileNameWithoutExtension(projectAssemblyName);
                    if (projectAssemblyName.EndsWith(".js"))
                        projectAssemblyName = Path.GetFileNameWithoutExtension(projectAssemblyName);
                    foreach (var file in files.OrderBy(o =>
                    {
                        //order the files such that the ones for this particular reference is processed last, its dependencies first
                        var fileName = Path.GetFileName(o);
                        if (fileName.EndsWith(".js"))
                            fileName = Path.GetFileNameWithoutExtension(fileName);
                        if (fileName == projectAssemblyName)
                            return 1;
                        return 0;
                    }))
                    {
                        var relativePath = Utility.GetRelativePath(refFolder, file);
                        pendingTask.Add(output.Output(global, relativePath, file));
                        if (Path.GetExtension(file).ToLower() == ".js" && !sortedOutputtedJsFiles.Contains(relativePath))
                            sortedOutputtedJsFiles.Add(relativePath);
                    }
                }
            }
        }

        void CopyOwnAssets()
        {
            foreach (var file in wwwrootFiles)
            {
                var relativePath = Utility.GetRelativePath(project.GetFolder(), file);
                var outputPath = !relativePath.StartsWith(Constants.OutputFolderName + Path.DirectorySeparatorChar) ? Constants.OutputFolderName + Path.DirectorySeparatorChar + relativePath : relativePath;
                if (outputPath == $"wwwroot{Path.DirectorySeparatorChar}blazor.netjs.js")
                {
                    outputPath = $"wwwroot{Path.DirectorySeparatorChar}_framework{Path.DirectorySeparatorChar}blazor.netjs.js";
                }
                else if (isRCL)
                {
                    outputPath = outputPath.Replace("wwwroot" + Path.DirectorySeparatorChar, $"wwwroot{Path.DirectorySeparatorChar}_content{Path.DirectorySeparatorChar}{project.GetName()}{Path.DirectorySeparatorChar}");
                }
                packages.Add((outputPath, file));
                pendingTask.Add(output.Output(global, outputPath, file));
            }

            var scopedCssBundles = contentFiles.Where(e => !e.StartsWith(wwwrootFolder) && e.EndsWith(".styles.css"));
            foreach (var file in scopedCssBundles)
            {
                var relativePath = Utility.GetRelativePath(project.GetFolder(), file);
                var outputPath = $"wwwroot{Path.DirectorySeparatorChar}{Path.GetFileName(file)}";
                if (isRCL)
                {
                    outputPath = outputPath.Replace("wwwroot" + Path.DirectorySeparatorChar, $"wwwroot{Path.DirectorySeparatorChar}_content{Path.DirectorySeparatorChar}{project.GetName()}{Path.DirectorySeparatorChar}");
                }
                packages.Add((outputPath, file));
                pendingTask.Add(output.Output(global, outputPath, file));
            }
        }

        void WriteTranslatedJsToDisk()
        {
            HashSet<INamedTypeSymbol> outputted = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            HashSet<INamedTypeSymbol> outputting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            HashSet<INamedTypeSymbol> stubbed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            void SortedOutputBuild(INamedTypeSymbol root, INamedTypeSymbol symbol, StringBuilder stringBuilder, int formatTabs, ref bool dependsOnSelf)
            {
                if (global.HasAttribute(symbol, typeof(DependsOnAttribute).FullName, null, false, out var args))
                {
                    var types = (args[0] as IEnumerable<TypedConstant>).Select(c => (INamedTypeSymbol)c.Value!);
                    foreach (var type in types)
                    {
                        bool _dependsOnSelf = false;
                        SortedOutputBuild(root, type, stringBuilder, formatTabs, ref _dependsOnSelf);
                    }
                }
                //var dependentTypes = DependentTypes(symbol);
                //if (dependentTypes.Contains(symbol, SymbolEqualityComparer.Default))
                //{
                //    //var metadata = global.GetMetadata(symbol.OriginalDefinition);
                //    //if (metadata != null)
                //    //{
                //    if (global.ShouldExportType(symbol.OriginalDefinition, null) && stubbed.Add(symbol.OriginalDefinition))
                //        stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{symbol.OriginalDefinition.CreateSignature(global, withGlobalNamespace: false, withAssemblySlugNamespace: true)}\");");
                //    //}
                //}
                IEnumerable<INamedTypeSymbol> GetDirectDependecies(INamedTypeSymbol symbol)
                {
                    if (symbol.BaseType != null)
                    {
                        yield return symbol.BaseType;
                        foreach (var ss in symbol.BaseType.TypeArguments)
                        {
                            if (ss is INamedTypeSymbol nt)
                            {
                                yield return nt;
                                //foreach(var sss in GetDirectDependecies)
                            }
                        }
                    }
                    if (symbol.EnumUnderlyingType != null)
                    {
                        yield return symbol.EnumUnderlyingType;
                    }
                    foreach (var s in symbol.Interfaces)
                    {
                        yield return s;
                        foreach (var ss in s.TypeArguments)
                        {
                            if (ss is INamedTypeSymbol nt)
                            {
                                yield return nt;
                                //foreach(var sss in GetDirectDependecies(nt))
                                //yield return sss;
                            }
                        }
                    }
                }
                var directDependency = GetDirectDependecies(symbol);
                foreach (var dep in directDependency)
                {
                    if (dep.ContainingAssembly.Equals(symbol.ContainingAssembly, SymbolEqualityComparer.Default) &&
                        dep.ContainingSymbol.Kind != SymbolKind.NamedType &&
                        !symbol.Equals(dep, SymbolEqualityComparer.Default) &&
                        !outputted.Contains(dep.OriginalDefinition))
                    {
                        //var metadata = global.GetMetadata(dep.OriginalDefinition);
                        //if (metadata != null)
                        //{
                        if (global.ShouldExportType(dep.OriginalDefinition, null) && stubbed.Add(dep.OriginalDefinition))
                            stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{dep.OriginalDefinition.CreateSignature(global, withGlobalNamespace: false, withAssemblySlugNamespace: true)}\");");
                        //}
                    }
                }
                //if (symbol.Arity > 0)
                //{
                //    foreach (var t in symbol.TypeArguments)
                //    {
                //        if (t is INamedTypeSymbol genericArgument)
                //        {
                //            if (genericArgument.OutputRank(0) > symbol.OutputRank(0) && !outputted.Contains(genericArgument.OriginalDefinition))
                //            {
                //                var metadata = global.GetMetadata(t);
                //                if (metadata != null)
                //                {
                //                    if (stubbed.Add(genericArgument.OriginalDefinition))
                //                        stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{metadata.FullName}\");");
                //                }
                //            }
                //            //if (!nt.IsGenericType && t.OriginalDefinition.Equals(root.OriginalDefinition, SymbolEqualityComparer.Default))
                //            //{
                //            //    if (!dependsOnSelf)
                //            //    {
                //            //        dependsOnSelf = true;
                //            //        var metadata = global.GetMetadata(t);
                //            //        if (metadata != null)
                //            //        {
                //            //            if (stubbed.Add(nt))
                //            //                stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{metadata.FullName}\");");
                //            //        }
                //            //    }
                //            //}

                //            //SortedOutputBuild(root, nt, stringBuilder, formatTabs, ref dependsOnSelf);
                //        }
                //    }
                //}
                ////bool isOutputted = outputted.Contains(symbol.OriginalDefinition);
                ////if (isOutputted)
                ////{
                ////    return;
                ////}
                ////bool isOutputting = outputting.Contains(symbol.OriginalDefinition);
                ////if (isOutputting) //this symbol has dependency on self
                ////{
                ////    var metadata = global.GetMetadata(symbol);
                ////    if (metadata != null)
                ////    {
                ////        if (stubbed.Add(symbol))
                ////            stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{metadata.FullName}\");");
                ////    }
                ////    return;
                ////}
                ////outputting.Add(symbol.OriginalDefinition);
                //if (symbol.BaseType != null)
                //{
                //    if (symbol.BaseType.Arity > 0)
                //    {
                //        foreach (var t in symbol.BaseType.TypeArguments)
                //        {
                //            if (t is INamedTypeSymbol genericArgument)
                //            {
                //                if (genericArgument.OutputRank(0) > symbol.OutputRank(0) && !outputted.Contains(genericArgument.OriginalDefinition))
                //                {
                //                    var metadata = global.GetMetadata(t.OriginalDefinition);
                //                    if (metadata != null)
                //                    {
                //                        if (stubbed.Add(genericArgument.OriginalDefinition))
                //                            stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{metadata.FullName}\");");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    //bool mdependsOnSelf = false;
                //    //SortedOutputBuild(symbol, symbol.BaseType, stringBuilder, formatTabs, ref mdependsOnSelf);
                //}
                //foreach (var i in symbol.AllInterfaces)
                //{
                //    if (i.Arity > 0)
                //    {
                //        foreach (var t in i.TypeArguments)
                //        {
                //            if (t is INamedTypeSymbol genericArgument)
                //            {
                //                if ((genericArgument.OutputRank(0) > symbol.OutputRank(0) || symbol.Equals(genericArgument, SymbolEqualityComparer.Default)) && !outputted.Contains(genericArgument.OriginalDefinition))
                //                {
                //                    var metadata = global.GetMetadata(t.OriginalDefinition);
                //                    if (metadata != null)
                //                    {
                //                        if (stubbed.Add(genericArgument.OriginalDefinition))
                //                            stringBuilder.AppendLine($"        {Constants.AssemblyRegistryName}.{Constants.AssemblyTypeProxyName}(\"{metadata.FullName}\");");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    //bool mdependsOnSelf = false;
                //    //SortedOutputBuild(symbol, i, stringBuilder, formatTabs, ref mdependsOnSelf);
                //}
                //var visitor = global.TypeVisitors.GetValueOrDefault(symbol.OriginalDefinition);
                //if (visitor != null)
                //{
                //    foreach (var dep in visitor.Dependencies)
                //    {
                //        SortedOutputBuild(root, dep, stringBuilder, formatTabs, ref dependsOnSelf);
                //    }
                //}
                var writer = global.TypeWriters.GetValueOrDefault(symbol.OriginalDefinition);
                if (writer != null)
                {
                    var code = writer.Build(formatTabs);
                    if (!string.IsNullOrWhiteSpace(code))
                        stringBuilder.AppendLine(code);
                }
                outputted.Add(symbol.OriginalDefinition);
            }

            if (global.OutputMode.HasFlag(OutputMode.SingleFile))
            {
                //var existingFolder = Path.Combine(output.OutputPath, Constants.OutputFolderName);
                //if (Directory.Exists(existingFolder))
                //Directory.Delete(existingFolder, true);
                string bootCodes = "";
                string codes;
                if (global.OutputMode.HasFlag(OutputMode.Global))
                {
                    StringBuilder stringBuilder = new();
                    StringBuilder bootStringBuilder = new();
                    foreach (var type in global.TypeVisitors.Where(e =>
                    {
                        return global.IsBootClass(e.Key);
                    })
                    .OrderBy(o =>
                    {
                        if (global.HasAttribute(o.Key, typeof(OutputOrderAttribute).FullName, null, false, out var args))
                        {
                            int a = int.Parse(args[0].ToString());
                            return a;
                        }
                        return o.Key.OutputRank(0);
                        //return 0;
                    })
                    //.ThenBy(e => e.Key.ComputeOutputTypeName(global))  //order in a predictable manner so we dont keep losing breakpoint position when debugging
                    )
                    {
                        var writer = global.TypeWriters.GetValueOrDefault(type.Key.OriginalDefinition);
                        if (writer != null)
                        {
                            //var writer = visitor.TypeWriters[type];
                            //foreach (var writer in visitor.TypeWriters.Values)
                            //{
                            var code = writer.Build(1);
                            if (!string.IsNullOrWhiteSpace(code))
                                bootStringBuilder.AppendLine(code);
                            //}
                            outputted.Add(type.Key.OriginalDefinition);
                        }
                    }
                    foreach (var tw in global.TypeVisitors.Where(e =>
                    {
                        return !global.IsBootClass(e.Key);
                        //return !global.HasAttribute(e, typeof(BootAttribute).FullName, null, false, out _);
                    })
                    //.OrderBy(e => e.Key.ComputeOutputTypeName(global))  //order in a predictable manner so we dont keep losing breakpoint position when debugging
                    .OrderBy(o =>
                    {
                        if (global.HasAttribute(o.Key, typeof(OutputOrderAttribute).FullName, null, false, out var args))
                        {
                            int a = int.Parse(args[0].ToString());
                            return a;
                        }
                        return o.Key.OutputRank(0);
                    }))
                    {
                        bool dependsOnSelf = false;
                        SortedOutputBuild(tw.Key, tw.Key, stringBuilder, 2, ref dependsOnSelf);
                    }
                    bootCodes = bootStringBuilder.ToString().Trim();
                    codes = stringBuilder.ToString().Trim();
                }
                else
                {
                    codes = string.Join("\r\n", global.Visitors.Select(v => v.Value.Build(2).Trim()).Where(e => !string.IsNullOrEmpty(e)));
                }
                //                if (global.ModuleInitializers.Count > 0)
                //                {
                //                    codes +=
                //                        @$"
                //        static {Constants.RunModuleInitializersName}()
                //        {{
                //{string.Join("\r\n", global.ModuleInitializers.Select(e => "            " + e))}
                //        }}
                //";
                //                }
                //var metadataBuilder = new ReflectionMetadataBuilder(global, isSystemPrivateCoreLib, contentFiles.Where(e => e.EndsWith(".resx")).ToArray(), embeddedFiles.ToArray());
                var reflectionMetadata = metadataBuilder.FromAssemblySymbol(csCompilation.Assembly);
                var refAssemblySlugs = string.Join(", ", csCompilation.SourceModule.ReferencedAssemblySymbols.Select(a => global.GetAssemblyGlobalSlug(a)));
                var refAssemblyNames = string.Join(", ", csCompilation.SourceModule.ReferencedAssemblySymbols.Select(a => "\"" + a.Name + "\""));
                var outputFileName = Constants.OutputFolderName + Path.DirectorySeparatorChar + project.GetName() + ".js";
                var stream = StringToStream(global.OutputMode.HasFlag(OutputMode.Global) ? @$"
(function ($global, {global.GlobalName}{(refAssemblySlugs.Length > 0 ? ", " : "")}{refAssemblySlugs}) {{
    ""use strict"";
    let _;
    {(isSystemPrivateCoreLib ? "let $asm; function $setasm(v){ $asm = v; }" : "")}
    {bootCodes}
    {(isSystemPrivateCoreLib ? $"{global.GlobalName}.{Constants.AssemblyRegistryName} = {global.GlobalName}.{global.GetAssemblyGlobalSlug(global.Compilation.Assembly)}.System.AppDomain.{Constants.AssemblyRegistryName};" : "")}
	{global.GlobalName}.{Constants.AssemblyRegistryName}(
    ""{project.GetAssemblyName()}"", 
    {JsonSerializer.Serialize(reflectionMetadata, ReflectionMetadataBuilder.SerializationOption)},
    function({Constants.AssemblyRegistryName})
	{{
        {(isSystemPrivateCoreLib ? "$setasm($asm);" : "")}
        {(isSystemPrivateCoreLib ? $"{global.GlobalName}.{global.GetAssemblyGlobalSlug(global.Compilation.Assembly)}.System.AppDomain.{Constants.AppDomainInitialize}($asm)" : "")}
        {codes}
	}});
}})(window, window.{Constants.ProjectName}.{Constants.BootName}(), ...window.{Constants.ProjectName}.$require({refAssemblyNames}))" : codes);
                pendingTask.Add(output.Output(global, outputFileName, stream));
                sortedOutputtedJsFiles.Add(outputFileName);
                packages.Add((outputFileName, stream));
            }
            else
            {
                var existingFile = Path.Combine(output.OutputPath, "js", Path.ChangeExtension(project.GetName(), ".js"));
                if (File.Exists(existingFile))
                    File.Delete(existingFile);
                foreach (var visitor in global.Visitors)
                {
                    var codes = visitor.Value.Build(2).Trim();
                    if (!string.IsNullOrEmpty(codes))
                    {
                        var relative = Utility.GetRelativePath(project.DirectoryPath, visitor.Key.FilePath);
                        var filePath = (project.DirectoryPath.Split('\\', '/').LastOrDefault() ?? "") + Path.ChangeExtension(relative, "js");
                        pendingTask.Add(output.Output(global, filePath, StringToStream(global.OutputMode.HasFlag(OutputMode.Global) ? @$"
(function ({global.GlobalName}, $global) {{
    ""use strict"";
    let _;
	{global.GlobalName}.{Constants.AssemblyRegistryName}(""{project.GetAssemblyName()}"", function({Constants.AssemblyRegistryName})
	{{
        {codes}
	}});
}})(window.{Constants.ProjectName}.{Constants.BootName}(), window)" : codes)));
                        //var path = Path.Combine(outputPath, "js", filePath);
                        ////var path = Path.Combine(outputPath, "js", $"{Path.ChangeExtension(Path.GetFileName(visitor.Key.FilePath), "js")}");
                        //var dir = Path.GetDirectoryName(path);
                        //if (dir != null && !Directory.Exists(dir))
                        //    Directory.CreateDirectory(dir);
                        //File.WriteAllText(Path.Combine(outputPath, path), visitor.Value.ToString());
                        //outputtedFiles.Add(filePath);
                    }
                }
            }
        }

        void WriteOwnSymbolToDisk()
        {
            var yaml = serializer.Serialize(global.Symbols);
            var yamlFileName = project.GetName() + $".Symbols.yaml";
            var yamlStream = StringToStream(yaml);
            pendingTask.Add(output.Output(global, yamlFileName, yamlStream));
            packages.Add((yamlFileName, yamlStream));
        }

        void WriteIndexHtml()
        {
            if (global.MainEntry != null)
            {
                var meta = global.GetRequiredMetadata(global.MainEntry);

                var jss = sortedOutputtedJsFiles
                    .Where(o => indexFile == null || !o.EndsWith("blazor.netjs.js"))//dont add blazor file again to index.html, already there
                    .Where(o => o.StartsWith(Constants.OutputFolderName) && o.EndsWith(".js"))
                    .Select(o => o.Replace(Constants.OutputFolderName + "/", "").Replace(Constants.OutputFolderName + "\\", ""))
                    .Distinct()
                    .ToArray();
                bool hasBlazorNet = jss.Any(o => o.EndsWith("blazor.netjs.js"));
                var insertHead = @$"
    {(string.Join("\r\n    ", jss.Select(o => $"<script type=\"text/javascript\" src=\"{Path.GetFileName(o)}\"{(o.EndsWith("blazor.netjs.js") ? " autostart=\"false\"" : "")}></script>")))}
";
                var insertScripts = @$"
	<script type=""{(global.OutputMode.HasFlag(OutputMode.Global) ? "text/javascript" : "module")}"">
        ({(global.MainEntry.IsAsync ? "async " : "")}function ({global.GlobalName}, $global) {{
            ""use strict"";
            {(hasBlazorNet && indexFile == null ? "Blazor.start();" : "")}
            {StreamToString(output.HtmlScriptContent)}
            {(!global.OutputMode.HasFlag(OutputMode.Global) ? $"import {global.MainEntry.ContainingSymbol.Name} from \"/{Path.GetFileNameWithoutExtension(global.MainEntry.DeclaringSyntaxReferences.First().SyntaxTree.FilePath)}.js\"" : "")}
            {(!global.OutputMode.HasFlag(OutputMode.Global) ? $"{global.MainEntry.ContainingSymbol.Name}.Main();" : "")}
            {(global.OutputMode.HasFlag(OutputMode.Global) ? $"{(global.MainEntry.IsAsync ? "await " : "")}{meta.InvocationName}();" : "")}
        }})(window.{Constants.ProjectName}.{Constants.BootName}(), window)
	</script>
";
                if (indexFile != null)
                {
                    var text = File.ReadAllText(indexFile);
                    text = text.Replace("</head>", insertHead + "\r\n</head>")
                        .Replace("</body>", insertScripts + "\r\n</body>");
                    var relativePath = Utility.GetRelativePath(project.GetFolder(), indexFile);
                    pendingTask.Add(output.Output(global,
                        !relativePath.StartsWith(Constants.OutputFolderName + Path.DirectorySeparatorChar) ? Constants.OutputFolderName + Path.DirectorySeparatorChar + relativePath : relativePath,
                        new MemoryStream(Encoding.UTF8.GetBytes(text))));
                }
                else
                {
                    var index = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{project.Evaluate("AppicationTitle")}</title>
    <style>
        {StreamToString(output.HtmlStyleContent)}
    </style>
{insertHead}
</head>
<body>
    <app id=""app""></app>
        {StreamToString(output.HtmlBodyContent)}
{insertScripts}
</body>
</html>
";
                    pendingTask.Add(output.Output(global, Constants.OutputFolderName + Path.DirectorySeparatorChar + "index.html", StringToStream(index)));
                }
            }
        }

        void WritePackages()
        {
            var localPackageCacheFolder = $"{compiler.LocalPackageCacheFolder}{Path.DirectorySeparatorChar}{project.GetName()}";
            if (Directory.Exists(localPackageCacheFolder))
                Directory.Delete(localPackageCacheFolder, true);
            Directory.CreateDirectory(localPackageCacheFolder);

            using (var fileStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var kv in packages)
                    {
                        var sourceName = kv.IsT0 ? kv.AsT0.Item1 : kv.AsT1.Item1;
                        using (var sourceStream = kv.IsT0 ? new FileStream(kv.AsT0.Item2, FileMode.Open) : kv.AsT1.Item2)
                        {
                            sourceStream.Position = 0;

                            var path = $"{localPackageCacheFolder}{Path.DirectorySeparatorChar}{sourceName}";
                            var dir = Path.GetDirectoryName(path);
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            using (var fs = new FileStream(path, FileMode.Create))
                            {
                                sourceStream.CopyTo(fs);
                            }

                            sourceStream.Position = 0;
                            var zipEntry = zipArchive.CreateEntry(sourceName);
                            using (var zipEntryStream = zipEntry.Open())
                                sourceStream.CopyTo(zipEntryStream);
                        }
                    }
                }
                var zipPackageFolder = project.Evaluate("ZipPackageFolder");
                if (!string.IsNullOrEmpty(zipPackageFolder))
                {
                    if (!Directory.Exists(zipPackageFolder))
                        Directory.CreateDirectory(zipPackageFolder);
                    var targetFramework = project.Evaluate("TargetFramework");
                    var path = $"{zipPackageFolder}{Path.DirectorySeparatorChar}{targetFramework}{Path.DirectorySeparatorChar}{project.GetName()}.package.zip";
                    var folder = Path.GetDirectoryName(path);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    fileStream.Position = 0;
                    using (var fs = new FileStream(path, FileMode.Create))
                    {
                        fileStream.CopyTo(fs);
                    }
                }
            }
        }

        public async Task Build()
        {
            compiler = new CodeCompiler(dotnetPath, dotnetVersion, dotnetSdkPath, dotnetSdkVersion, TempFolder, deSerializer);
            if (!Directory.Exists(output.OutputPath))
                Directory.CreateDirectory(output.OutputPath);

            Clean();

            await BuildRazorFiles();

            await PreBuildSyntaxTree();

            await RewiteFirstPass();

            replacements = new (string, string)[syntaxTrees.Count()];

            await RewiteSecondPass();

            await ReBuildSyntaxTree();

            RunSourceGenerators();

            if (!EmitDll())
                return;

            PrepareToTranspile();

            Transpile();

            WriteOwnMetadataToDisk();

            CopyDependencies();

            CopyOwnAssets();

            WriteTranslatedJsToDisk();

            WriteOwnSymbolToDisk();

            WriteIndexHtml();

            WritePackages();

            await Task.WhenAll(pendingTask);
        }
    }
}
