using NetJs;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetJs
{
    public class YieldToIterator<T> : IEnumerable<T>, IAsyncEnumerable<T>
    {
        NativeFunction<IGenerator<T>> _getGenerator;
        public YieldToIterator(NativeFunction<IGenerator<T>> getGenerator)
        {
            _getGenerator = getGenerator;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_getGenerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(_getGenerator(), cancellationToken);
        }

        class Enumerator : IEnumerator<T>, IAsyncEnumerator<T>
        {
            IGenerator<T> _generator;
            T? _current;
            CancellationToken _cancellationToken;
            public Enumerator(IGenerator<T> generator, CancellationToken cancellationToken = default)
            {
                _generator = generator;
                _cancellationToken = cancellationToken;
            }

            public T Current => _current!;
            object IEnumerator.Current => _current!;

            public void Dispose()
            {
            }

            bool alreadyDone;
            public bool MoveNext()
            {
                if (alreadyDone)
                    return false;
                var nxt = _generator.Next();
                alreadyDone = nxt.Done;
                _current = nxt.Value;
                return !nxt.Done;
            }

            public void Reset()
            {
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (alreadyDone)
                    return false;
                var nxt = await _generator.Next().As<Task<IGeneratorIteratorResult<T>>>();
                alreadyDone = nxt.Done;
                _current = nxt.Value;
                return !nxt.Done;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}