
using Microsoft.Extensions.Configuration;
using NetJs;

[assembly: Attached(typeof(ConfigurationDebugViewContext), typeof(NonScriptableAttribute))]
[assembly: Attached(typeof(ConfigurationRootExtensions), typeof(NonScriptableAttribute))]
