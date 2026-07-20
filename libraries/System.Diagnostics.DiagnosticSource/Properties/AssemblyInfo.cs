
using NetJs;

[assembly: AssemblyHandle(AssemblyHandleAttribute.SystemDiagnosticsDiagnosticSource)]
[assembly:Reflectable(false)]//FIX: a recursion in the type metadata build
