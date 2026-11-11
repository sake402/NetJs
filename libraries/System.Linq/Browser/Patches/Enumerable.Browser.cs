
//namespace System.Linq
//{
//    public static partial class Enumerable
//    {
//        [NetJs.VerbatimReplacement(@"case 2:
//                        Debug.Assert(_enumerator is not null);
//                        Debug.Assert(_set is not null);
//                        while (_enumerator.MoveNext())
//                        {
//                            element = _enumerator.Current;
//                            if (_set.Add(element))
//                            {
//                                _current = element;
//                                return true;
//                            }
//                        }

//                        break;", @"case 2:
//                        Debug.Assert(_enumerator is not null);
//                        Debug.Assert(_set is not null);
//                        while (_enumerator.MoveNext())
//                        {
//                            var element2 = _enumerator.Current;
//                            if (_set.Add(element2))
//                            {
//                                _current = element2;
//                                return true;
//                            }
//                        }

//                        break;")]
//        private sealed partial class DistinctIterator<TSource>
//        {

//        }
//    }
//}