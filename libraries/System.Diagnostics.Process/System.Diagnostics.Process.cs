// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeProcessHandle() : base (default(bool)) { }
        public SafeProcessHandle(System.IntPtr existingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw new System.PlatformNotSupportedException(); }
    }
}
namespace System.Diagnostics
{
    public partial class DataReceivedEventArgs : System.EventArgs
    {
        internal DataReceivedEventArgs() { }
        public string? Data { get { throw new System.PlatformNotSupportedException(); } }
    }
    public delegate void DataReceivedEventHandler(object sender, System.Diagnostics.DataReceivedEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class MonitoringDescriptionAttribute : System.ComponentModel.DescriptionAttribute
    {
        public MonitoringDescriptionAttribute(string description) { }
        public override string Description { get { throw new System.PlatformNotSupportedException(); } }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class Process : System.ComponentModel.Component, System.IDisposable
    {
        public Process() { }
        public int BasePriority { get { throw new System.PlatformNotSupportedException(); } }
        public bool EnableRaisingEvents { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int ExitCode { get { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime ExitTime { get { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr Handle { get { throw new System.PlatformNotSupportedException(); } }
        public int HandleCount { get { throw new System.PlatformNotSupportedException(); } }
        public bool HasExited { get { throw new System.PlatformNotSupportedException(); } }
        public int Id { get { throw new System.PlatformNotSupportedException(); } }
        public string MachineName { get { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ProcessModule? MainModule { get { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr MainWindowHandle { get { throw new System.PlatformNotSupportedException(); } }
        public string MainWindowTitle { get { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr MaxWorkingSet { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos"), System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] get { throw new System.PlatformNotSupportedException(); } [System.Runtime.Versioning.SupportedOSPlatformAttribute("freebsd"), System.Runtime.Versioning.SupportedOSPlatformAttribute("macos"), System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst"), System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr MinWorkingSet { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos"), System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] get { throw new System.PlatformNotSupportedException(); } [System.Runtime.Versioning.SupportedOSPlatformAttribute("freebsd"), System.Runtime.Versioning.SupportedOSPlatformAttribute("macos"), System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst"), System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ProcessModuleCollection Modules { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.NonpagedSystemMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.NonpagedSystemMemorySize64 instead.")]
        public int NonpagedSystemMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long NonpagedSystemMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PagedMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PagedMemorySize64 instead.")]
        public int PagedMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long PagedMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PagedSystemMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PagedSystemMemorySize64 instead.")]
        public int PagedSystemMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long PagedSystemMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PeakPagedMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakPagedMemorySize64 instead.")]
        public int PeakPagedMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long PeakPagedMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PeakVirtualMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakVirtualMemorySize64 instead.")]
        public int PeakVirtualMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long PeakVirtualMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PeakWorkingSet has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakWorkingSet64 instead.")]
        public int PeakWorkingSet { get { throw new System.PlatformNotSupportedException(); } }
        public long PeakWorkingSet64 { get { throw new System.PlatformNotSupportedException(); } }
        public bool PriorityBoostEnabled { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ProcessPriorityClass PriorityClass { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.PrivateMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PrivateMemorySize64 instead.")]
        public int PrivateMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long PrivateMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatform("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan PrivilegedProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        public string ProcessName { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        public System.IntPtr ProcessorAffinity { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool Responding { get { throw new System.PlatformNotSupportedException(); } }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle SafeHandle { get { throw new System.PlatformNotSupportedException(); } }
        public int SessionId { get { throw new System.PlatformNotSupportedException(); } }
        public System.IO.StreamReader StandardError { get { throw new System.PlatformNotSupportedException(); } }
        public System.IO.StreamWriter StandardInput { get { throw new System.PlatformNotSupportedException(); } }
        public System.IO.StreamReader StandardOutput { get { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ProcessStartInfo StartInfo { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.DateTime StartTime { get { throw new System.PlatformNotSupportedException(); } }
        public System.ComponentModel.ISynchronizeInvoke? SynchronizingObject { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ProcessThreadCollection Threads { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan TotalProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan UserProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.VirtualMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.VirtualMemorySize64 instead.")]
        public int VirtualMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public long VirtualMemorySize64 { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("Process.WorkingSet has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.WorkingSet64 instead.")]
        public int WorkingSet { get { throw new System.PlatformNotSupportedException(); } }
        public long WorkingSet64 { get { throw new System.PlatformNotSupportedException(); } }
        public event System.Diagnostics.DataReceivedEventHandler? ErrorDataReceived { add { } remove { } }
        public event System.EventHandler Exited { add { } remove { } }
        public event System.Diagnostics.DataReceivedEventHandler? OutputDataReceived { add { } remove { } }
        public void BeginErrorReadLine() { }
        public void BeginOutputReadLine() { }
        public void CancelErrorRead() { }
        public void CancelOutputRead() { }
        public void Close() { }
        public bool CloseMainWindow() { throw new System.PlatformNotSupportedException(); }
        protected override void Dispose(bool disposing) { }
        public static void EnterDebugMode() { }
        public static System.Diagnostics.Process GetCurrentProcess() { throw new System.PlatformNotSupportedException(); }
        public static System.Diagnostics.Process GetProcessById(int processId) { throw new System.PlatformNotSupportedException(); }
        public static System.Diagnostics.Process GetProcessById(int processId, string machineName) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process[] GetProcesses() { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process[] GetProcesses(string machineName) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process[] GetProcessesByName(string? processName) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process[] GetProcessesByName(string? processName, string machineName) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public void Kill() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public void Kill(bool entireProcessTree) { }
        public static void LeaveDebugMode() { }
        protected void OnExited() { }
        public void Refresh() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public bool Start() { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process? Start(System.Diagnostics.ProcessStartInfo startInfo) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process Start(string fileName) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process Start(string fileName, string arguments) { throw new System.PlatformNotSupportedException(); }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public static System.Diagnostics.Process Start(string fileName, System.Collections.Generic.IEnumerable<string> arguments) { throw new System.PlatformNotSupportedException(); }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Diagnostics.Process? Start(string fileName, string userName, System.Security.SecureString password, string domain) { throw new System.PlatformNotSupportedException(); }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Diagnostics.Process? Start(string fileName, string arguments, string userName, System.Security.SecureString password, string domain) { throw new System.PlatformNotSupportedException(); }
        public override string ToString() { throw new System.PlatformNotSupportedException(); }
        public void WaitForExit() { }
        public bool WaitForExit(int milliseconds) { throw new System.PlatformNotSupportedException(); }
        public bool WaitForExit(System.TimeSpan timeout) { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task WaitForExitAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw new System.PlatformNotSupportedException(); }
        public bool WaitForInputIdle() { throw new System.PlatformNotSupportedException(); }
        public bool WaitForInputIdle(int milliseconds) { throw new System.PlatformNotSupportedException(); }
        public bool WaitForInputIdle(System.TimeSpan timeout) { throw new System.PlatformNotSupportedException(); }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessModuleDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessModule : System.ComponentModel.Component
    {
        internal ProcessModule() { }
        public System.IntPtr BaseAddress { get { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr EntryPointAddress { get { throw new System.PlatformNotSupportedException(); } }
        public string FileName { get { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.FileVersionInfo FileVersionInfo { get { throw new System.PlatformNotSupportedException(); } }
        public int ModuleMemorySize { get { throw new System.PlatformNotSupportedException(); } }
        public string ModuleName { get { throw new System.PlatformNotSupportedException(); } }
        public override string ToString() { throw new System.PlatformNotSupportedException(); }
    }
    public partial class ProcessModuleCollection : System.Collections.ReadOnlyCollectionBase
    {
        protected ProcessModuleCollection() { }
        public ProcessModuleCollection(System.Diagnostics.ProcessModule[] processModules) { }
        public System.Diagnostics.ProcessModule this[int index] { get { throw new System.PlatformNotSupportedException(); } }
        public bool Contains(System.Diagnostics.ProcessModule module) { throw new System.PlatformNotSupportedException(); }
        public void CopyTo(System.Diagnostics.ProcessModule[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessModule module) { throw new System.PlatformNotSupportedException(); }
    }
    public enum ProcessPriorityClass
    {
        Normal = 32,
        Idle = 64,
        High = 128,
        RealTime = 256,
        BelowNormal = 16384,
        AboveNormal = 32768,
    }
    public sealed partial class ProcessStartInfo
    {
        public ProcessStartInfo() { }
        public ProcessStartInfo(string fileName) { }
        public ProcessStartInfo(string fileName, string arguments) { }
        public ProcessStartInfo(string fileName, System.Collections.Generic.IEnumerable<string> arguments) { }
        public System.Collections.ObjectModel.Collection<string> ArgumentList { get { throw new System.PlatformNotSupportedException(); } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Arguments { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public bool CreateNewProcessGroup { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool CreateNoWindow { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Domain { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Collections.Generic.IDictionary<string, string?> Environment { get { throw new System.PlatformNotSupportedException(); } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.StringDictionaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public System.Collections.Specialized.StringDictionary EnvironmentVariables { get { throw new System.PlatformNotSupportedException(); } }
        public bool ErrorDialog { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr ErrorDialogParentHandle { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.StartFileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string FileName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public bool LoadUserProfile { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public bool UseCredentialsForNetworkingOnly { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public System.Security.SecureString? Password { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public string? PasswordInClearText { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool RedirectStandardError { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool RedirectStandardInput { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool RedirectStandardOutput { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Text.Encoding? StandardErrorEncoding { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Text.Encoding? StandardInputEncoding { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Text.Encoding? StandardOutputEncoding { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string UserName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool UseShellExecute { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Verb { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string[] Verbs { get { throw new System.PlatformNotSupportedException(); } }
        [System.ComponentModel.DefaultValueAttribute(System.Diagnostics.ProcessWindowStyle.Normal)]
        public System.Diagnostics.ProcessWindowStyle WindowStyle { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.WorkingDirectoryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string WorkingDirectory { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessThreadDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessThread : System.ComponentModel.Component
    {
        internal ProcessThread() { }
        public int BasePriority { get { throw new System.PlatformNotSupportedException(); } }
        public int CurrentPriority { get { throw new System.PlatformNotSupportedException(); } }
        public int Id { get { throw new System.PlatformNotSupportedException(); } }
        public int IdealProcessor { set { throw new System.PlatformNotSupportedException(); } }
        public bool PriorityBoostEnabled { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ThreadPriorityLevel PriorityLevel { [System.Runtime.Versioning.SupportedOSPlatform("windows")] [System.Runtime.Versioning.SupportedOSPlatform("linux")] [System.Runtime.Versioning.SupportedOSPlatform("freebsd")] get { throw new System.PlatformNotSupportedException(); } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan PrivilegedProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public System.IntPtr ProcessorAffinity { set { throw new System.PlatformNotSupportedException(); } }
        public System.IntPtr StartAddress { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        public System.DateTime StartTime { get { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ThreadState ThreadState { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan TotalProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("maccatalyst")] // this needs to come after the ios attribute due to limitations in the platform analyzer
        public System.TimeSpan UserProcessorTime { get { throw new System.PlatformNotSupportedException(); } }
        public System.Diagnostics.ThreadWaitReason WaitReason { get { throw new System.PlatformNotSupportedException(); } }
        public void ResetIdealProcessor() { }
    }
    public partial class ProcessThreadCollection : System.Collections.ReadOnlyCollectionBase
    {
        protected ProcessThreadCollection() { }
        public ProcessThreadCollection(System.Diagnostics.ProcessThread[] processThreads) { }
        public System.Diagnostics.ProcessThread this[int index] { get { throw new System.PlatformNotSupportedException(); } }
        public int Add(System.Diagnostics.ProcessThread thread) { throw new System.PlatformNotSupportedException(); }
        public bool Contains(System.Diagnostics.ProcessThread thread) { throw new System.PlatformNotSupportedException(); }
        public void CopyTo(System.Diagnostics.ProcessThread[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessThread thread) { throw new System.PlatformNotSupportedException(); }
        public void Insert(int index, System.Diagnostics.ProcessThread thread) { }
        public void Remove(System.Diagnostics.ProcessThread thread) { }
    }
    public enum ProcessWindowStyle
    {
        Normal = 0,
        Hidden = 1,
        Minimized = 2,
        Maximized = 3,
    }
    public enum ThreadPriorityLevel
    {
        Idle = -15,
        Lowest = -2,
        BelowNormal = -1,
        Normal = 0,
        AboveNormal = 1,
        Highest = 2,
        TimeCritical = 15,
    }
    public enum ThreadState
    {
        Initialized = 0,
        Ready = 1,
        Running = 2,
        Standby = 3,
        Terminated = 4,
        Wait = 5,
        Transition = 6,
        Unknown = 7,
    }
    public enum ThreadWaitReason
    {
        Executive = 0,
        FreePage = 1,
        PageIn = 2,
        SystemAllocation = 3,
        ExecutionDelay = 4,
        Suspended = 5,
        UserRequest = 6,
        EventPairHigh = 7,
        EventPairLow = 8,
        LpcReceive = 9,
        LpcReply = 10,
        VirtualMemory = 11,
        PageOut = 12,
        Unknown = 13,
    }
}
