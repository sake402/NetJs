using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NetJs.Translator;

namespace NetJs.Compiler
{

    public class LibraryDoctor
    {
        string _dotnetJsSolutionPath;
        public string DotnetGitRoot { get; }
        string _dotnetRuntimeRoot;
        string _repoRoot;
        string _coreLibRoot;
        string _coreLibSharedDir;
        string _commonPath;
        string _sharedSourceRoot;
        string _bclSourcesRoot;
        string _librariesProjectRoot;
        string _privateCoreLibSharedProjectDirectory;
        public LibraryDoctor(string dotnetJsSolutionPath)
        {
            _dotnetJsSolutionPath = dotnetJsSolutionPath;
            var directoryBuildProps = Path.Combine(dotnetJsSolutionPath, "libraries", "Directory.Build.props");
            var fileContent = File.ReadAllText(directoryBuildProps);
            DotnetGitRoot = Regex.Match(fileContent, ".?<DotnetGitRoot>(.+)</DotnetGitRoot>.?").Groups[1].Value;
            _dotnetRuntimeRoot = Regex.Match(fileContent, ".?<DotnetRuntimeRoot>(.+)</DotnetRuntimeRoot>.?").Groups[1].Value.Replace("$(DotnetGitRoot)", DotnetGitRoot).Replace("/", "\\");
            _coreLibRoot = Regex.Match(fileContent, ".?<CoreLibRoot>(.+)</CoreLibRoot>.?").Groups[1].Value.Replace("$(DotnetRuntimeRoot)", _dotnetRuntimeRoot).Replace("/", "\\"); ;
            _coreLibSharedDir = Regex.Match(fileContent, ".?<CoreLibSharedDir>(.+)</CoreLibSharedDir>.?").Groups[1].Value.Replace("$(DotnetRuntimeRoot)", _dotnetRuntimeRoot).Replace("/", "\\"); ;
            _repoRoot = Regex.Match(fileContent, ".?<RepoRoot>(.+)</RepoRoot>.?").Groups[1].Value.Replace("$(DotnetGitRoot)", DotnetGitRoot).Replace("/", "\\"); ;
            _commonPath = Regex.Match(fileContent, ".?<CommonPath>(.+)</CommonPath>.?").Groups[1].Value.Replace("/", "\\"); ;
            _sharedSourceRoot = Regex.Match(fileContent, ".?<SharedSourceRoot>(.+)</SharedSourceRoot>.?").Groups[1].Value.Replace("/", "\\"); ;
            _bclSourcesRoot = Regex.Match(fileContent, ".?<BclSourcesRoot>(.+)</BclSourcesRoot>.?").Groups[1].Value.Replace("/", "\\"); ;
            _librariesProjectRoot = Regex.Match(fileContent, ".?<LibrariesProjectRoot>(.+)</LibrariesProjectRoot>.?").Groups[1].Value.Replace("/", "\\"); ;
            _privateCoreLibSharedProjectDirectory = Regex.Match(fileContent, ".?<PrivateCoreLibSharedProjectDirectory>(.+)</PrivateCoreLibSharedProjectDirectory>.?").Groups[1].Value.Replace("/", "\\"); ;

            //var MsBuildThisProjectFile = $"$(DotnetRuntimeRoot)src/mono/System.Private.CoreLib/System.Private.CoreLib.csproj";
            //var MsBuildThisFileDirectory = $"$(DotnetRuntimeRoot)src/mono/System.Private.CoreLib/";
        }

