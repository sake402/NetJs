using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetJs.Translator
{
    public partial class Translator
    {
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

        //void DeepCopyFolder(string source, string? relative = null)
        //{
        //    var files = Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories).ToList();
        //    foreach (var file in files)
        //    {
        //        var relativePath = Utility.GetRelativePath(relative ?? source, file);
        //        //var thisPath = Path.Combine(outputPath, "js", relative);
        //        //var existingFileInfo = new FileInfo(file);
        //        output.Output(global, relativePath, file);
        //        if (Path.GetExtension(file).ToLower() == ".js" && !sortedOutputtedJsFiles.Contains(relativePath))
        //            sortedOutputtedJsFiles.Add(relativePath);
        //        //File.Copy(file, thisPath, true);
        //        //File.Copy(file, thisPath, true);
        //        //outputtedFiles.Add(relative);
        //        //if (!outputtedFiles.Contains(file))
        //        //outputtedFiles.Add(file);
        //    }
        //    //foreach (var file in Directory.EnumerateDirectories(source))
        //    //{
        //    //    DeepCopyFolder(file, source);
        //    //}
        //}

        //static IEnumerable<string> SplitByCamelCase(string str)
        //{
        //    if (str.Length <= 2)
        //        yield return str;
        //    int start = 0;
        //    for (int i = 0; i < str.Length; i++)
        //    {
        //        if (i >= 2 && char.IsUpper(str[i]) && char.IsLower(str[i - 1]))
        //        {
        //            yield return str.Substring(start, i - start);
        //            start = i;
        //        }
        //        else if (i >= 2 && char.IsUpper(str[i]) && char.IsUpper(str[i - 1]) && i + 1 < str.Length && char.IsLower(str[i + 1]))
        //        {
        //            yield return str.Substring(start, i - start);
        //            start = i;
        //        }
        //    }
        //    if (start < str.Length)
        //        yield return str.Substring(start, str.Length - start);
        //}

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
