using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// File System Access API handles.
    /// </summary>
    [NetJs.External]
    public class FileSystemFileHandle
    {
        public extern Promise<File> getFile();
        public extern Promise<FileSystemWritableFileStream> createWritable();
        public extern Promise<bool> isSameEntry(FileSystemFileHandle other);
    }

    [NetJs.External]
    public class FileSystemDirectoryHandle
    {
        public extern Promise<FileSystemFileHandle?> getFileHandle(string name, object? options = null);
        public extern Promise<FileSystemDirectoryHandle?> getDirectoryHandle(string name, object? options = null);
        public extern Promise<bool> removeEntry(string name, object? options = null);
        public extern Promise<string[]> keys();
    }

    [NetJs.External]
    public class FileSystemWritableFileStream
    {
        public extern Promise<object> write(object data);
        public extern Promise<object> close();
        public extern Promise<object> seek(long position);
    }
}