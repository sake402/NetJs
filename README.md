OK! C#+Blazor is cool🔥🔥🔥.

Ah well, until you host it and your website visitors have a poor bandwidth.😠😠😠

Blazor typically has two hosting model, each with its pros and cons.

## Server Hosting
Here your app visitor connect to your app over signalR socket while the app and the client sends events and UI updates back and forth.

It is great, fast, until you have som poor bandwidth users. They'd keep getting disconnected every now and then and UI responsiveness is poor. Even when you want to show a loading indicator, this wont happen until the server can send that UI update to the client.
Also doesnt scale too well for a site with hundreds of thousands of visitors, as each visitor is using the server resource in connections and memory.

## Webassemby Hosting
Here the whole app is downloaded into the client/browser. Performance is good when loaded, but statup time is poor.

The first time the visitor visit, they'd have to downloas 10s of MB of wasm files into the browser, which for a relatively large website, is very slow.
On subsequent visit, the browser uses the cached files, but startup is still very poor because the dotnet runtime has to load and initialize before user code can even run. 
And the webassembly is mostly interpreted in the browser, making it much slower than javascript itself.
Of course, you also have the option of AOT, but that is way worse as the binary size is just too large for you to want to use it in production.

# Motivation
NetJs (initially BlazorJS) was conceived to bring blazor natively to javascript, without changing a thing in the existing codebase being used for Blazor Server or Blazor Wasm.

I was building a social platform https://zoey.africa in blazor. Started with Blazor Server and the performance is goo, but we soon realize we cant scale this to millions of users without having to
deploy a mammoth server with huge resource.

We therefore migrated to Blazor WebAssembly, but the startup time makes it unbearable as well, given that wasm is interpreted.

And then the eureka moment: What If we have Blazor Javascript Hosting model, where everything will be transpiled to javascript to be run natively.

Turns out we can and we did.

# Usage
Install NetJs tool
```
dotnet tool install --global NetJs.Compiler
```

Enter the directory where your project is and execute ``dotnet build`` first to make sure project builds.

Then execute ``NetJs build``.

Finally open your app in browser with ``NetJs serve -o``
```
dotnet build -p:EmitCompilerGeneratedFiles=true
NetJs build
NetJs serve -o
```

Once build completes, Find a wwwroot folder in your
Supports and tested with Sdks _Microsoft.NET.Sdk.BlazorWebAssembly_, _Microsoft.NET.Sdk.Razor_, _Microsoft.NET.Sdk_

# Notes
If building a razor project or any project with source generators, you must have _<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>_ in your PropertyGroup or execute _dotnet build -p:EmitCompilerGeneratedFiles=true_ first.

This ensures that the csharp code files are generated into the obj directory first. They are picked up by NetJs for traspilation.

# Code size and minification
By default, identifiers for namespace, class and members are maintained as is. 

Given
```C#
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

You can enable name minification using NetJsBuildFlags in PropertyGroup
```<NetJsBuildFlags>Global,InlineConstants,SingleFile,MinifyNamespaces,MinifyTypeNames,MinifyMemberNames,ShortNamesCreateFromCamelCase</NetJsBuildFlags>```

This is now emitted like ``$assembly.LN.L.V``

# Trimming
TODO

# Package Supports
Of course transpiling your project to js requires its dependecies to be transpiled too. We have transpiles many core client dotnet packages already and NetJs knows to pull these in and replace them when buiding your project

For example if your project references ``System.Net.Http`` package, NetJs will pull in ``NetJs.System.Net.Http`` as a substitute for it when building.

Build package are currently hosted on github too.

If you need a package, that isnt built yet, you can download the repo of such package and build it locally or request for us to build such and add it to our 3rd party library package.
