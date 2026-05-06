using NetJs;
using System.Collections;
using System.Collections.Generic;

namespace System
{
    public class YieldToIterator<T> : IEnumerable<T>
    {
        [ObjectLiteral]
        public class IteratorResult
        {
            [Name("value")]
            public T? Value { get; set; }
            [Name("done")]
            public bool Done { get; set; }
        }

        public interface IGenerator
        {
            [Name("next")]
            IteratorResult Next();
        }

        [NativeDelegate]
        Func<IGenerator> _getGenerator;
        public YieldToIterator([NativeDelegate] Func<IGenerator> getGenerator)
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

        class Enumerator : IEnumerator<T>
        {
            IGenerator _generator;
            T? _current;
            public Enumerator(IGenerator generator)
            {
                _generator = generator;
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
        }
    }
}