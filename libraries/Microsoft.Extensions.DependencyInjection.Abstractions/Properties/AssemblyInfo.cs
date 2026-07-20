
using Microsoft.Extensions.DependencyInjection;
using NetJs;


[assembly: AssemblySlug("mxdi")]

[assembly: AssemblyHandle(AssemblyHandleAttribute.MicrosoftExtensionsDependencyInjectionAbstractions)]
[assembly: Attached(typeof(ServiceLifetime), typeof(InlineConstAttribute))]
