using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    public partial interface IUIContent : IDisposable
    {
        UIFrameState State { get; }
        //void Build(object key = null);
    }
}
