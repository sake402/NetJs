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

namespace NetJs.Translator
{
    public static class Translator
    {
        //const string GeneratedFolderName = "__dotnetJs";
        public static void Build(
            IProject project,
            IProjectOutputProvider output,
            IEnumerable<IIncrementalGenerator>? sourceGenerators = null,
            TextWriter? logTo = null,
            string? tempFolder = null)
        {
            tempFolder ??= Path.GetTempPath() + "NetJs\\";
            logTo.LogTo();
            Random random = new Random();
            var compiler = new CodeCompiler();
            //var dependencies = project.Imports.Select(i => i.ImportedProject);
            var sourceFiles = project.GetSourceFiles();
            var contentFiles = project.GetContentFiles();
            var linkerFiles = project.GetLinkerFiles();
            var embeddedFiles = project.GetEmbeddedFiles();
            //var projectFolder = Path.GetDirectoryName(project.FullPath)!;
            Console.WriteLine($"\r\nProcessing in {project.DirectoryPath}...");
            //var outputPath = Path.Combine(project.DirectoryPath, project.GetOutputPath(), GeneratedFolderName);
            if (!Directory.Exists(output.OutputPath))
                Directory.CreateDirectory(output.OutputPath);
            //var projectInfo = ProjectInfo.GetProjectDefinition(projectFolder);
            var razorFiles = sourceFiles
                .Where(f => f.EndsWith(".razor") && Path.GetFileName(f) != "_Imports.razor");

            if (razorFiles.Any())
            {
                var rcsFiles = sourceFiles.Where(f => f.EndsWith(".cs") /*&& !f.Contains(GeneratedFolderName)*/).ToList();
                CSharpCompilation? compilation = project.Compilation;
                if (compilation == null)
                {
                    $"Precompiling {rcsFiles.Count} files for razor generator...".Profile(() =>
                    {
                        compilation = compiler.GenerateCode(project, rcsFiles.ToArray(), null, out _, out _);
                    });
                }

                Dictionary<string, ComponentCodeGenerationContext> components = new Dictionary<string, ComponentCodeGenerationContext>();

                List<string> outStartupCodes = new List<string>();

                var referencedAssemblySymbols = compilation!.ExternalReferences.Select(r => (IAssemblySymbol?)compilation.GetAssemblyOrModuleSymbol(r));

                //var types = compilation.GetSymbolsWithName(e =>
                //{
                //    return true;
                //}, SymbolFilter.Type);

                static bool InheritsFromComponentBase(ITypeSymbol ts)
                {
                    if (ts.Name == "ComponentBase")
                        return true;
                    if (ts.BaseType != null)
                        return InheritsFromComponentBase(ts.BaseType);
                    return false;
                }

                static IEnumerable<T> GetSymbolsDeep<T>(ISymbol source)
                    where T : ITypeSymbol
                {
                    if (source is INamespaceOrTypeSymbol nsource)
                    {
                        var symbols = nsource.GetMembers();
                        foreach (var t in symbols.OfType<T>())
                            yield return t;
                        foreach (var t in symbols)
                        {
                            var inner = GetSymbolsDeep<T>(t);
                            foreach (var i in inner)
                                yield return i;
                        }
                    }
                }
                foreach (var assembly in referencedAssemblySymbols)
                {
                    if (assembly == null)
                        continue;
                    var types = GetSymbolsDeep<ITypeSymbol>(assembly.GlobalNamespace);
                    foreach (var type in types)
                    {
                        if (type is INamedTypeSymbol ts && InheritsFromComponentBase(ts))
                        {
                            //if (ts.ContainingAssembly.Name==projectInfo.AssemblyName)
                            //continue;
                            var componentClassName = ts.Name;
                            var context = new ComponentCodeGenerationContext(outStartupCodes, project)
                            {
                                //GlobalUsing = imports,
                                //RazorFile = razorFile,
                                //Namespace = projectInfo.Namespace + (relativePath != "." ? ("." + relativePath.Replace("/", ".").Replace("\\", ".")) : ""),
                                ClassName = componentClassName,
                                //SequenceNumber = Random.Shared.Next(int.MinValue + 200000, int.MaxValue - 200000), //make sure the sequnce number wont overflow when incrmented
                                ComponentClassSymbol = ts,
                                //ComponentClassCompilationUnit = ts.Sy
                                KnownComponents = components
                            };
                            components.Add(componentClassName, context);
                        }
                    }
                }

                foreach (var razorFile in razorFiles)
                {
                    var componentClassName = Path.GetFileNameWithoutExtension(razorFile);
                    INamedTypeSymbol? _componentClassSymbol = null;
                    CompilationUnitSyntax? componentClassCompilationSyntax = null;
                    var csFilePath = Path.ChangeExtension(razorFile, "razor.cs");
                    if (File.Exists(csFilePath))
                    {
                        var codeBehindSyntaxTree = compilation.SyntaxTrees.SingleOrDefault(s => s.FilePath == csFilePath);// compiler.GetSyntaxTrees(csFilePath).First();
                        if (codeBehindSyntaxTree != null)
                        {
                            var compilationSemanticModel = compilation.GetSemanticModel(codeBehindSyntaxTree);
                            componentClassCompilationSyntax = (CompilationUnitSyntax)codeBehindSyntaxTree.GetRoot();
                            var _namespace = (NamespaceDeclarationSyntax?)componentClassCompilationSyntax.Members.FirstOrDefault(m => m is NamespaceDeclarationSyntax);
                            if (_namespace != null)
                            {
                                var _class = _namespace.Members.FirstOrDefault(m => m is ClassDeclarationSyntax c && compilationSemanticModel.GetDeclaredSymbol(c)?.Name == componentClassName);
                                if (_class != null)
                                    _componentClassSymbol = (INamedTypeSymbol?)compilationSemanticModel.GetDeclaredSymbol(_class);
                            }
                        }
                    }

                    var razorFolder = Path.GetDirectoryName(razorFile)!;
                    var relativePath = Utility.GetRelativePath(project.DirectoryPath, razorFolder);
                    string? GetRazorImports(string directory)
                    {
                        if (File.Exists(directory + "/_Imports.razor"))
                        {
                            return File.ReadAllText(directory + "/_Imports.razor");
                        }
                        if (directory == project.DirectoryPath)
                            return null;
                        var upperDirectory = Path.GetFullPath(directory + "/..");
                        return GetRazorImports(upperDirectory);
                    }
                    var imports = GetRazorImports(razorFolder);
                    var context = new ComponentCodeGenerationContext(outStartupCodes, project)
                    {
                        RazorImports = imports,
                        RazorFile = razorFile,
                        CsFile = csFilePath,
                        Namespace = project.GetNamespace() + (relativePath != "." ? ("." + relativePath.Replace("/", ".").Replace("\\", ".")) : ""),
                        ClassName = componentClassName,
                        RazorSequenceNumber = random.Next(int.MinValue + 200000, int.MaxValue - 200000), //make sure the sequnce number wont overflow when incrmented
                        ComponentClassSymbol = _componentClassSymbol,
                        ComponentClassCompilationUnit = componentClassCompilationSyntax,
                        KnownComponents = components
                    };
                    components[componentClassName] = context;
                }

                foreach (var csComponent in compilation.SyntaxTrees)
                {
                    if (components.Any(c => c.Value.CsFile == csComponent.FilePath))
                        continue;
                    var componentClassName = Path.GetFileNameWithoutExtension(csComponent.FilePath);
                    INamedTypeSymbol? _componentClassSymbol = null;
                    var compilationSemanticModel = compilation.GetSemanticModel(csComponent);
                    var componentClassCompilationSyntax = (CompilationUnitSyntax)csComponent.GetRoot();
                    var _namespace = (NamespaceDeclarationSyntax?)componentClassCompilationSyntax.Members.FirstOrDefault(m => m is NamespaceDeclarationSyntax);
                    if (_namespace != null)
                    {
                        var _class = _namespace.Members.FirstOrDefault(m => m is ClassDeclarationSyntax c && compilationSemanticModel.GetDeclaredSymbol(c)?.Name == componentClassName);
                        if (_class != null)
                            _componentClassSymbol = (INamedTypeSymbol?)compilationSemanticModel.GetDeclaredSymbol(_class);
                    }

                    if (_componentClassSymbol == null || _componentClassSymbol.Name == "ComponentBase" || !InheritsFromComponentBase(_componentClassSymbol))
                        continue;
                    var csFolder = Path.GetDirectoryName(csComponent.FilePath);
                    var relativePath = Utility.GetRelativePath(project.DirectoryPath, csComponent.FilePath);

                    var context = new ComponentCodeGenerationContext(outStartupCodes, project)
                    {
                        CsFile = csComponent.FilePath,
                        Namespace = _namespace!.Name.ToString(),
                        ClassName = componentClassName,
                        RazorSequenceNumber = random.Next(int.MinValue + 200000, int.MaxValue - 200000), //make sure the sequnce number wont overflow when incremented
                        ComponentClassSymbol = _componentClassSymbol,
                        ComponentClassCompilationUnit = componentClassCompilationSyntax,
                        KnownComponents = components
                    };
                    components[componentClassName] = context;
                }

                $"Generating razor codes".Profile(() =>
                {
                    foreach (var component in components.Where(c => c.Value.RazorFile != null || c.Value.CsFile != null))
                    {
                        if (component.Value.RazorFile != null)
                        {
                            var parser = new RazorComponentParser(component.Value.RazorImports + "\r\n" + File.ReadAllText(component.Value.RazorFile!));
                            var parseResult = parser.Parse();
                            component.Value.RazorComponentSymbol = parseResult;
                        }
                        var code = component.Value.GenerateCode();
                        var csFileName = (component.Value.RazorFile ?? component.Value.CsFile!.Replace(".cs", ""));
                        csFileName = Path.Combine(output.OutputPath, Utility.GetRelativePath(project.DirectoryPath, csFileName) + ".g.cs");
                        var folder = Path.GetDirectoryName(csFileName)!;
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);
                        File.WriteAllText(csFileName, code);
                    }

                    if (outStartupCodes.Any())
                    {
                        File.WriteAllText(Path.Combine(output.OutputPath, "__Startup.g.cs"), @$"
namespace {project.GetNamespace()}
{{
    public static class GeneratedStartup
    {{
        public static void Run()
        {{
{string.Join("\r\n", outStartupCodes.Select(r => "            " + r))}
        }}
    }}
}}
");
                    }
                });
            }

            //var shortNames = GenerateShortNames(compilation);
            //File.WriteAllText(Path.Combine(project.DirectoryPath, "__ShortNames.g.cs"), shortNames);
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var deSerializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            //ResXResourceReader;
            bool isSystemPrivateCoreLib = project.GetAssemblyName() == "NetJs.System.Private.CoreLib";

            var csFiles = sourceFiles.Where(e => e.EndsWith(".cs")).ToList();
            CSharpCompilation csCompilation = default!;
            IEnumerable<MetadataReference> references = default!;
            IEnumerable<string> symbolFiles = default!;
            IEnumerable<SyntaxTree> syntaxTrees = default!;
            $"Prebuilding Syntax Tree".Profile(() =>
            {
                csCompilation = compiler.GenerateCode(project, csFiles.ToArray(), null, out references, out _);
                syntaxTrees = csCompilation.SyntaxTrees;
            });
            //Measure($"Generating Syntax Trees", () =>
            //{
            //    syntaxTrees = compiler.GetSyntaxTrees(project, csFiles.ToArray(), null);
            //});
            (string FilePath, string Source)[] replacements = new (string, string)[syntaxTrees.Count()];
            //Delete all existing files first, we want to check for filename duplicates since this is a flat directory structure
            var projectTempFolder = Path.Combine(tempFolder, project.GetName());
            if (!Directory.Exists(projectTempFolder))
                Directory.CreateDirectory(projectTempFolder);
            var files = Directory.GetFiles(projectTempFolder);
            foreach (var f in files)
                File.Delete(f);
            $"Rewriting".Profile(() =>
            {
                var partialClassGroupings = syntaxTrees
                .SelectMany(s => s.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                .GroupBy(t => t.CreateFullMemberName()!)
                .ToDictionary(e => e.Key, e => e.ToList());
                Parallel.ForEach(syntaxTrees.Select((tree, i) => (tree, i)), new ParallelOptions { MaxDegreeOfParallelism = 10 }, tree =>
                {
                    var visitor = new PreWriterSyntaxVisitor(csCompilation, tree.tree, partialClassGroupings);
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
                    var path = project.DirectoryPath.GetRelativePath(newTree.FilePath);
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
            $"Rebuilding Syntax Tree".Profile(() =>
            {
                csCompilation = compiler.GenerateCode(project, replacements.Select(s => s.FilePath).ToArray(), replacements.Select(s => s.Source).ToArray(), out references, out symbolFiles);
                //var errors = csCompilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error);
            });
            if (sourceGenerators != null)
            {
                $"Running source generators".Profile(() =>
                {
                    var genDriver = CSharpGeneratorDriver.Create(sourceGenerators.ToArray());
                    genDriver.RunGeneratorsAndUpdateCompilation(csCompilation, out var newCompilation, out var diagnostics);
                    csCompilation = (CSharpCompilation)newCompilation;
                });
            }
            var dllStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var docStream = new MemoryStream();
            EmitResult emitResult = default!;
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
                return;
            }
            //foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
            //{
            //    Console.WriteLine(diagnostic.ToString());
            //}
            GlobalCompilationVisitor global = default!;
            ReflectionMetadataBuilder metadataBuilder = default!;
            $"Preparing to transpile".Profile(() =>
            {
                var importedNames = symbolFiles.Select(s => deSerializer.Deserialize<SymbolDescriptor>(File.ReadAllText(s))).ToList();
                global = new GlobalCompilationVisitor(csCompilation, project, importedNames);
                metadataBuilder = new ReflectionMetadataBuilder(global, isSystemPrivateCoreLib, contentFiles.Where(e => e.EndsWith(".resx")).ToArray(), embeddedFiles.ToArray());
                metadataBuilder.InitializeForAssembly(csCompilation.Assembly);
                global.Reflection = metadataBuilder;

            });
            Parallel.ForEach(csCompilation.SyntaxTrees.Select((tree, i) => (tree, i)), new ParallelOptions { MaxDegreeOfParallelism = 1 }, (tree) =>
            {
                $"{tree.i + 1}/{csCompilation.SyntaxTrees.Length}. Transpiling \"{tree.tree.FilePath}\"".Profile(() =>
                {
                    var visitor = new TranslatorSyntaxVisitor(global, tree.tree);
                    ((CSharpSyntaxNode)tree.tree.GetRoot()).Accept(visitor);
                    global.Visitors[tree.tree] = visitor;
                });
            });
            //TODO: If file size grows, consider returning this in a FileStream
            Stream StringToStream(string content)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(content);
                return new MemoryStream(byteArray);
            }

            string StreamToString(Stream stream)
            {
                // Ensure the stream is at the beginning for reading
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }

            void DeepCopyFolder(string source, string? relative = null)
            {
                foreach (var file in Directory.EnumerateFiles(source))
                {
                    var relativePath = Utility.GetRelativePath(relative ?? source, file);
                    //var thisPath = Path.Combine(outputPath, "js", relative);
                    var existingFileInfo = new FileInfo(file);
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        output.Output(global, relativePath, fs, existingFileInfo.LastWriteTime);
                    }
                    //File.Copy(file, thisPath, true);
                    //File.Copy(file, thisPath, true);
                    //outputtedFiles.Add(relative);
                }
                foreach (var file in Directory.EnumerateDirectories(source))
                {
                    DeepCopyFolder(file, source);
                }
            }

            //output the dll and pdb
            dllStream.Position = 0;
            pdbStream.Position = 0;
            docStream.Position = 0;
            output.Output(global, project.GetName() + ".js.dll", dllStream, null);
            output.Output(global, project.GetName() + ".js.pdb", pdbStream, null);
            output.Output(global, project.GetName() + ".js.xml", docStream, null);

            //copy the js folder in every refence over to this js folder
            foreach (var _ref in references)
            {
                var refFolder = Path.GetDirectoryName(_ref.Display);
                var jsFolder = Path.Combine(refFolder!, Constants.OutputFolderName) + "\\";
                if (Directory.Exists(refFolder))
                {
                    DeepCopyFolder(refFolder);
                }
            }

            var jsFiles = contentFiles.Where(e => e.EndsWith(".js")).ToList();
            foreach (var file in jsFiles)
            {
                var existingFileInfo = new FileInfo(file);
                using (var source = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var relativePath = Utility.GetRelativePath(project.GetFolder(), file);
                    //Since wwwroot contains content files like js, css and img.
                    //And what we are producing from cs files are also js files, flatten the wwwroot folder with our output path
                    output.Output(global,
                        !relativePath.StartsWith(Constants.OutputFolderName + "\\") ? Constants.OutputFolderName + "\\" + relativePath : relativePath,
                        source,
                        existingFileInfo.LastWriteTime);
                }
            }

            var cssFiles = contentFiles.Where(e => e.EndsWith(".css")).ToList();
            foreach (var file in cssFiles)
            {
                var existingFileInfo = new FileInfo(file);
                using (var source = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var relativePath = Utility.GetRelativePath(project.GetFolder(), file);
                    output.Output(global,
                        !relativePath.StartsWith(Constants.OutputFolderName + "\\") ? Constants.OutputFolderName + "\\" + relativePath : relativePath,
                        source,
                        existingFileInfo.LastWriteTime);
                    //output.Output(global, relativePath.StartsWith(Constants.OutputFolderName + "\\") ? relativePath.Substring((Constants.OutputFolderName + "\\").Length) : relativePath, source, existingFileInfo.LastWriteTime);
                }
            }

            void RecursiveDependentTypes(INamedTypeSymbol symbol, HashSet<INamedTypeSymbol> found, int depth)
            {
                if (depth != 0)
                {
                    if (!found.Add(symbol))
                        return;
                }
                if (symbol.BaseType != null)
                {
                    //found.Add(symbol.BaseType);
                    RecursiveDependentTypes(symbol.BaseType, found, depth + 1);
                }
                if (symbol.Arity > 0)
                {
                    foreach (var t in symbol.TypeArguments)
                    {
                        if (t is INamedTypeSymbol genericArgument)
                        {
                            //found.Add(genericArgument);
                            RecursiveDependentTypes(genericArgument, found, depth + 1);
                        }
                    }
                }
                foreach (var i in symbol.AllInterfaces)
                {
                    //found.Add(i);
                    RecursiveDependentTypes(i, found, depth + 1);
                }
            }

            IEnumerable<INamedTypeSymbol> DependentTypes(INamedTypeSymbol symbol)
            {
                var found = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                RecursiveDependentTypes(symbol, found, 0);
                return found;
            }

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
                    foreach (var type in global.TypeVisitors.Keys.Where(e =>
                    {
                        return global.IsBootClass(e);
                    }).OrderBy(o =>
                    {
                        if (global.HasAttribute(o, typeof(OutputOrderAttribute).FullName, null, false, out var args))
                        {
                            int a = int.Parse(args[0].ToString());
                            return a;
                        }
                        return o.OutputRank(0);
                        //return 0;
                    }))
                    {
                        var writer = global.TypeWriters.GetValueOrDefault(type.OriginalDefinition);
                        if (writer != null)
                        {
                            //var writer = visitor.TypeWriters[type];
                            //foreach (var writer in visitor.TypeWriters.Values)
                            //{
                            var code = writer.Build(1);
                            if (!string.IsNullOrWhiteSpace(code))
                                bootStringBuilder.AppendLine(code);
                            //}
                            outputted.Add(type.OriginalDefinition);
                        }
                    }
                    foreach (var tw in global.TypeVisitors.Keys.Where(e =>
                    {
                        return !global.IsBootClass(e);
                        //return !global.HasAttribute(e, typeof(BootAttribute).FullName, null, false, out _);
                    }).OrderBy(o =>
                    {
                        if (global.HasAttribute(o, typeof(OutputOrderAttribute).FullName, null, false, out var args))
                        {
                            int a = int.Parse(args[0].ToString());
                            return a;
                        }
                        return o.OutputRank(0);
                    }))
                    {
                        bool dependsOnSelf = false;
                        SortedOutputBuild(tw, tw, stringBuilder, 2, ref dependsOnSelf);
                    }
                    bootCodes = bootStringBuilder.ToString().Trim();
                    codes = stringBuilder.ToString().Trim();
                }
                else
                {
                    codes = string.Join("\r\n", global.Visitors.Select(v => v.Value.Build(2).Trim()).Where(e => !string.IsNullOrEmpty(e)));
                }
                if (isSystemPrivateCoreLib)
                {
                    bootCodes += @$"

    {global.GlobalName}.{global.GetAssemblyGlobalSlug(global.Compilation.Assembly)}.System.AppDomain.{Constants.AppDomainInitialize}()
";
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
                output.Output(global, Constants.OutputFolderName + "/" + project.GetName() + ".js", StringToStream(global.OutputMode.HasFlag(OutputMode.Global) ? @$"
(function ({global.GlobalName}, $global) {{
    ""use strict"";
    let _;
    {(isSystemPrivateCoreLib ? "let $asm; function $setasm(v){ $asm = v; }" : "")}
    {bootCodes}
    $.$meta(""{project.GetAssemblyName()}"", {JsonSerializer.Serialize(reflectionMetadata, ReflectionMetadataBuilder.SerializationOption)});

	{global.GlobalName}.{Constants.AssemblyRegistryName}(""{project.GetAssemblyName()}"", function({Constants.AssemblyRegistryName})
	{{
        {(isSystemPrivateCoreLib ? "$setasm($asm);" : "")}
        {codes}
	}});
}})(window.{Constants.ProjectName}.{Constants.BootName}(), window)" : codes), null);
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
                        output.Output(global, filePath, StringToStream(global.OutputMode.HasFlag(OutputMode.Global) ? @$"
(function ({global.GlobalName}, $global) {{
    ""use strict"";
    let _;
	{global.GlobalName}.{Constants.AssemblyRegistryName}(""{project.GetAssemblyName()}"", function({Constants.AssemblyRegistryName})
	{{
        {codes}
	}});
}})(window.{Constants.ProjectName}.{Constants.BootName}(), window)" : codes), null);
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

            var yaml = serializer.Serialize(global.Symbols);
            output.Output(global, project.GetName() + $".SymbolNames.yaml", StringToStream(yaml), null);

            if (global.MainEntry != null)
            {
                var meta = global.GetRequiredMetadata(global.MainEntry);
                var jss = output.OutputtedFiles.Where(o => o.EndsWith(".js")).Select(o => o.Replace(Constants.OutputFolderName + "/", "").Replace(Constants.OutputFolderName + "\\", "")).Distinct();
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
    {(string.Join("\r\n    ", jss.Select(o => $"<script type=\"text/javascript\" src=\"{o}\"></script>")))}
	<script type=""{(global.OutputMode.HasFlag(OutputMode.Global) ? "text/javascript" : "module")}"">
        (function ({global.GlobalName}, $global) {{
            ""use strict"";
            {StreamToString(output.HtmlScriptContent)}
            {(!global.OutputMode.HasFlag(OutputMode.Global) ? $"import {global.MainEntry.ContainingSymbol.Name} from \"/{Path.GetFileNameWithoutExtension(global.MainEntry.DeclaringSyntaxReferences.First().SyntaxTree.FilePath)}.js\"" : "")}
            {(!global.OutputMode.HasFlag(OutputMode.Global) ? $"{global.MainEntry.ContainingSymbol.Name}.Main();" : "")}
            {(global.OutputMode.HasFlag(OutputMode.Global) ? $"{meta.InvocationName}();" : "")}
        }})(window.{Constants.ProjectName}.{Constants.BootName}(), window)
	</script>
</head>
<body>
        {StreamToString(output.HtmlBodyContent)}
</body>
</html>
";
                output.Output(global, Constants.OutputFolderName + "/" + project.GetName() + ".html", StringToStream(index), null);
            }
        }



        static IEnumerable<string> SplitByCamelCase(string str)
        {
            if (str.Length <= 2)
                yield return str;
            int start = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (i >= 2 && char.IsUpper(str[i]) && char.IsLower(str[i - 1]))
                {
                    yield return str.Substring(start, i - start);
                    start = i;
                }
                else if (i >= 2 && char.IsUpper(str[i]) && char.IsUpper(str[i - 1]) && i + 1 < str.Length && char.IsLower(str[i + 1]))
                {
                    yield return str.Substring(start, i - start);
                    start = i;
                }
            }
            if (start < str.Length)
                yield return str.Substring(start, str.Length - start);
        }

        //        public static string GenerateShortNames(Compilation compilation)
        //        {
        //            string GetShortName(TypeDeclarationSyntax _class, List<string> takenNames, out NamespaceDeclarationSyntax? _namespace, bool addToTaken = true)
        //            {
        //                string? parentName = null;
        //                if (_class.Parent is TypeDeclarationSyntax pClass)
        //                {
        //                    parentName = GetShortName(pClass, takenNames, out _namespace, false);
        //                }
        //                else
        //                {
        //                    _namespace = (NamespaceDeclarationSyntax?)_class.Parent;
        //                    var mnamespace = _namespace?.Name.ToString();
        //                    parentName = mnamespace != null ? string.Join("", mnamespace.Split('.').Select(p => p[0])) : null;
        //                }
        //                var _className = _class.Identifier.ToString();
        //                var classNameTokens = SplitByCamelCase(_className).ToArray();
        //                var shortName = parentName + "_" + string.Join("", classNameTokens.Select(c => c[0]));
        //                if (_class.TypeParameterList?.Parameters.Any() ?? false)
        //                {
        //                    shortName += "$" + _class.TypeParameterList.Parameters.Count;
        //                }
        //                //int classN_i = 0;
        //                //while (takenNames.Contains(shortName) && classN_i < classNameTokens.Length)
        //                //{
        //                //    shortName += classNameTokens[classN_i][0];
        //                //    classN_i++;
        //                //}
        //                if (addToTaken && takenNames.Contains(shortName))
        //                {
        //                    var likes = takenNames.Count(t => t.StartsWith(shortName));
        //                    shortName += "$" + (likes + 1);
        //                }
        //                if (addToTaken)
        //                    takenNames.Add(shortName);
        //                return shortName;
        //            }
        //            List<string> takenNames = new List<string>();
        //            string ConvertClass(TypeDeclarationSyntax type, int depth)
        //            {
        //                var shortName = GetShortName(type, takenNames, out _);
        //                var innerClasses = string.Join("\r\n", type.ChildNodes()
        //                    .Where(d => d is TypeDeclarationSyntax)
        //                    .Cast<TypeDeclarationSyntax>()
        //                    .Select(i => ConvertClass(i, depth + 1)));
        //                var tab = string.Join("", Enumerable.Range(1, depth + 1).Select(t => "    "));
        //                var modifiers = type.Modifiers.ToString();
        //                if (!modifiers.Contains("partial"))
        //                    modifiers += " partial";
        //                return $@"{tab}[Name(""{shortName.ToLower()}"")]
        //{tab}{modifiers} {(type is StructDeclarationSyntax ? "struct" : type is ClassDeclarationSyntax ? "class" : "interface")} {type.Identifier}{((type.TypeParameterList?.Parameters.Any() ?? false) ? $"<{string.Join(", ", type.TypeParameterList.Parameters.Select(p => p.Identifier))}>" : "")}
        //{tab}{{
        //{tab}{innerClasses}
        //{tab}}}";
        //            }
        //            var shortNames = @"
        //#if RELEASE
        //using dotnetJs;
        //" + string.Join("\r\n\r\n", compilation.SyntaxTrees.SelectMany(syntax =>
        //            {
        //                var compilationSemanticModel = compilation.GetSemanticModel(syntax);
        //                var componentClassCompilationSyntax = (CompilationUnitSyntax)syntax.GetRoot();
        //                var _classes = componentClassCompilationSyntax.DescendantNodes().Where(d => d is TypeDeclarationSyntax).Where(t => t.Parent is NamespaceDeclarationSyntax).Cast<TypeDeclarationSyntax>();
        //                return _classes;
        //            }).DistinctBy(c => c.Identifier.ToString())
        //            .Select(type =>
        //        {
        //            var _namespace = ((NamespaceDeclarationSyntax?)type.Parent)?.Name.ToString();
        //            var code = $@"
        //namespace {_namespace}
        //{{
        //{ConvertClass(type, 0)}
        //}}";
        //            return code;
        //        })) + "\r\n\r\n#endif";
        //            return shortNames;
        //        }
    }
}
