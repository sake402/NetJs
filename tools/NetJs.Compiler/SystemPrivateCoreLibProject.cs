using NetJs.Translator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetJs.Compiler
{
    internal static class SystemPrivateCoreLibProject
    {
        internal static void Generate(string dotnetJsSolutionPath)
        {
            var directoryBuildProps = Path.Combine(dotnetJsSolutionPath, "libraries", "Directory.Build.props");
            var fileContent = File.ReadAllText(directoryBuildProps);
            var netJsFolder = Regex.Match(fileContent, ".?<NetJsFolder>(.+)</NetJsFolder>.?").Groups[1].Value;
            var DotnetGitRoot = Regex.Match(fileContent, ".?<DotnetGitRoot>(.+)</DotnetGitRoot>.?").Groups[1].Value.Replace("$(NetJsFolder)", netJsFolder);
            var DotnetRuntimeRoot = Regex.Match(fileContent, ".?<DotnetRuntimeRoot>(.+)</DotnetRuntimeRoot>.?").Groups[1].Value.Replace("$(DotnetGitRoot)", DotnetGitRoot);
            var CoreLibRoot = Regex.Match(fileContent, ".?<CoreLibRoot>(.+)</CoreLibRoot>.?").Groups[1].Value.Replace("$(DotnetRuntimeRoot)", DotnetRuntimeRoot);
            var CoreLibSharedDir = Regex.Match(fileContent, ".?<CoreLibSharedDir>(.+)</CoreLibSharedDir>.?").Groups[1].Value.Replace("$(DotnetRuntimeRoot)", DotnetRuntimeRoot);
            var RepoRoot = Regex.Match(fileContent, ".?<RepoRoot>(.+)</RepoRoot>.?").Groups[1].Value;
            var CommonPath = Regex.Match(fileContent, ".?<CommonPath>(.+)</CommonPath>.?").Groups[1].Value;
            var SharedSourceRoot = Regex.Match(fileContent, ".?<SharedSourceRoot>(.+)</SharedSourceRoot>.?").Groups[1].Value;
            var BclSourcesRoot = Regex.Match(fileContent, ".?<BclSourcesRoot>(.+)</BclSourcesRoot>.?").Groups[1].Value;
            var LibrariesProjectRoot = Regex.Match(fileContent, ".?<LibrariesProjectRoot>(.+)</LibrariesProjectRoot>.?").Groups[1].Value.Replace("$(DotnetRuntimeRoot)", DotnetRuntimeRoot);
            var PrivateCoreLibSharedProjectDirectory = Regex.Match(fileContent, ".?<PrivateCoreLibSharedProjectDirectory>(.+)</PrivateCoreLibSharedProjectDirectory>.?").Groups[1].Value;

            var MsBuildThisProjectFile = $"$(DotnetRuntimeRoot)src/mono/System.Private.CoreLib/System.Private.CoreLib.csproj";
            var MsBuildThisFileDirectory = $"$(DotnetRuntimeRoot)src/mono/System.Private.CoreLib/";

            var monoCsProject = File.ReadAllText(MsBuildThisProjectFile.Replace("$(DotnetRuntimeRoot)", DotnetRuntimeRoot))
                .Comment("<EnableDefaultItems>false</EnableDefaultItems>")
                .Comment("<OutputPath>$(RuntimeBinDir)IL/</OutputPath>")
                .Comment("<Platforms>x64;x86;arm;arm64</Platforms>")
                //.Comment("<DefineConstants>$(DefineConstants);MONO_FEATURE_SRE</DefineConstants>")
                .Comment(@"<PropertyGroup>
    <CommonPath>$([MSBuild]::NormalizeDirectory('$(LibrariesProjectRoot)', 'Common', 'src'))</CommonPath>
    <BclSourcesRoot>$(MSBuildThisFileDirectory)src</BclSourcesRoot>
  </PropertyGroup>")
                .Comment(@"<Target Name=""CopyCoreLibToBinDir"" AfterTargets=""Build"">
    <Copy SourceFiles=""$(RuntimeBinDir)/IL/System.Private.CoreLib.dll;$(RuntimeBinDir)/IL/System.Private.CoreLib.pdb""
          DestinationFolder=""$(RuntimeBinDir)""
          SkipUnchangedFiles=""true"" />
  </Target>")
                //              .Comment(@"<ItemGroup Condition=""'$(FeaturePerfTracing)' == 'true'"">
                //  <AdditionalFiles Include=""$(CoreClrProjectRoot)vm/ClrEtwAll.man"" />
                //</ItemGroup>")
                .Replace("<NoWarn>$(NoWarn),0419,0649</NoWarn>", "<NoWarn>$(NoWarn),0419,0649,1591</NoWarn>")
                .Replace("$(MSBuildThisFileDirectory)", MsBuildThisFileDirectory)
                .Replace("<ILLinkSubstitutionsXmls Include=\"$(ILLinkDirectory)ILLink.Substitutions.Intrinsics.Vectors.xml\" />",
                @"<ILLinkSubstitutionsXmls Include=""$(ILLinkDirectory)ILLink.Substitutions.Intrinsics.Vectors.xml"" />

    <ILLinkSubstitutionsXmls Include=""$(MsBuildThisProjectDirectory)Browser/ILLink.Substitutions.Intrinsics.Vectors.xml"" />")
                ;

            var import = Regex.Match(monoCsProject, ".?<Import\\s+Project=\"([^\"]+)\".+\\/>.?").Groups[1].Value;

            var imported = File.ReadAllText(import.Replace("$(LibrariesProjectRoot)", LibrariesProjectRoot))
                .Comment("<Project>")
                .Comment("</Project>")
                .Replace("$(MSBuildThisFileDirectory)", $"$(PrivateCoreLibSharedProjectDirectory)");

            monoCsProject = monoCsProject
                .Comment("<Import Project=\"$(LibrariesProjectRoot)\\System.Private.CoreLib\\src\\System.Private.CoreLib.Shared.projitems\" Label=\"Shared\" />")
                .InsertAfter("<!--<Import Project=\"$(LibrariesProjectRoot)\\System.Private.CoreLib\\src\\System.Private.CoreLib.Shared.projitems\" Label=\"Shared\" />-->", "\r\n" + imported)
                .Replace("<FeaturePerfTracing Condition=\"('$(TargetsWasi)' != 'true')\">true</FeaturePerfTracing>", "<FeaturePerfTracing Condition=\"('$(TargetsWasi)' != 'true')\">false</FeaturePerfTracing>");

            var csProjPath = $"{dotnetJsSolutionPath}/libraries/System.Private.CoreLib/NetJs.System.Private.CoreLib.csproj";
            File.WriteAllText(csProjPath, monoCsProject);

            ResXGenerator.GenerateStaticResource(null, "System.Private.CoreLib", PrivateCoreLibSharedProjectDirectory.Replace("$(DotnetRuntimeRoot)", DotnetRuntimeRoot), Path.GetDirectoryName(csProjPath)!, "SR1");
        }
    }
}
