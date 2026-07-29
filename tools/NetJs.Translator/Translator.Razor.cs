using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.RazorToCSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetJs.Translator
{
    public partial class Translator
    {
        async Task BuildRazorFiles()
        {
            var razorFiles = sourceFiles
                .Where(f => f.EndsWith(".razor") && Path.GetFileName(f) != "_Imports.razor");

            if (razorFiles.Any())
            {
                var rcsFiles = sourceFiles.Where(f => f.EndsWith(".cs") /*&& !f.Contains(GeneratedFolderName)*/).ToList();
                CSharpCompilation? compilation = _project.Compilation;
                if (compilation == null)
                {
                    await $"Precompiling {rcsFiles.Count} files for razor generator...".ProfileAsync(async () =>
                    {
                        compilation = await metadataProvider.CreateCompilation(_project, rcsFiles.ToArray(), null, globalUsings, null, null);
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
                            var context = new ComponentCodeGenerationContext(outStartupCodes, _project)
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
                    var relativePath = Utility.GetRelativePath(_project.DirectoryPath, razorFolder);
                    string? GetRazorImports(string directory)
                    {
                        if (File.Exists(directory + "/_Imports.razor"))
                        {
                            return File.ReadAllText(directory + "/_Imports.razor");
                        }
                        if (directory == _project.DirectoryPath)
                            return null;
                        var upperDirectory = Path.GetFullPath(directory + "/..");
                        return GetRazorImports(upperDirectory);
                    }
                    var imports = GetRazorImports(razorFolder);
                    var context = new ComponentCodeGenerationContext(outStartupCodes, _project)
                    {
                        RazorImports = imports,
                        RazorFile = razorFile,
                        CsFile = csFilePath,
                        Namespace = _project.GetNamespace() + (relativePath != "." ? ("." + relativePath.Replace("/", ".").Replace("\\", ".")) : ""),
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
                    var relativePath = Utility.GetRelativePath(_project.DirectoryPath, csComponent.FilePath);

                    var context = new ComponentCodeGenerationContext(outStartupCodes, _project)
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
                        csFileName = Path.Combine(_output.OutputPath, Utility.GetRelativePath(_project.DirectoryPath, csFileName) + ".g.cs");
                        var folder = Path.GetDirectoryName(csFileName)!;
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);
                        File.WriteAllText(csFileName, code);
                    }

                    if (outStartupCodes.Any())
                    {
                        File.WriteAllText(Path.Combine(_output.OutputPath, "__Startup.g.cs"), @$"
namespace {_project.GetNamespace()}
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
        }
    }
}