        public static void GenerateStaticResource(XElement? doc, string projectName, string sourceProjectDirectory, string newProjectDirectory, string? outputResourceFileName = null)
        {
            var resx = Directory.EnumerateFiles(sourceProjectDirectory, "*.resx", SearchOption.AllDirectories)
                .OrderBy(o => o.EndsWith("Strings.resx") ? 1 : int.MaxValue)
                .GroupBy(srPath =>
            {
                var className = "SR";
                if (Path.GetFileNameWithoutExtension(srPath) != "Strings")
                {
                    className = Path.GetFileNameWithoutExtension(srPath);
                }
                return className;
            });
            foreach (var srPath in resx)
            //var srPath = Path.Join(projectFolderPath, "Resources", "Strings.resx");
            //if (File.Exists(srPath))
            {
                var className = srPath.Key;
                var keyValues = srPath.SelectMany(s =>
                {
                    var srxml = File.ReadAllText(s);
                    var srdoc = XElement.Parse(srxml);
                    var keyValues = srdoc.Elements("data")
                    .Where(r => r.Attribute("name") is not null && r.Element("value") is not null);
                    return keyValues;
                }).DistinctBy(e => e.Attribute("name")!.Value)
                .ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")!.Value)
                .Select(kv =>
                {
                    int countParams = 0;
                    for (int i = 0; i < kv.Value.Length; i++)
                    {
                        if (kv.Value[i] == '{')
                        {
                            if (kv.Value[i + 1] != '{')
                                countParams++;
                            else
                                i++;
                        }
                    }
                    return @$"
        /// <summary>
{string.Join("\r\n", kv.Value.Trim().Split(['\n']).Select(e => $"        /// {e}"))}
        /// </summary>
        internal static string {kv.Key}
        {{
            get
            {{
                return ResourceManager.GetString(""{kv.Key}"", resourceCulture);
            }}
        }}
" + (countParams > 0 ? @$"

        internal static string Format{kv.Key}({string.Join(", ", Enumerable.Range(1, countParams).Select(c => $"object arg{c}"))})
        {{
            return string.Format({kv.Key}, {string.Join(", ", Enumerable.Range(1, countParams).Select(c => $"arg{c}"))});
        }}" : "");
                });
                var hasNamespace = className == "SR";
                var SRTemplate = $@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

{(hasNamespace ? $"namespace {(className == "SR" ? "System" : projectName)}" : "")}
{(hasNamespace ? $"{{" : "")}

    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Resources.Tools.StronglyTypedResourceBuilder"", ""17.0.0.0"")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal static partial class {className}
    {{

        private static global::System.Resources.ResourceManager s_resourceManager;

        private static global::System.Globalization.CultureInfo resourceCulture;

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {{
            get
            {{
                if (object.ReferenceEquals(s_resourceManager, null))
                {{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(""{projectName}"", typeof({className}).Assembly);
                    s_resourceManager = temp;
                }}
                return s_resourceManager;
            }}
        }}

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {{
            get
            {{
                return resourceCulture;
            }}
            set
            {{
                resourceCulture = value;
            }}
        }}
        {string.Join("\r\n\r\n", keyValues)}
    }}
{(hasNamespace ? $"}}" : "")}
";
                var sroutPath = Path.Join(newProjectDirectory, $"{outputResourceFileName ?? className}.cs");
                File.WriteAllText(sroutPath, SRTemplate);
                //var itemGroup = new XElement("ItemGroup");
                //var content = new XElement("Content");
                //content.Add(new XAttribute("Include", $"{projectFolderPathAsVariable}\\Resources\\Strings.resx"));
                //content.Add(new XElement("GenerateSource", "true"));
                //content.Add(new XElement("ClassName", "System.SR"));
                //content.Add(new XElement("Generator", "ResXFileCodeGenerator"));
                ////content.Add(new XElement("LastGenOutput", "SR.cs"));
                //itemGroup.Add(content);
                //doc.Add(itemGroup);
            }
            if (resx.Any(e => e.Key == "SR"))
            {
                //Add reference to partial SR class
                //<Compile Include="$(DotnetRuntimeRoot)src/libraries/Common/src/System/SR.cs"></Compile>
                var itemGroup = new XElement("ItemGroup");
                var content = new XElement("Compile");
                content.Add(new XAttribute("Include", $"$(DotnetRuntimeRoot)src/libraries/Common/src/System/SR.cs"));
                itemGroup.Add(content);
                doc.Add(itemGroup);
            }
        }

