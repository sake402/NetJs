using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetJs.Translator
{
    public interface IProject
    {
        CSharpCompilation? Compilation { get; }
        CompilationOptions? CompilationOptions { get; }
        string DirectoryPath { get; }
        string FullPath { get; }
        string? Evaluate(string propertyName);
        string GetAssemblyName();
        string GetNamespace();
        string GetOutputPath();
        OutputMode GetOutputMode();
        IList<string> GetSourceFiles();
        IList<string> GetContentFiles();
        IList<string> GetLinkerFiles();
        IList<string> GetEmbeddedFiles();
        IList<string> GetGlobalUsings();
        bool Build();
    }

    public interface IProjectOutputProvider
    {
        string OutputPath { get; }
        Stream HtmlScriptContent { get; }
        Stream HtmlStyleContent { get; }
        Stream HtmlBodyContent { get; }
        IEnumerable<string> OutputtedFiles { get; }
        void Output(GlobalCompilationVisitor global, string destinationRelativePath, OneOf<string, Stream> content);
    }
}
