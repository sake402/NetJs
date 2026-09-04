
using Microsoft.Extensions.DependencyInjection;
using NetJs;


[assembly: AssemblySlug("mxdi")]

[assembly: Attached(typeof(ServiceLifetime), typeof(InlineConstAttribute))]