        public static void GenerateStaticResourceInlined(XElement? doc, string projectName, string sourceProjectDirectory, string newProjectDirectory, string? outputResourceFileName = null)
        {
            var resx = Directory.EnumerateFiles(sourceProjectDirectory, "*.resx", SearchOption.AllDirectories)
                .OrderBy(o => o.EndsWith("Strings.resx") ? 1 : int.MaxValue)
                .GroupBy(srPath =>
                {
                    var className = "SR";
                    if (Path.GetFileNameWithoutExtension(srPath) != "Strings")
                    {
                        className = Path.GetFileNameWithoutExtension(srPath);
                    }
                    return className;
                });
            foreach (var srPath in resx)
            //var srPath = Path.Join(projectFolderPath, "Resources", "Strings.resx");
            //if (File.Exists(srPath))
            {
                var className = srPath.Key;
                var keyValues = srPath.SelectMany(s =>
                {
                    var srxml = File.ReadAllText(s);
                    var srdoc = XElement.Parse(srxml);
                    var keyValues = srdoc.Elements("data")
                    .Where(r => r.Attribute("name") is not null && r.Element("value") is not null);
                    return keyValues;
                }).DistinctBy(e => e.Attribute("name")!.Value)
                .ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")!.Value)
                .Select(kv =>
                {
                    int countParams = 0;
                    for (int i = 0; i < kv.Value.Length; i++)
                    {
                        if (kv.Value[i] == '{')
                        {
                            if (kv.Value[i + 1] != '{')
                                countParams++;
                            else
                                i++;
                        }
                    }
                    return @$"
        /// <summary>
{string.Join("\r\n", kv.Value.Trim().Split(['\n']).Select(e => $"        /// {e}"))}
        /// </summary>
        internal static string {kv.Key}
        {{
            get
            {{
                return ""{kv.Value.EscapeString()}"";
            }}
        }}
" + (countParams > 0 ? @$"

        internal static string Format{kv.Key}({string.Join(", ", Enumerable.Range(1, countParams).Select(c => $"object arg{c}"))})
        {{
            return string.Format(""{kv.Value.EscapeString()}"", {string.Join(", ", Enumerable.Range(1, countParams).Select(c => $"arg{c}"))});
        }}" : "");
                });
                var hasNamespace = className == "SR";
                var SRTemplate = $@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

{(hasNamespace ? $"namespace {(className == "SR" ? "System" : projectName)}" : "")}
{(hasNamespace ? $"{{" : "")}

    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Resources.Tools.StronglyTypedResourceBuilder"", ""17.0.0.0"")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal static partial class {className}
    {{

        private static global::System.Resources.ResourceManager s_resourceManager;

        private static global::System.Globalization.CultureInfo resourceCulture;

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {{
            get
            {{
                if (object.ReferenceEquals(s_resourceManager, null))
                {{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(""{projectName}"", typeof({className}).Assembly);
                    s_resourceManager = temp;
                }}
                return s_resourceManager;
            }}
        }}

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {{
            get
            {{
                return resourceCulture;
            }}
            set
            {{
                resourceCulture = value;
            }}
        }}
        {string.Join("\r\n\r\n", keyValues)}
    }}
{(hasNamespace ? $"}}" : "")}
";
                var sroutPath = Path.Join(newProjectDirectory, $"{outputResourceFileName ?? className}.cs");
                File.WriteAllText(sroutPath, SRTemplate);
                //var itemGroup = new XElement("ItemGroup");
                //var content = new XElement("Content");
                //content.Add(new XAttribute("Include", $"{projectFolderPathAsVariable}\\Resources\\Strings.resx"));
                //content.Add(new XElement("GenerateSource", "true"));
                //content.Add(new XElement("ClassName", "System.SR"));
                //content.Add(new XElement("Generator", "ResXFileCodeGenerator"));
                ////content.Add(new XElement("LastGenOutput", "SR.cs"));
                //itemGroup.Add(content);
                //doc.Add(itemGroup);
            }
            if (doc != null && resx.Any(e => e.Key == "SR"))
            {
                //Add reference to partial SR class
                //<Compile Include="$(DotnetRuntimeRoot)src/libraries/Common/src/System/SR.cs"></Compile>
                var itemGroup = new XElement("ItemGroup");
                var content = new XElement("Compile");
                content.Add(new XAttribute("Include", $"$(DotnetRuntimeRoot)src/libraries/Common/src/System/SR.cs"));
                itemGroup.Add(content);
                doc.Add(itemGroup);
            }
        }

        //public async Task<string> Doctor(string originalCsProjectFilePath,
        //    Dictionary<string, string> addPropertyGroups,
        //    Dictionary<string, List<string>> addCompilations,
        //    Dictionary<string, string> variables,
        //    Dictionary<string, List<string>> removePath,
        //    List<string> addReferences)
        //{
        //    var projectFileName = Path.GetFileName(originalCsProjectFilePath);
        //    var projectName = Path.GetFileNameWithoutExtension(originalCsProjectFilePath);
        //    var projectFolderPath = Path.GetDirectoryName(originalCsProjectFilePath)!;
        //    var projectFolderPathAsVariable = projectFolderPath.Replace("E:\\dotnet\\runtime\\", $"$(DotnetRuntimeRoot)");
        //    var projectFolderName = Path.GetFileName(projectFolderPath);

        //    var newProjectDirectory = Path.Join(_dotnetJsSolutionPath, "libraries", projectName);
        //    bool isNewProject = false; ;
        //    if (!Directory.Exists(newProjectDirectory))
        //    {
        //        isNewProject = true;
        //        Directory.CreateDirectory(newProjectDirectory);
        //    }

        //    var xml = File.ReadAllText(originalCsProjectFilePath);

        //    if (variables.Count > 0)
        //    {
        //        foreach (var kv in variables)
        //        {
        //            xml = xml.Replace($"$({kv.Key})", kv.Value);
        //        }
        //    }

        //    var doc = XElement.Parse(xml);

        //    if (removePath.Count > 0)
        //    {
        //        foreach (var r in removePath)
        //        {
        //            var key = r.Key.Split('@');
        //            foreach (var value in r.Value)
        //            {
        //                var path = $"{key[0]}[@{key[1]}=\"{value}\"]";
        //                var nodes = doc.XPathSelectElements(path);
        //                foreach (var node in nodes)
        //                {
        //                    XComment comment = new XComment(node.ToString());
        //                    node.AddBeforeSelf(comment);
        //                }
        //                nodes.Remove();
        //            }
        //        }
        //    }

        //    const bool forceTargetBrowserOnly = true;

        //    var tfmMulti = doc.XPathSelectElement("//PropertyGroup/TargetFrameworks");
        //    bool isMultiTarget = false;
        //    bool targetsBrowser = false;
        //    if (tfmMulti != null)
        //    {
        //        isMultiTarget = tfmMulti.Value.Contains(";");
        //        if (forceTargetBrowserOnly)
        //        {
        //            var tfmComment = new XComment(tfmMulti!.ToString());
        //            tfmMulti.AddBeforeSelf(tfmComment);
        //            tfmMulti.Remove();
        //        }
        //        else if (tfmMulti.Value.Contains("-browser"))
        //        {
        //            targetsBrowser = true;
        //            tfmMulti.Value = "$(NetCoreAppCurrent)-browser;$(NetCoreAppCurrent)";
        //        }
        //        else
        //        {
        //            tfmMulti.Value = "$(NetCoreAppCurrent)";
        //        }

        //    }

        //    if (forceTargetBrowserOnly)
        //    {
        //        var tfmSingle = doc.XPathSelectElement("//PropertyGroup/TargetFramework");
        //        if (tfmSingle != null)
        //        {
        //            var tfmComment = new XComment(tfmSingle!.ToString());
        //            tfmSingle.AddBeforeSelf(tfmComment);
        //            tfmSingle.Remove();
        //        }
        //    }
        //    //var tpi = doc.XPathSelectElement("//PropertyGroup/TargetPlatformIdentifier");
        //    //if (tpi != null)
        //    //{
        //    //    //tpi.Value="browser";
        //    //    var tfmComment = new XComment(tpi!.ToString());
        //    //    tpi.AddBeforeSelf(tfmComment);
        //    //    tpi.Remove();
        //    //}

        //    string[] includes = ["//ItemGroup/Compile", "//ItemGroup/Compile/DependentUpon", "//ItemGroup/AsnXml"];

        //    foreach (var includePath in includes)
        //    {
        //        doc.XPathSelectElements(includePath).FirstOrDefault(e =>
        //        {
        //            var include = e.Attribute("Include");
        //            if (include != null)
        //            {
        //                if (!include.Value.StartsWith("$("))
        //                {
        //                    include.Value = $"{projectFolderPathAsVariable}\\{include.Value}";
        //                }
        //            }
        //            var remove = e.Attribute("Remove");
        //            if (remove != null)
        //            {
        //                if (!remove.Value.StartsWith("$("))
        //                {
        //                    remove.Value = $"{projectFolderPathAsVariable}\\{remove.Value}";
        //                }
        //            }
        //            return false;
        //        });
        //    }
        //    doc.XPathSelectElements("//ItemGroup/ProjectReference").FirstOrDefault(e =>
        //    {
        //        if (e.Attribute("OutputItemType")?.Value == "Analyzer")
        //        {
        //            //Skip analyzers
        //            return false;
        //        }
        //        var include = e.Attribute("Include");
        //        if (include != null)
        //        {
        //            if (include.Value.StartsWith("$(LibrariesProjectRoot)"))
        //            {
        //                var newPath = include.Value.Replace("$(LibrariesProjectRoot)", "$(NewLibrariesProjectRoot)");
        //                var split = newPath.Split('\\');
        //                //split[split.Length-1] = "NetJs." + split[split.Length - 1];
        //                //include.Value = string.Join("\\", split);
        //                include.Value = $"$(NewLibrariesProjectRoot){Path.GetFileNameWithoutExtension(split[split.Length - 1])}\\NetJs.{split[split.Length - 1]}";
        //            }
        //        }
        //        return false;
        //    });

        //    if (addPropertyGroups.Count > 0)
        //    {
        //        var propertyGroup = doc.XPathSelectElement("//PropertyGroup");
        //        foreach (var kv in addPropertyGroups)
        //        {
        //            propertyGroup!.Add(new XElement(kv.Key, kv.Value));
        //        }
        //    }

        //    if (addCompilations.Count > 0)
        //    {
        //        foreach (var conditionalCompilation in addCompilations)
        //        {
        //            var itemGroup = new XElement("ItemGroup");
        //            if (!string.IsNullOrEmpty(conditionalCompilation.Key))
        //            {
        //                itemGroup.Add(new XAttribute("Condition", $"'$(TargetPlatformIdentifier)' == '{conditionalCompilation.Key.Trim('_')}'"));
        //            }
        //            foreach (var kv in conditionalCompilation.Value)
        //            {
        //                var compile = new XElement("Compile");
        //                compile.Add(new XAttribute("Include", kv));
        //                itemGroup!.Add(compile);
        //            }
        //            doc.Add(itemGroup);
        //        }
        //    }
        //    if (addReferences.Count > 0)
        //    {
        //        var itemGroup = new XElement("ItemGroup");
        //        foreach (var kv in addReferences)
        //        {
        //            var compile = new XElement("ProjectReference");
        //            compile.Add(new XAttribute("Include", kv));
        //            itemGroup!.Add(compile);
        //        }
        //        doc.Add(itemGroup);
        //    }

        //    GenerateStaticResource(doc, projectName, projectFolderPath, newProjectDirectory);

        //    var doctored = doc.ToString();
        //    var outPath = Path.Join(newProjectDirectory, $"NetJs.{projectFileName}");
        //    File.WriteAllText(outPath, doc.ToString());


        //    if (isNewProject)
        //    {
        //        //Make sure the project is added to solution
        //        await $"cd {newProjectDirectory} & dotnet sln ../../dotnetJs.sln add NetJs.{projectFileName} --solution-folder libraries".CLI();
        //    }

        //    return $"$(NewLibrariesProjectRoot){projectName}\\NetJs.{projectFileName}";
        //}


        public async Task<string> Doctor(string netJsPath, XElement sourceNode, IEnumerable<string> allProjects)
        {
            var csProj = sourceNode.Attribute("Include")!.Value;
            var originalCsProjectFilePath = csProj.Replace("$(DotnetGitRoot)", $"{netJsPath}\\dotnet\\").Replace("\\\\", "\\").Replace("/\\", "\\").Replace("\\/", "\\");
            Console.WriteLine($"Doctoring {originalCsProjectFilePath}...");
            var projectFileName = Path.GetFileName(originalCsProjectFilePath);
            var projectName = Path.GetFileNameWithoutExtension(originalCsProjectFilePath);
            var projectFolderPath = Path.GetDirectoryName(originalCsProjectFilePath)!;
            var projectFolderPathAsVariable = projectFolderPath
                .Replace($"{netJsPath}\\dotnet\\runtime\\", $"$(DotnetRuntimeRoot)")
                .Replace($"{netJsPath}\\dotnet\\aspnetcore\\", $"$(RepoRoot)");
            var projectFolderName = Path.GetFileName(projectFolderPath);

            var newProjectDirectory = Path.Join(_dotnetJsSolutionPath, "libraries", projectName);
            bool isNewProject = false; ;
            if (!Directory.Exists(newProjectDirectory))
            {
                isNewProject = true;
                Directory.CreateDirectory(newProjectDirectory);
            }

            var xml = File.ReadAllText(originalCsProjectFilePath);
            var destinationDocument = XElement.Parse(xml);

            var destinationPropertyGroup = destinationDocument.XPathSelectElement("//PropertyGroup");
            if (destinationPropertyGroup == null)
            {
                destinationPropertyGroup = new XElement("PropertyGroup");
                destinationDocument.AddFirst(destinationPropertyGroup);
            }


            var directoryBuildProps = Path.Combine(projectFolderPath + "/..", "Directory.Build.props");
            if (File.Exists(directoryBuildProps))
            {
                var dirBuildPropsContent = File.ReadAllText(directoryBuildProps);
                var dirBuildPropsDoc = XElement.Parse(dirBuildPropsContent);
                var dirBuildPropsPropertyGroups = dirBuildPropsDoc.XPathSelectElements("//PropertyGroup");
                foreach (var dirBuildPropsPropertyGroup in dirBuildPropsPropertyGroups)
                {
                    foreach (var property in dirBuildPropsPropertyGroup.Elements())
                    {
                        var existing = destinationPropertyGroup.Elements().FirstOrDefault(e => e.Name == property.Name);
                        if (existing == null)
                        {
                            destinationPropertyGroup.Add(new XElement(property.Name, property.Value));
                        }
                    }
                }
            }

            var sourcePropertyGroup = sourceNode.XPathSelectElement("PropertyGroup");

            if (sourcePropertyGroup != null)
            {
                foreach (var property in sourcePropertyGroup.Elements())
                {
                    var existing = destinationPropertyGroup.Elements().FirstOrDefault(e => e.Name == property.Name);
                    if (existing != null)
                    {
                        XComment comment = new XComment(existing.ToString());
                        existing.AddBeforeSelf(comment);
                        existing.Remove();
                    }
                    destinationPropertyGroup.Add(new XElement(property.Name, property.Value));
                }
            }

            var sourceItemGroups = sourceNode.XPathSelectElements("ItemGroup");
            foreach (var sourceItemGroup in sourceItemGroups)
            {
                destinationDocument.Add(new XElement(sourceItemGroup.Name, sourceItemGroup.Attributes(), sourceItemGroup.Elements()));
                //var destinationItemGroup = new XElement("ItemGroup");
                //foreach (var item in sourceItemGroup.Elements())
                //{
                //    var existing = destinationItemGroup.Elements().FirstOrDefault(e => e.Name == item.Name && e.Attribute("Include")?.Value == item.Attribute("Include")?.Value);
                //    if (existing != null)
                //    {
                //        XComment comment = new XComment(existing.ToString());
                //        existing.AddBeforeSelf(comment);
                //        existing.Remove();
                //    }
                //    destinationItemGroup.Add(new XElement(item.Name, item.Attributes(), item.Elements()));
                //}
                //destinationNode.Add(destinationItemGroup);
            }

            var remove = sourceNode.Attribute("Remove")?.Value;
            if (remove != null)
            {
                Dictionary<string, List<string>> removePath = new Dictionary<string, List<string>>();
                var vars = remove.Split(",");
                foreach (var mvar in vars)
                {
                    var kvp = mvar.Split('=');
                    if (kvp.Length > 1)
                    {
                        var values = kvp[1].Split('|');
                        if (!removePath.TryGetValue(kvp[0], out var list))
                        {
                            list = new List<string>();
                            removePath.Add(kvp[0], list);
                        }
                        list.AddRange(values);
                    }
                    else
                    {
                        var nodes = destinationDocument.XPathSelectElements(mvar).ToList();
                        foreach (var node in nodes)
                        {
                            XComment comment = new XComment(node.ToString());
                            node.AddBeforeSelf(comment);
                        }
                        nodes.Remove();
                    }
                }
                if (removePath.Count > 0)
                {
                    foreach (var r in removePath)
                    {
                        var key = r.Key.Split('@');
                        foreach (var value in r.Value)
                        {
                            var path = $"{key[0]}[@{key[1]}=\"{value}\"]";
                            var nodes = destinationDocument.XPathSelectElements(path).ToList();
                            foreach (var node in nodes)
                            {
                                XComment comment = new XComment(node.ToString());
                                node.AddBeforeSelf(comment);
                            }
                            nodes.Remove();
                        }
                    }
                }
            }

            const bool forceTargetBrowserOnly = true;
            bool isMultiTarget = false;
            bool hasExplicitBrowserTarget = false;
            var tfmMulti = destinationDocument.XPathSelectElement("//PropertyGroup/TargetFrameworks");
            if (tfmMulti != null)
            {
                isMultiTarget = tfmMulti.Value.Contains(";");
                hasExplicitBrowserTarget = tfmMulti.Value.Contains("-browser");
                if (forceTargetBrowserOnly)
                {
                    var tfmComment = new XComment(tfmMulti!.ToString());
                    tfmMulti.AddBeforeSelf(tfmComment);
                    tfmMulti.Remove();
                }
                else if (tfmMulti.Value.Contains("-browser"))
                {
                    tfmMulti.Value = "$(NetCoreAppCurrent)-browser;$(NetCoreAppCurrent)";
                }
                else
                {
                    tfmMulti.Value = "$(NetCoreAppCurrent)";
                }
            }

            if (forceTargetBrowserOnly)
            {
                var tfmSingle = destinationDocument.XPathSelectElement("//PropertyGroup/TargetFramework");
                if (tfmSingle != null)
                {
                    var tfmComment = new XComment(tfmSingle!.ToString());
                    tfmSingle.AddBeforeSelf(tfmComment);
                    tfmSingle.Remove();
                }
            }

            //if (!supportsBrowser)
            //{
            //    Console.WriteLine($"Warning: Project {projectName} does not support browser target.");
            //}
            //var tpi = doc.XPathSelectElement("//PropertyGroup/TargetPlatformIdentifier");
            //if (tpi != null)
            //{
            //    //tpi.Value="browser";
            //    var tfmComment = new XComment(tpi!.ToString());
            //    tpi.AddBeforeSelf(tfmComment);
            //    tpi.Remove();
            //}

            //There is an empty space in front of DefineConstants in System.Linq.Expressions , remove it
            var defineConstants = destinationDocument.XPathSelectElement("//PropertyGroup/DefineConstants");
            if (defineConstants != null)
            {
                defineConstants.Value = defineConstants.Value.Trim();
            }

            //Add EmitCompilerGeneratedFiles to all projects
            var emitCompilerGeneratedFiles = destinationDocument.XPathSelectElement("//PropertyGroup/EmitCompilerGeneratedFiles");
            if (emitCompilerGeneratedFiles == null)
            {
                var propertyGroup = destinationDocument.XPathSelectElement("//PropertyGroup");
                if (propertyGroup != null)
                {
                    var emitCompilerGeneratedFilesElement = new XElement("EmitCompilerGeneratedFiles");
                    emitCompilerGeneratedFilesElement.Value = "true";
                    propertyGroup.Add(emitCompilerGeneratedFilesElement);
                }
            }

            //Rename all InternalsVisibleTo Value
            var internalVisibles = destinationDocument.XPathSelectElements("//ItemGroup/InternalsVisibleTo");
            foreach (var internalVisible in internalVisibles)
            {
                var include = internalVisible.Attribute("Include");
                if (include != null)
                {
                    if (allProjects.Any(p => Path.GetFileNameWithoutExtension(p).Equals(include.Value, StringComparison.OrdinalIgnoreCase)))
                    {
                        include.Value = "NetJs." + include.Value;
                    }
                }
            }


            string[] includes = ["//ItemGroup/Compile", "//ItemGroup/ILLinkSubstitutionsXmls", "//ItemGroup/None", "//ItemGroup/Compile/DependentUpon", "//ItemGroup/AsnXml", "//ItemGroup/EmbeddedResource"];

            //Resolve Compile paths
            foreach (var includePath in includes)
            {
                destinationDocument.XPathSelectElements(includePath).FirstOrDefault(e =>
                {
                    var include = e.Attribute("Include");
                    if (include != null)
                    {
                        if (!include.Value.StartsWith("$(") && !include.Value.StartsWith("@("))
                        {
                            include.Value = $"{projectFolderPathAsVariable}\\{include.Value}";
                        }
                    }
                    var remove = e.Attribute("Remove");
                    if (remove != null)
                    {
                        if (!remove.Value.StartsWith("$(") && !remove.Value.StartsWith("@("))
                        {
                            remove.Value = $"{projectFolderPathAsVariable}\\{remove.Value}";
                        }
                    }
                    return false;
                });
            }

            //Resolve Import path
            destinationDocument.XPathSelectElements("//Import").FirstOrDefault(e =>
            {
                var project = e.Attribute("Project");
                if (project != null)
                {
                    if (!project.Value.StartsWith("$("))
                    {
                        project.Value = $"{projectFolderPathAsVariable}\\{project.Value}";
                    }
                }
                return false;
            });

            //Remove Reference to assembly
            destinationDocument.XPathSelectElements("//ItemGroup/Reference").ToList().FirstOrDefault(e =>
            {
                var comment = new XComment(e!.ToString());
                e.AddBeforeSelf(comment);
                e.Remove();
                return false;
            });

            //Remove package reference with Versions
            destinationDocument.XPathSelectElements("//ItemGroup/PackageReference[@Version]").ToList().FirstOrDefault(e =>
            {
                var comment = new XComment(e!.ToString());
                e.AddBeforeSelf(comment);
                e.Remove();
                return false;
            });

            //Resolve ProjectReference path
            destinationDocument.XPathSelectElements("//ItemGroup/ProjectReference").FirstOrDefault(e =>
            {
                if (e.Attribute("OutputItemType")?.Value == "Analyzer")
                {
                    //Skip analyzers
                    return false;
                }
                var include = e.Attribute("Include");
                if (include != null)
                {
                    if (include.Value.StartsWith("$(LibrariesProjectRoot)"))
                    {
                        var newPath = include.Value.Replace("$(LibrariesProjectRoot)", "$(NewLibrariesProjectRoot)");
                        var split = newPath.Split('\\');
                        //split[split.Length-1] = "NetJs." + split[split.Length - 1];
                        //include.Value = string.Join("\\", split);
                        include.Value = $"$(NewLibrariesProjectRoot){Path.GetFileNameWithoutExtension(split[split.Length - 1])}\\NetJs.{split[split.Length - 1]}";
                    }
                    else if (include.Value.StartsWith("../") || include.Value.StartsWith("..\\") || include.Value.StartsWith("/..") || include.Value.StartsWith("\\.."))
                    {
                        var fullPath = Path.GetFullPath(Path.Join(projectFolderPath, include.Value));
                        var relative = Path.GetRelativePath(_dotnetRuntimeRoot, fullPath);
                        var newPath = $"$(DotnetRuntimeRoot){relative}";
                        include.Value = newPath;
                    }
                }
                return false;
            });

            //Add Compile for all .cs files if EnableDefaultItems is true
            var enableDefaultItems = destinationDocument.XPathSelectElement("//PropertyGroup/EnableDefaultItems")?.Value == "true";
            if (enableDefaultItems)
            {
                var files = Directory.GetFiles(projectFolderPath, "*.cs", SearchOption.AllDirectories).Concat(Directory.GetFiles(projectFolderPath, "*.razor", SearchOption.AllDirectories));
                var existingCompiles = destinationDocument.XPathSelectElements("//ItemGroup/Compile")
                    .Select(e => e.Attribute("Include")?.Value.Replace($"{projectFolderPathAsVariable}\\", "").Replace("/", "\\"))
                    .Where(v => v != null)
                    .ToHashSet();
                var itemGroup = new XElement("ItemGroup");
                foreach (var file in files)
                {
                    if (Path.GetFileName(file) == "Strings.Designer.cs")
                        continue;
                    if (file.StartsWith(_dotnetRuntimeRoot))
                    {
                        var relativePath = file.Replace(_dotnetRuntimeRoot, "").Replace("/", "\\");
                        if (!existingCompiles.Contains(relativePath))
                        {
                            var compile = new XElement("Compile");
                            compile.Add(new XAttribute("Include", $"$(DotnetRuntimeRoot){relativePath}"));
                            itemGroup!.Add(compile);
                        }
                    }
                    else if (file.StartsWith(_repoRoot))
                    {
                        var relativePath = file.Replace(_repoRoot, "").Replace("/", "\\");
                        if (!existingCompiles.Contains(relativePath))
                        {
                            var compile = new XElement("Compile");
                            compile.Add(new XAttribute("Include", $"$(RepoRoot){relativePath}"));
                            itemGroup!.Add(compile);
                        }
                    }
                }
                destinationDocument.Add(itemGroup);
            }

            //If browser is not explicitly targeted, replace <ItemGroup Condition="'$(TargetPlatformIdentifier)' == ''"> with <ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'browser'">
            //The item under this condition would have being used in default build
            if (!hasExplicitBrowserTarget)
            {
                var itemGroups = destinationDocument.XPathSelectElements("//*[@Condition]").ToList();
                foreach (var itemGroup in itemGroups)
                {
                    var conditionAttribute = itemGroup.Attribute("Condition");
                    var conditionValue = conditionAttribute?.Value;
                    if (conditionAttribute != null && conditionValue != null && conditionValue.Contains("'$(TargetPlatformIdentifier)' == ''"))
                    {
                        conditionAttribute.Remove();
                        conditionValue = conditionValue.Replace("''", "'browser'");
                        itemGroup.SetAttributeValue("Condition", conditionValue);
                    }
                }
            }

            //If broswer is not supported, use ref project and doctor the files to throw PlatformNotSupportedException
            var unsupportedPlatform = destinationDocument.XPathSelectElement("//PropertyGroup/UnsupportedOSPlatforms")?.Value;
            var supportedPlatform = destinationDocument.XPathSelectElement("//PropertyGroup/SupportedOSPlatforms")?.Value;
            bool supportsBrowser = true;
            if (unsupportedPlatform?.Contains("browser") ?? false)
                supportsBrowser = false;
            else if (supportedPlatform != null && !supportedPlatform.Contains("bowser"))
                supportsBrowser = false;
            bool isPartialFacade = destinationDocument.XPathSelectElement("//PropertyGroup/IsPartialFacadeAssembly")?.Value == "true";
            if (!supportsBrowser && !isPartialFacade)
            {
                //Not supported on browser, Use the ref project instead
                var refProjectPath = Path.GetFullPath(Path.Join(projectFolderPath, "..", "ref"));
                var csFiles = Directory.GetFiles(refProjectPath, "*.cs", SearchOption.AllDirectories);
                //remove every other files
                var compiles = destinationDocument.XPathSelectElements("//ItemGroup/Compile").ToList();
                foreach (var compile in compiles)
                {
                    XComment comment = new XComment(compile.ToString());
                    compile.AddBeforeSelf(comment);
                    compile.Remove();
                }
                if (true)
                {
                    foreach (var csFile in csFiles)
                    {
                        var content = File.ReadAllText(csFile);
                        content = content.Replace("throw null", "throw new System.PlatformNotSupportedException()").Replace("set { }", "set { throw new System.PlatformNotSupportedException(); }");
                        File.WriteAllText(Path.Join(newProjectDirectory, Path.GetFileName(csFile)), content);
                    }
                }
                else
                {
                    var itemGroup = new XElement("ItemGroup");
                    foreach (var csFile in csFiles)
                    {
                        var compile = new XElement("Compile");
                        compile.Add(new XAttribute("Include", $"$(DotnetRuntimeRoot)src\\libraries\\{projectName}\\ref\\{Path.GetFileName(csFile)}"));
                        itemGroup!.Add(compile);
                    }
                    destinationDocument.Add(itemGroup);
                }
            }
            else
            {
                //GenerateStaticResource(destinationDocument, projectName, projectFolderPath, newProjectDirectory);
                GenerateStaticResourceInlined(destinationDocument, projectName, projectFolderPath, newProjectDirectory);
            }
            var doctored = destinationDocument.ToString();
            var outPath = Path.Join(newProjectDirectory, $"NetJs.{projectFileName}");
            try
            {
                File.WriteAllText(outPath, $"\r\n<!--Generated by NetJs doctor from {csProj}-->\r\n\r\n" + destinationDocument.ToString());
                if (isNewProject)
                {
                    //Make sure the project is added to solution
                    await $"cd {newProjectDirectory} & dotnet sln ../../NetJs.sln add NetJs.{projectFileName} --solution-folder libraries".CLI();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return $"$(NewLibrariesProjectRoot){projectName}\\NetJs.{projectFileName}";
        }
    }
}
