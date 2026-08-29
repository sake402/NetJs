
using Microsoft.AspNetCore.Components.RenderTree;
using NetJs;

[assembly: AssemblyHandle(AssemblyHandleAttribute.MicrosoftAspnetCoreComponents)]

//Do not minify these member names as js rendering acceses them with the original names
[assembly: Attached(typeof(RenderBatch), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayRange<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeDiff), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeFrame), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayBuilderSegment<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(RenderTreeEdit), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(ArrayBuilder<>), typeof(KeepMemberNamesAttribute))]
[assembly: Attached(typeof(NamedEventChange), typeof(KeepMemberNamesAttribute))]
