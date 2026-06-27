using LivingThing.Core.Frameworks.Common.OneOf;
using NetJs.Translator;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetJs.Compiler
{
    public class ProjectBinOutputProvider : IProjectOutputProvider
    {
        //const string GeneratedFolderName = "__dotnetJs";
        IProject project;
        public string OutputPath => Path.Combine(project.DirectoryPath, project.GetOutputPath()/*, GeneratedFolderName*/);
        public Stream HtmlScriptContent => htmlScriptContent;
        public Stream HtmlStyleContent => htmlStyleContent;
        public Stream HtmlBodyContent => htmlBodyContent;
        public IEnumerable<string> OutputtedFiles => outputtedFiles;

        List<string> outputtedFiles = new();
        MemoryStream htmlScriptContent = new MemoryStream();
        MemoryStream htmlStyleContent = new MemoryStream();
        MemoryStream htmlBodyContent = new MemoryStream();
        bool cleaned;
        public ProjectBinOutputProvider(IProject project)
        {
            this.project = project;
        }

        public void Output(GlobalCompilationVisitor global, string destinationRelativePath, OneOf<string, Stream> content)
        {
            if (!cleaned)
            {
                var files = Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if (f.EndsWith(".js.dll") || f.EndsWith(".js.pdb") || f.EndsWith(".js.xml")) //clean only the ones we created, to avoid deleting files created by other tools (e.g. .NET build)
                        File.Delete(f);
                }
                var directories = Directory.GetDirectories(OutputPath);
                foreach (var d in directories)
                {
                    try
                    {
                        Directory.Delete(d, true);
                    }
                    catch
                    {
                        var mfiles = Directory.GetFiles(d, "*.*", SearchOption.AllDirectories);
                        foreach (var f in mfiles)
                        {
                            File.Delete(f);
                        }
                        var mdirectories = Directory.GetDirectories(d);
                        foreach (var dd in mdirectories)
                        {
                            Directory.Delete(dd, true);
                        }
                    }
                }
                cleaned = true;
            }
            var outputFile = Path.Combine(OutputPath, /*Constants.OutputFolderName,*/ destinationRelativePath);
            FileInfo? existingInfo = null;
            DateTime? sourceCreateTime = null;
            if (content.IsT0 && File.Exists(outputFile))
            {
                var fileInfo = new FileInfo(content.AsT0);
                sourceCreateTime = fileInfo.LastWriteTime;
                existingInfo = new FileInfo(outputFile);
                if (fileInfo.LastWriteTime < existingInfo.LastWriteTime)
                    return;
            }
            Stream stream;
            if (content.IsT0)
            {
                stream = new FileStream(content.AsT0, FileMode.Open, FileAccess.Read);
            }
            else
            {
                stream = content.AsT1;
            }
            if (destinationRelativePath.EndsWith(".dll") || destinationRelativePath.EndsWith(".pdb") || destinationRelativePath.EndsWith(".xml"))
            {
                var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                stream.CopyTo(output);
                output.Flush();
                output.Close();
            }
            else if (!global.OutputMode.HasFlag(OutputMode.SingleHtmlFile) || destinationRelativePath.EndsWith(".html"))
            {
                var dir = Path.GetDirectoryName(outputFile);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                stream.CopyTo(output);
                output.Flush();
                output.Close();
                if (existingInfo != null && sourceCreateTime != null)
                    existingInfo.LastWriteTime = sourceCreateTime.Value;
                //File.WriteAllText(outputFile, content);
            }
            else
            {
                if (destinationRelativePath.EndsWith(".js"))
                    stream.CopyTo(htmlScriptContent);
                else if (destinationRelativePath.EndsWith(".css"))
                    stream.CopyTo(htmlStyleContent);
                else
                    stream.CopyTo(htmlBodyContent);
            }

            if (!outputtedFiles.Contains(destinationRelativePath))
                outputtedFiles.Add(destinationRelativePath);
        }
    }
}
