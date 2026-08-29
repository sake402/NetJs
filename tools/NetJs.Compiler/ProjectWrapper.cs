using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using NetJs.Translator;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeAnalysisProject = Microsoft.CodeAnalysis.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NetJs.Compiler
{
    public class ProjectWrapper : IProject
    {
        MSBuildWorkspace msWorkspace;
        ProjectCollection projectCollection;
        MsBuildProject msProject;
        CodeAnalysisProject caProject;
        IDictionary<string, string>? buildProperties;
        public CSharpCompilation? Compilation { get; }
        public string DirectoryPath => msProject.DirectoryPath;
        public string FullPath => msProject.FullPath;
        public string SDK => msProject.Xml.Sdk;
        public string BaseIntermediateOutputPath => Evaluate("BaseIntermediateOutputPath").First().Replace("\\", "/");
        public string IntermediateOutputPath => Evaluate("IntermediateOutputPath").First().Replace("\\", "/");
        public CompilationOptions? CompilationOptions => caProject.CompilationOptions;
        public ProjectWrapper(MSBuildWorkspace msWorkspace, ProjectCollection projectCollection, CodeAnalysisProject caProject, MsBuildProject project, IDictionary<string, string>? buildProperties)
        {
            this.msWorkspace = msWorkspace;
            this.projectCollection = projectCollection;
            this.caProject = caProject;
            this.msProject = project;
            this.buildProperties = buildProperties;
        }

        public IEnumerable<string> Evaluate(string propertyName, bool allItems = false)
        {
            var v = msProject.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(v))
                return [v];
            if (allItems)
            {
                var value = msProject.AllEvaluatedItems.Where(e => e.ItemType == propertyName);
                return value.Select(e => e.EvaluatedInclude);
            }
            else
            {
                var value = msProject.AllEvaluatedProperties.Where(e => e.Name == propertyName);
                return value.Select(e => e.EvaluatedValue);
            }
        }

        public string GetAssemblyName()
        {
            var aName = msProject.AllEvaluatedProperties.Last(e => e.Name == "AssemblyName").EvaluatedValue;
            //We may have override assembly name back to default for some libraries, beacuase of vs intelissense
            if (!aName.StartsWith(Constants.ProjectName))
            {
                aName = Constants.ProjectName + "." + aName;
            }
            return aName;
        }
        public string GetNamespace()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "RootNamespace").EvaluatedValue;
        }
        public string GetOutputPath()
        {
            var result = msProject.AllEvaluatedProperties.Last(e => e.Name == "OutputPath").EvaluatedValue;
            if (!Path.IsPathRooted(result))
                result = Path.Combine(msProject.DirectoryPath, result);
            return result;
        }
        public string GetConfiguration()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "Configuration").EvaluatedValue;
        }
        public string GetPlatform()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "Platform").EvaluatedValue;
        }
        public string GetTargetFramework()
        {
            var framework = msProject.AllEvaluatedProperties.LastOrDefault(e => e.Name == "TargetFramework")?.EvaluatedValue;
            if (framework == null)
            {
                framework = msProject.AllEvaluatedProperties.LastOrDefault(e => e.Name == "TargetFrameworks")?.EvaluatedValue?.Split(';').LastOrDefault();
            }
            if (framework == null)
            {
                framework = "net10.0";
            }
            return framework;
        }
        public NetJsBuildFlags GetBuildFlags()
        {
            var v = msProject.AllEvaluatedProperties.LastOrDefault(e => e.Name == nameof(NetJsBuildFlags))?.EvaluatedValue;
            Enum.TryParse<NetJsBuildFlags>(v, out var value);
            if (value == NetJsBuildFlags.None)
            {
                value = NetJsBuildFlags.Default;
            }
            if (value.HasFlag(NetJsBuildFlags.Module) && value.HasFlag(NetJsBuildFlags.Global))
            {
                throw new InvalidOperationException("Cannot enable both global and module at the same time");
            }
            //if (!value.HasFlag(NetJsBuildFlags.Module) && !value.HasFlag(NetJsBuildFlags.Global))
            //{
            value |= NetJsBuildFlags.Global;//Module not yet supported, we must use global for now
            //}
            return value;
        }

        public IList<string> GetGlobalUsings()
        {
            IList<string> sourceFiles = new List<string>();

            //Already part of source files
            //foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "Using"))
            //{
            //    sourceFiles.Add(projectItem.EvaluatedInclude);
            //}

            return sourceFiles;
        }

        public IList<string> GetSourceFiles()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "Compile"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                if (Path.IsPathRooted(projectItem.EvaluatedInclude)) //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }

            //var platform = GetPlatform();
            //if (platform.Equals("AnyCPU", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    platform = "";
            //}
            //else
            //{
            //    platform = "/" + platform;
            //}
            //Check for cs files like .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
            var objectFolder = IntermediateOutputPath;
            if (!Path.IsPathRooted(objectFolder)) //check if it has volume label already
                objectFolder = Path.Join(msProject.DirectoryPath, objectFolder);
            if (Directory.Exists(objectFolder))
            {
                var csFiles = Directory.EnumerateFiles(objectFolder, "*.cs", SearchOption.TopDirectoryOnly);
                sourceFiles.AddRange(csFiles);
            }

            //Check for source generated files
            var sourceGenOutputPath = Path.Combine(objectFolder, "generated");
            if (Directory.Exists(sourceGenOutputPath))
            {
                var csFiles = Directory.EnumerateFiles(sourceGenOutputPath, "*.cs", SearchOption.AllDirectories);
                sourceFiles.AddRange(csFiles);
            }
            return sourceFiles;
        }

        public IList<string> GetContentFiles()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "Content"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                if (Path.IsPathRooted(projectItem.EvaluatedInclude)) //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }
            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "None"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                var extension = Path.GetExtension(projectItem.EvaluatedInclude);
                if (extension == ".js" || extension == ".css" || extension == ".html")
                {
                    if (Path.IsPathRooted(projectItem.EvaluatedInclude))  //check if it has volume label already
                        sourceFiles.Add(projectItem.EvaluatedInclude);
                    else
                        sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
                }
            }

            //var platform = GetPlatform();
            //if (platform.Equals("AnyCPU", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    platform = "";
            //}
            //else
            //{
            //    platform = "/" + platform;
            //}
            var objectFolder = IntermediateOutputPath;
            if (!Path.IsPathRooted(objectFolder)) //check if it has volume label already
                objectFolder = Path.Join(msProject.DirectoryPath, objectFolder);
            //Check for css files scopedcss/bundle
            var sourceGenOutputPath = Path.Combine(objectFolder, "scopedcss", "bundle");// $"{msProject.DirectoryPath}/obj{platform}/{GetConfiguration()}/{GetTargetFramework()}/scopedcss/bundle";
            if (Directory.Exists(sourceGenOutputPath))
            {
                var csFiles = Directory.EnumerateFiles(sourceGenOutputPath, "*.css", SearchOption.AllDirectories);
                sourceFiles.AddRange(csFiles);
            }

            return sourceFiles;
        }

        public IList<string> GetLinkerFiles()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "ILLinkSubstitutionsXmls"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                if (Path.IsPathRooted(projectItem.EvaluatedInclude))  //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }

            return sourceFiles;
        }

        public IList<string> GetEmbeddedFiles()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "EmbeddedResource"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                if (Path.IsPathRooted(projectItem.EvaluatedInclude))  //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }

            return sourceFiles;
        }

        public async Task<IProject> LoadDependecy(string projectName)
        {
            var csProjectFile = projectName;
            var codeAnalysisProject = msWorkspace.CurrentSolution.Projects.FirstOrDefault(f => f.Name.Equals(projectName, StringComparison.InvariantCultureIgnoreCase));
            if (codeAnalysisProject == null && !projectName.StartsWith(Constants.ProjectName + "."))
            {
                projectName = Constants.ProjectName + "." + projectName;
                codeAnalysisProject = msWorkspace.CurrentSolution.Projects.First(f => f.Name.Equals(projectName, StringComparison.InvariantCultureIgnoreCase));
            }
            var msBuildProject = projectCollection.LoadedProjects.SingleOrDefault(e => e.FullPath == codeAnalysisProject!.FilePath) ??
                new MsBuildProject(codeAnalysisProject!.FilePath, buildProperties, null, projectCollection);
            return new ProjectWrapper(msWorkspace, projectCollection, codeAnalysisProject!, msBuildProject, buildProperties);
        }

        public bool Build()
        {
            return msProject.Build();
        }
    }
}
