using System;
using System.Reflection.Emit;
using System.Text;

namespace System.Collections
{
    [NetJs.ForcePartial(typeof(IEnumerable))]
    public partial interface IEnumerable_Partial
    {
        [NetJs.Name("[Symbol.iterator]")]
        public IGenerator<object> Iterator()
        {
            if (Array.Is(this, null))
            {
                return NetJs.Script.Write<IGenerator<object>>("Array.prototype[Symbol.iterator].call(this)"); //Native array iterator will be faster
            }
            return new IEnumerableIterator(this.As<IEnumerable>());
        }
    }

    [NetJs.Reflectable(false)]
    class IEnumerableIterator : IGenerator<object>
    {
        IEnumerator enumerator;

        public IEnumerableIterator(IEnumerable enumerator)
        {
            this.enumerator = enumerator.GetEnumerator();
        }

        bool alreadyDone;
        public IGeneratorIteratorResult<object> Next()
        {
            if (alreadyDone)
                return new GeneratorIteratorResult<object> { Done = true };
            var nxt = enumerator.MoveNext();
            alreadyDone = !nxt;
            return new GeneratorIteratorResult<object>
            {
                Done = alreadyDone,
                Value = nxt ? enumerator.Current : null!
            };
        }
    }
}

//namespace System.Collections.Generic
//{
//    [NetJs.ForcePartial(typeof(IEnumerable<>))]
//    public partial interface IEnumerable_Partial<out T>
//        where T : allows ref struct
//    {
//        [NetJs.Name("[Symbol.iterator]")]
//        public IGenerator<T> Iterator()
//        {
//            return new IEnumerableIterator<T>(this.As<IEnumerable<T>>());
//        }
//    }


//    [NetJs.IgnoreGeneric]
//    class IEnumerableIterator<T> : IGenerator<T>
//        where T : allows ref struct
//    {
//        IEnumerator<T> enumerator;

//        public IEnumerableIterator(IEnumerable<T> enumerator)
//        {
//            this.enumerator = enumerator.GetEnumerator();
//        }

//        bool alreadyDone;
//        public IGeneratorIteratorResult<T> Next()
//        {
//            if (alreadyDone)
//                return new GeneratorIteratorResult<object> { Done = true }.As<IGeneratorIteratorResult<T>>();
//            var nxt = enumerator.MoveNext();
//            alreadyDone = !nxt;
//            var val = enumerator.Current;
//            return new GeneratorIteratorResult<object>
//            {
//                Done = false,
//                Value = val.As<object>()
//            }.As<IGeneratorIteratorResult<T>>();
//        }
//    }
//}