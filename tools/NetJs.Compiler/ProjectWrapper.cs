using NetJs.Translator;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MsBuildProject = Microsoft.Build.Evaluation.Project;
using CodeAnalysisProject = Microsoft.CodeAnalysis.Project;
using NuGet.Packaging;
using Microsoft.CodeAnalysis;

namespace NetJs.Compiler
{
    public class ProjectWrapper : IProject
    {
        MsBuildProject msProject;
        CodeAnalysisProject caProject;
        public CSharpCompilation? Compilation { get; }
        public string DirectoryPath => msProject.DirectoryPath;
        public string FullPath => msProject.FullPath;
        public CompilationOptions? CompilationOptions => caProject.CompilationOptions;
        public ProjectWrapper(CodeAnalysisProject caProject, MsBuildProject project)
        {
            this.caProject = caProject;
            this.msProject = project;

        }

        public string? Evaluate(string propertyName)
        {
            var v = msProject.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(v))
                return v;
            var value = msProject.AllEvaluatedProperties.LastOrDefault(e => e.Name == propertyName);
            return value?.EvaluatedValue;
        }
        public string GetAssemblyName()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "AssemblyName").EvaluatedValue;
        }
        public string GetNamespace()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "RootNamespace").EvaluatedValue;
        }
        public string GetOutputPath()
        {
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "OutputPath").EvaluatedValue;
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
            return msProject.AllEvaluatedProperties.Last(e => e.Name == "TargetFramework").EvaluatedValue;
        }
        public OutputMode GetOutputMode()
        {
            var v = msProject.AllEvaluatedProperties.LastOrDefault(e => e.Name == "OutputMode")?.EvaluatedValue;
            Enum.TryParse<OutputMode>(v, out var value);
            if (value == OutputMode.None)
            {
                value = OutputMode.Global | OutputMode.InlineConstants | OutputMode.SingleFile;
            }
            if (value.HasFlag(OutputMode.Module) && value.HasFlag(OutputMode.Global))
            {
                throw new InvalidOperationException("Cannot enable both global and module at the same time");
            }
            if (!value.HasFlag(OutputMode.Module) && !value.HasFlag(OutputMode.Global))
            {
                value |= OutputMode.Global;
            }
            return value;
        }

        public IList<string> GetGlobalUsings()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "Using"))
            {
                sourceFiles.Add(projectItem.EvaluatedInclude);
            }

            return sourceFiles;
        }

        public IList<string> GetSourceFiles()
        {
            IList<string> sourceFiles = new List<string>();

            foreach (var projectItem in msProject.AllEvaluatedItems.Where(i => i.ItemType == "Compile"))
            {
                if (projectItem.EvaluatedInclude.Contains(".NETCoreApp,"))
                    continue;
                if (projectItem.EvaluatedInclude.Contains(':')) //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }

            //Check for source generated files
            var sourceGenOutputPath = $"{msProject.DirectoryPath}/obj/{GetPlatform()}/{GetConfiguration()}/{GetTargetFramework()}/generated";
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
                if (projectItem.EvaluatedInclude.Contains(':')) //check if it has volume label already
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
                    if (projectItem.EvaluatedInclude.Contains(':')) //check if it has volume label already
                        sourceFiles.Add(projectItem.EvaluatedInclude);
                    else
                        sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
                }
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
                if (projectItem.EvaluatedInclude.Contains(':')) //check if it has volume label already
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
                if (projectItem.EvaluatedInclude.Contains(':')) //check if it has volume label already
                    sourceFiles.Add(projectItem.EvaluatedInclude);
                else
                    sourceFiles.Add(Path.Join(msProject.DirectoryPath, projectItem.EvaluatedInclude));
            }

            return sourceFiles;
        }

        public bool Build()
        {
            return msProject.Build();
        }
    }
}
