using System;

namespace Window
{
    /// <summary>
    /// File extends Blob with filesystem metadata.
    /// </summary>
    [NetJs.External]
    public class File : Blob
    {
        public extern File(object? parts, string name, object? options = null);
        public extern string name { get; }
        public extern long lastModified { get; }
    }
}