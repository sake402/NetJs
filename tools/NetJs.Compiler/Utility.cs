using NetJs.Translator;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project = Microsoft.Build.Evaluation.Project;

namespace NetJs.Compiler
{
    public static class Utility
    {
        public static string? Evaluate(this Project project, string propertyName)
        {
            var value = project.AllEvaluatedProperties.LastOrDefault(e => e.Name == propertyName);
            return value?.EvaluatedValue;
        }
        public static string GetAssemblyName(this Project project)
        {
            return project.AllEvaluatedProperties.Last(e => e.Name == "AssemblyName").EvaluatedValue;
        }
        public static string GetNamespace(this Project project)
        {
            return project.AllEvaluatedProperties.Last(e => e.Name == "RootNamespace").EvaluatedValue;
        }
        public static string GetFolder(this Project project)
        {
            return System.IO.Path.GetDirectoryName(project.FullPath)!;
        }
        public static string GetFolderName(this Project project)
        {
            return System.IO.Path.GetDirectoryName(project.FullPath)!.Split('/', '\\').Last();
        }
        public static string GetName(this Project project)
        {
            return System.IO.Path.GetFileNameWithoutExtension(project.FullPath);
        }
        public static string GetOutputPath(this Project project)
        {
            return project.AllEvaluatedProperties.Last(e => e.Name == "OutputPath").EvaluatedValue;
        }
        public static NetJsBuildFlags GetOutputMode(this Project project)
        {
            var v = project.AllEvaluatedProperties.LastOrDefault(e => e.Name == "OutputMode")?.EvaluatedValue;
            Enum.TryParse<NetJsBuildFlags>(v, out var value);
            if (value.HasFlag(NetJsBuildFlags.Module) && value.HasFlag(NetJsBuildFlags.Global))
            {
                throw new InvalidOperationException("Cannot enable both global and module at the same time");
            }
            if (!value.HasFlag(NetJsBuildFlags.Module) && !value.HasFlag(NetJsBuildFlags.Global))
            {
                value |= NetJsBuildFlags.Module;
            }
            return value;
        }
    }
}
