using System;
using System.IO;
using System.Threading;

class ProjectContext
{
    public ProjectContext(FileSystemWatcher razorWatcher, FileSystemWatcher csWatcher)
    {
        RazorWatcher = razorWatcher;
        CsWatcher = csWatcher;
    }

    public DateTime LastProcessed { get; set; }
    public FileSystemWatcher RazorWatcher { get; }
    public FileSystemWatcher CsWatcher { get; }
    public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1);
}
//var code = result.ToString();
//Console.WriteLine(code);
