using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    public partial class DisposableDelegate : IDisposable
    {
        Action dispose;

        public DisposableDelegate(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            dispose?.Invoke();
        }
    }
}
