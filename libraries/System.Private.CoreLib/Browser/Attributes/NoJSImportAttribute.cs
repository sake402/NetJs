using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs
{
    /// <summary>
    /// Ignores the earlier defined JsImport
    /// </summary>
    [NonScriptable]
    public class NoJSImportAttribute : Attribute
    {
    }
}
