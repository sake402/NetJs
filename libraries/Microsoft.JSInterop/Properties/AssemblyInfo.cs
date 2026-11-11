
using Microsoft.JSInterop.Infrastructure;
using NetJs;
using System.Diagnostics.CodeAnalysis;

[assembly: Attached(typeof(DotNetObjectReferenceJsonConverter<>), typeof(DynamicDependencyAttribute), "$ctor(Microsoft.JSInterop.JSRuntime)")]