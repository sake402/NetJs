Build C# into native javascript with official dotnet runtime and aspnetcore support.

USAGE
---------
Enter the directory where the project is and execute _dotnet build_ first to make sure project builds.
Then execute _NetJs build_.

Supports and tested with Sdks _Microsoft.NET.Sdk.BlazorWebAssembly_, _Microsoft.NET.Sdk.Razor_, _Microsoft.NET.Sdk_

NOTES
---------
If building a razor project or any project with source generators, you must have _<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>_ in your PropertyGroup or execute _dotnet build -p:EmitCompilerGeneratedFiles=true_ first.

This ensures that the csharp code files are generated into the obj directory first. They are picked up by NetJs for traspilation.

CODE SIZE AND MINIFICATION
---------
By default, identifiers for namespace, class and members are maintained as is. 

Given
```
namespace Long.Namespace
{
	static class LongClassName
	{
		static void VeryVeryLongMemberName()
		{

		}
	}
}
```

Accessing ``VeryVeryLongMemberName`` will will emit ``$assembly.Long.Namespace.LongClassName.VeryVeryLongMemberName``. This can make the js file heavy.

You can enable name minification using _<NetJsBuildFlags>Global,InlineConstants,SingleFile,MinifyNamespaces,MinifyTypeNames,MinifyMemberNames,ShortNamesCreateFromCamelCase</NetJsBuildFlags>_

This is now emitted like ``$assembly.LN.L.V``

More details later...