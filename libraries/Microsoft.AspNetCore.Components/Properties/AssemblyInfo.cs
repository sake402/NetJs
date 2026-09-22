
using Microsoft.AspNetCore.Components.RenderTree;
using NetJs;
using System.Diagnostics.CodeAnalysis;

//Do not minify these member names as js rendering acceses them with the original names
[assembly: Attached(typeof(RenderBatch), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayRange<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeDiff), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeFrame), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayBuilderSegment<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeEdit), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayBuilder<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(NamedEventChange), typeof(KeepMemberNamesAttribute))]

//Keep all members of these as they as used in the blazor.netjs.js layer
[assembly: Attached(typeof(RenderBatch), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(ArrayRange<>), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(RenderTreeDiff), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(RenderTreeFrame), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(ArrayBuilderSegment<>), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(RenderTreeEdit), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(ArrayBuilder<>), typeof(DynamicDependencyAttribute), "*")]
[assembly: Attached(typeof(NamedEventChange), typeof(DynamicDependencyAttribute), "*")]