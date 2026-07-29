using LivingThing.Core.Frameworks.Common.OneOf;
using NetJs.Translator;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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

        public async Task Output(GlobalCompilationVisitor global, string destinationRelativePath, OneOf<string, Stream> content)
        {
            bool debugLog = false;
            List<Task> pendingTasks = new();
            if (!cleaned)
            {
                var files = Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if (f.EndsWith(".SymbolNames.yaml") ||
                        f.EndsWith(".js") ||
                        f.EndsWith(".js.dll") ||
                        f.EndsWith(".js.pdb") ||
                        f.EndsWith(".js.xml")) //clean only the ones we created, to avoid deleting files created by other tools (e.g. .NET build)
                    {
                        if (debugLog)
                            Console.WriteLine($"Clean \"{f}\"");
                        File.Delete(f);
                    }
                }
                var directories = Directory.GetDirectories(OutputPath);
                foreach (var d in directories)
                {
                    try
                    {
                        if (debugLog)
                            Console.WriteLine($"Clean \"{d}\"!");
                        Directory.Delete(d, true);
                    }
                    catch
                    {
                        var mfiles = Directory.GetFiles(d, "*.*", SearchOption.AllDirectories);
                        foreach (var f in mfiles)
                        {
                            if (debugLog)
                                Console.WriteLine($"Clean \"{f}\"!");
                            File.Delete(f);
                        }
                        var mdirectories = Directory.GetDirectories(d);
                        foreach (var dd in mdirectories)
                        {
                            if (debugLog)
                                Console.WriteLine($"Clean \"{dd}\"!");
                            Directory.Delete(dd, true);
                        }
                    }
                }
                cleaned = true;
            }
            var outputFile = Path.Combine(OutputPath, /*Constants.OutputFolderName,*/ destinationRelativePath);
            if (content.IsT0 && content.AsT0 == outputFile)
                return;
            FileInfo? existingInfo = null;
            DateTime? sourceCreateTime = null;
            if (content.IsT0 && File.Exists(outputFile))
            {
                var fileInfo = new FileInfo(content.AsT0);
                sourceCreateTime = fileInfo.LastWriteTime;
                existingInfo = new FileInfo(outputFile);
                if (existingInfo.LastWriteTime > fileInfo.LastWriteTime)
                {
                    if (debugLog)
                        Console.WriteLine($"Skip copy to \"{outputFile}\" as {existingInfo.LastWriteTime} > {fileInfo.LastWriteTime}!");
                    return;
                }
            }
            Stream stream;
            bool ownStream = false;
            if (content.IsT0)
            {
                stream = new FileStream(content.AsT0, FileMode.Open, FileAccess.Read);
                ownStream = true;
                if (debugLog)
                    Console.WriteLine($"Open stream from \"{content.AsT0}\"!");
            }
            else
            {
                stream = content.AsT1;
            }
            if (destinationRelativePath.EndsWith(".dll") || destinationRelativePath.EndsWith(".pdb") || destinationRelativePath.EndsWith(".xml"))
            {
                var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(output);
                await output.FlushAsync();
                output.Close();
                if (debugLog)
                    Console.WriteLine($"Copy stream to \"{outputFile}\"!");
            }
            else if (!global.OutputMode.HasFlag(OutputMode.SingleHtmlFile) || destinationRelativePath.EndsWith(".html"))
            {
                var dir = Path.GetDirectoryName(outputFile);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(output);
                await output.FlushAsync();
                output.Close();
                if (debugLog)
                    Console.WriteLine($"Copy stream to \"{outputFile}\"!");
                if (existingInfo != null && sourceCreateTime != null)
                {
                    existingInfo.LastWriteTime = sourceCreateTime.Value;
                }
                //File.WriteAllText(outputFile, content);
            }
            else
            {
                if (destinationRelativePath.EndsWith(".js"))
                {
                    if (debugLog)
                        await stream.CopyToAsync(htmlScriptContent);
                    Console.WriteLine($"Copy stream to js stream!");
                }
                else if (destinationRelativePath.EndsWith(".css"))
                {
                    if (debugLog)
                        await stream.CopyToAsync(htmlStyleContent);
                    Console.WriteLine($"Copy stream to css stream!");
                }
                else
                {
                    if (debugLog)
                        await stream.CopyToAsync(htmlBodyContent);
                    Console.WriteLine($"Copy stream to html stream!");
                }
            }

            if (!outputtedFiles.Contains(destinationRelativePath))
                outputtedFiles.Add(destinationRelativePath);

            if (ownStream)
            {
                stream.Close();
                stream.Dispose();
            }
        }
    }
}
