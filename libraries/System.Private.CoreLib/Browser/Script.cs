using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetJs
{
    [External]
    //[Name("dotnetJs")]
    public static class Script
    {
        [Template("{0}")]
        public static extern ref T Ref<T>(RefOrPointer<T> value);
        [Template("{0}")]
        public static extern RefOrPointer<T> Ref<T>(ref readonly T value);
        [Template("{0}")]
        public static unsafe extern RefOrPointer<T> Ref<T>(T* value);
        [Template("{0}")]
        public static unsafe extern RefOrPointer<T> Ref<T>(void* value);
        [Template("{0}")]
        public static unsafe extern T* RefP<T>(RefOrPointer<T> value);
        [Template("{0}")]
        public static unsafe extern void* RefAsVoidPointer<T>(RefOrPointer<T> value);
        [Template("{0}")]
        public static unsafe extern ref T PointerARef<T>(T* value) where T : allows ref struct;
        [Template("{0}")]
        public static unsafe extern T* RefAsPointer<T>(ref readonly T value) where T : allows ref struct;
        [Template("({0}._ === true)")]
        public static extern bool IsDiscardRef<T>(ref readonly T value) where T : allows ref struct;
        [Template("({0}._ === true)")]
        public static extern bool IsDiscardRef<T>(RefOrPointer<T> value);
        //[Template("{0}")]
        //public static extern ref T ArrayRef<T>(ArrayRef<T> value);
        //[Template("{0}")]
        //public static extern ArrayRef<T> ArrayRef<T>(ref T value);
        [Template("JSON.stringify({obj})")]
        public static extern string JSONStringify(object? obj);
        [Template("JSON.parse({obj})")]
        public static extern T JSONParse<T>(string obj);
        [Template("{function}.apply({_this}, {values})")]
        public static extern object Apply(object? _this, object function, params object?[] values);
        [Template("{function}.apply({_this}, {values})")]
        public static extern object Apply(object? _this, object function, object values);
        [Template("{function}.apply({_this}, {values})")]
        public static extern T Apply<T>(object? _this, object function, object values);
        [Template("{obj1} == {obj2}")]
        public static extern bool LooseEqual(object obj1, object obj2);
        [Template("{obj1} != {obj2}")]
        public static extern bool LooseNotEqual(object obj1, object obj2);
        [Template("{obj1} === {obj2}")]
        public static extern bool StrictEqual(object obj1, object obj2);
        [Template("{obj1} !== {obj2}")]
        public static extern bool StrictNotEqual(object obj1, object obj2);

        [Template("typeof({value}) === 'number'")]
        public static extern bool IsNumber(object value);
        [Template("typeof({value}) === 'Date'")]
        public static extern bool IsDate(object value);

        [Template("{value}")]
        public extern static bool IsDefined(object? value);
        [Template("{value}")]
        public extern static bool HasValue(object? value);
        [Template("{value} === undefined")]
        public static extern bool IsUndefined(object? value);
        [Template("{value} == null")]
        public static extern bool IsUndefinedOrNull(object? value);
        [Template("{key} in {value}")]
        public static extern bool KeyIn(string key, object value);
        //public static bool IsDefined(object? value, bool noNull = false)
        //{
        //    return StrictNotEqual(TypeOf(value), "undefined") && (noNull ? value != null : true);
        //}

        //public static bool IsUndefined(object value, bool noNull = false)
        //{
        //    return !IsDefined(value, noNull);
        //}
        [Template("(({value}) << 0)")]
        public static extern int AsSigned(int value);
        [Template("(({value}) << 0)")]
        public static extern int AsSigned(uint value);
        [Template("(({value}) >>> 0)")]
        public static extern uint AsUnsigned(int value);
        [Template("(({value}) >>> 0)")]
        public static extern uint AsUnsigned(uint value);
        [Template("(({value}) >>> {shift})")]
        public static extern uint UnsignedRightShift(int value, int shift);
        [Template("(({value}) >>> {shift})")]
        public static extern uint UnsignedRightShift(uint value, int shift);
        [Template("((({value}) << {shift}) >>> 0)")]
        public static extern uint UnsignedLeftShift(int value, int shift);
        [Template("((({value}) << {shift}) >>> 0)")]
        public static extern uint UnsignedLeftShift(uint value, int shift);
        [Template("arguments")]
        public static extern object[] Arguments();
        [Template("arguments[{index}]")]
        public static extern T Argument<T>(int index);
        [Template("Array.isArray({obj})")]
        public static extern bool IsArray(object? obj);

        [Template("dotnetJs.toArray({items})")]
        public static extern T[] ToArray<T>(IEnumerable<T> items);
        [Template("Array.from({array})")]
        public static extern byte[] ArrayFrom(Window.Uint8Array array);

        [Template("[]")]
        public static extern T[] NewArray<T>();
        [Template("new Array({length})")]
        public static extern T[] NewArray<T>(int length);
        [Template("{values}")]
        public static extern T[] CreateArrayFromValues<T>(params T[] values);

        public static extern T Identity<T>(T arg, params object[] args);

        /// <summary>
        /// Emit a return statement
        /// </summary>
        /// <param name="obj">An object to return.</param>
        [Template("return {0}")]
        public static extern void Return(object obj);

        /// <summary>
        /// The delete operator removes a property from an object.
        /// </summary>
        /// <param name="obj">The name of an object, or an expression evaluating to an object.</param>
        /// <returns>true for all cases except when the property is an own non-configurable property, in which case, false is returned in non-strict mode.</returns>
        [Template("delete {0}")]
        public static extern bool Delete(object? obj);

        /// <summary>
        /// The delete operator removes a property from an object.
        /// </summary>
        /// <param name="obj">The name of an object, or an expression evaluating to an object.</param>
        /// <param name="prop">The property to delete.</param>
        /// <returns>true for all cases except when the property is an own non-configurable property, in which case, false is returned in non-strict mode.</returns>
        [Template("delete {0}[{1}]")]
        public static extern bool Delete(object obj, string prop);
        public static extern bool Delete(object obj, Window.Symbol prop);

        [Template("dotnetJs.is({0}, {1})")]
        public static extern bool Is(object type, string typeName);

        [Template("dotnetJs.copy({0}, {1}, {2})")]
        public static extern object Copy(object to, object from, string[] keys);

        [Template("dotnetJs.copy({0}, {1}, {2})")]
        public static extern object Copy(object to, object from, string keys);

        [Template("dotnetJs.copy({0}, {1}, {2}, {3})")]
        public static extern object Copy(object to, object from, string[] keys, bool toIf);

        [Template("dotnetJs.copy({0}, {1}, {2}, {3})")]
        public static extern object Copy(object to, object from, string keys, bool toIf);

        [Template("dotnetJs.ns({0}, {1})")]
        public static extern object NS(string ns, object scope);

        [Template("dotnetJs.ns({0})")]
        public static extern object NS(string ns);

        [Template("dotnetJs.getHashCode({0})")]
        public static extern int GetHashCode(object obj);

        [Template("{global.}" + Constants.DefaultTypeName + "({0})")]
        public static extern T GetDefaultValue<T>(Type type);

        [Template("{global.}" + Constants.DefaultTypeName + "({0})")]
        public static extern object GetDefaultValue(TypePrototype type);
        [Template("{global.}" + Constants.DefaultTypeName + "({0}._prototype)")]
        public static extern object GetDefaultValue(Type type);
        /// <summary>
        /// Checks if the specified object is undefined. The object passed in should be a local variable, and not a member of a class (to avoid potential script warnings).
        /// </summary>
        /// <param name="obj">The object to test against undefined.</param>
        /// <returns>true if the object is undefined; false otherwise.</returns>

        /// <summary>
        /// Checks if the object has a value.
        /// </summary>
        /// <param name="obj">The object to test if there is a value.</param>
        /// <returns>true if the object has a value; false otherwise.</returns>
        //[Template("dotnetJs.hasValue({0})")]
        //public static extern bool HasValue(object obj);

        /// <summary>
        /// Checks if the specified object is null.
        /// </summary>
        /// <param name="obj">The object to test against null.</param>
        /// <returns>true if the object is null; false otherwise.</returns>
        [Template("{0} === null")]
        public static extern bool IsNull(object obj);

        /// <summary>
        /// Converts an object into a boolean.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>true if the object is not null, zero, empty string or undefined.</returns>
        [Template("!!{0}")]
        public static extern bool Boolean(object obj);

        /// <summary>
        /// Generate <c>member in obj</c>.
        /// </summary>
        /// <param name="obj">The object to test against.</param>
        /// <param name="member">The member to check if in the object.</param>
        /// <returns>true if member in object; false otherwise.</returns>
        [Template("{member} in {obj}")]
        public static extern bool In(object obj, string member);
        [Template("{member} in {obj}")]
        public static extern bool In(object obj, Window.Symbol member);

        /// <summary>
        /// Invoke a method on an object
        /// </summary>
        /// <param name="obj">The object to invoke the method against.</param>
        /// <param name="name">The method to invoke.</param>
        /// <param name="args">The arguments passed into the method.</param>
        /// <returns></returns>
        [Template("{obj}[{name}]({*args})")]
        public static extern object InvokeMethod(object obj, string name, params object[] args);

        /// <summary>
        /// Inject javascript code
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [Template]
        public static extern T Write<T>(string code, params object[] args) where T : allows ref struct;
        [Template]
        public static extern ref T WriteRef<T>(string code, params object[] args) where T : allows ref struct;

        /// <summary>
        /// Inject javascript code
        /// </summary>
        /// <param name="code"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [Template]
        public static extern void Write(string code, params object[] args);

        /// <summary>
        /// The global undefined property represents the value undefined.
        /// </summary>
        [Template("undefined")]
        public static readonly object Undefined;

        /// <summary>
        /// The global NaN property is a value representing Not-A-Number.
        /// </summary>
        [Template("NaN")]
        public static readonly double NaN;

        /// <summary>
        /// The global Infinity property is a numeric value representing infinity.
        /// </summary>
        [Template("Infinity")]
        public static readonly double Infinity;
        [Template("Number.NEGATIVE_INFINITY")]
        public static readonly double NumberNegativeInfinity;
        [Template("Number.POSITIVE_INFINITY")]
        public static readonly double NumberPositiveInfinity;

        [Template("debugger")]
        public static extern void Debugger();

        /// <summary>
        /// The eval() method evaluates JavaScript code represented as a string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">A string representing a JavaScript expression, statement, or sequence of statements. The expression can include variables and properties of existing objects.</param>
        /// <returns></returns>
        [Template("eval({0})")]
        public static extern T Eval<T>(string expression);

        /// <summary>
        /// The eval() method evaluates JavaScript code represented as a string.
        /// </summary>
        /// <param name="expression">A string representing a JavaScript expression, statement, or sequence of statements. The expression can include variables and properties of existing objects.</param>
        /// <returns></returns>
        [Template("eval({0})")]
        public static extern void Eval(string expression);

        /// <summary>
        /// The global isFinite() function determines whether the passed value is a finite number. If needed, the parameter is first converted to a number.
        /// </summary>
        /// <param name="testValue">The value to be tested for finiteness.</param>
        /// <returns></returns>
        [Template("isFinite({0})")]
        public static extern bool IsFinite(object testValue);

        /// <summary>
        /// Parses a string argument and returns a floating point number corresponding to double .Net type.
        /// </summary>
        /// <param name="value">A string that represents the value you want to parse.</param>
        /// <returns>Parsed floating point number with type corresponding to double .Net type</returns>
        [Template("parseFloat({0})")]
        public static extern double ParseFloat(string value);

        /// <summary>
        /// The parseInt() function parses a string argument and returns an integer of the specified radix or base.
        /// </summary>
        /// <param name="value">The value to parse. If string is not a string, then it is converted to one. Leading whitespace in the string is ignored.</param>
        /// <returns></returns>
        [Template("parseInt({0})")]
        public static extern int ParseInt(string value);

        /// <summary>
        /// The parseInt() function parses a string argument and returns an integer of the specified radix or base.
        /// </summary>
        /// <param name="value">The value to parse. If string is not a string, then it is converted to one. Leading whitespace in the string is ignored.</param>
        /// <param name="radix">An integer that represents the radix of the above mentioned string. Always specify this parameter to eliminate reader confusion and to guarantee predictable behavior. Different implementations produce different results when a radix is not specified.</param>
        /// <returns></returns>
        [Template("parseInt({0}, {1})")]
        public static extern int ParseInt(string value, int radix);

        /// <summary>
        /// The isNaN() function determines whether a value is NaN or not. Be careful, this function is broken. You may be interested in Number.isNaN() as defined in ECMAScript 6 or you can use typeof to determine if the value is Not-A-Number.
        /// </summary>
        /// <param name="testValue">The value to be tested.</param>
        /// <returns></returns>
        [Template("isNaN({0})")]
        public static extern bool IsNaN(object testValue);
        //[Template("{0}.abs()")]
        //public static extern TNumber MathAbs<TNumber>(TNumber number);
        //[Template("Math.pow({number}, {power})")]
        //public static extern double MathPower<TNumber>(TNumber number, TNumber power);
        //[Template("Math.pow({number}, {power})")]
        //public static extern TNumber MathFloor<TNumber>(TNumber number);

        /// <summary>
        /// The decodeURI() function decodes a Uniform Resource Identifier (URI) previously created by encodeURI or by a similar routine.
        /// </summary>
        /// <param name="encodedURI">A complete, encoded Uniform Resource Identifier.</param>
        /// <returns></returns>
        [Template("decodeURI({0})")]
        public static extern string DecodeURI(string encodedURI);

        /// <summary>
        /// The decodeURIComponent() method decodes a Uniform Resource Identifier (URI) component previously created by encodeURIComponent or by a similar routine.
        /// </summary>
        /// <param name="encodedURI">An encoded component of a Uniform Resource Identifier.</param>
        /// <returns></returns>
        [Template("decodeURIComponent({0})")]
        public static extern string DecodeURIComponent(string encodedURI);

        /// <summary>
        /// The encodeURI() method encodes a Uniform Resource Identifier (URI) by replacing each instance of certain characters by one, two, three, or four escape sequences representing the UTF-8 encoding of the character (will only be four escape sequences for characters composed of two "surrogate" characters).
        /// </summary>
        /// <param name="uri">A complete Uniform Resource Identifier.</param>
        /// <returns></returns>
        [Template("encodeURI({0})")]
        public static extern string EncodeURI(string uri);

        /// <summary>
        /// The encodeURIComponent() method encodes a Uniform Resource Identifier (URI) component by replacing each instance of certain characters by one, two, three, or four escape sequences representing the UTF-8 encoding of the character (will only be four escape sequences for characters composed of two "surrogate" characters).
        /// </summary>
        /// <param name="component">A component of a URI.</param>
        /// <returns></returns>
        [Template("encodeURIComponent({0})")]
        public static extern string EncodeURIComponent(string component);

        [Template("{global.}" + Constants.BoxName + "({0}, {1})")]
        public static extern object Box(object? value, TypePrototype type);
        [Template("{global.}" + NetJs.Constants.UnboxName + "({0})")]
        public static extern object Unbox(object? obj);
        [Template("typeof {0}")]
        public static extern string TypeOf(object? obj);
        [Template("typeof {0}")]
        public static extern unsafe string TypeOf(void* obj);

        [Template("({obj} instanceof {type})")]
        public static extern bool InstanceOf(object? obj, TypePrototype type);
        [Template("({obj} instanceof {type}._prototype)")]
        public static extern bool InstanceOf(object? obj, Type type);

        [Template("this")]
        public static extern T This<T>();

        [Template("(dotnetJs.caller[0] || this)")]
        public static extern T Caller<T>();

        [Template("{scope:raw}[{name}] = {value}")]
        public static extern void Set(object scope, string name, object value);

        [Template("{name:raw} = {value}")]
        public static extern void Set(string name, object value);

        [Template("{name:raw}")]
        public static extern object Get(string name);

        [Template("{scope:raw}[{name}]")]
        public static extern object Get(object scope, string name);

        [Template("{name:raw}")]
        public static extern T Get<T>(string name);

        [Template("{scope:raw}[{name}]")]
        public static extern T Get<T>(object scope, string name);

        [Template("{name:raw}({args})")]
        public static extern void Call(string name, params object[] args);

        [Template("{name:raw}()")]
        public static extern void Call(string name);

        [Template("{name:raw}({args})")]
        public static extern T Call<T>(string name, params object[] args);

        [Template("{name:raw}()")]
        public static extern T Call<T>(string name);

        //[GlobalTarget("dotnetJs.global")]
        //[Template("$global")]
        //public new static extern dynamic ToDynamic();

        [Template("({a} === {b})")]
        public static extern bool StrictEquals(object a, object b);

        [Template("{init}({t})")]
        public static extern T CallFor<T>(T t, Func<T, T> init);

        [Template("{init}({t})")]
        public static extern Task<T> AsyncCallFor<T>(T t, Func<T, Task<T>> init);

        [Template("({name:tmp} = {t})")]
        [Unbox(false)]
        public static extern T ToTemp<T>(string name, T t);

        [Template("{name:gettmp}")]
        [Unbox(false)]
        public static extern T FromTemp<T>(string name);

        [Template("{name:gettmp}")]
        [Unbox(false)]
        public static extern T FromTemp<T>(string name, T t);

        [Template("{action:body}")]
        public static extern object FromLambda(NativeAction action);

        [Template("{o:plain}")]
        public static extern T ToPlainObject<T>(T o);

        [Template("{o:plain}")]
        public static extern T ToObjectLiteral<T>(T o);

        /// <summary>
        /// Runs the function in a try/catch statement
        /// </summary>
        /// <param name="fn">Function to run</param>
        /// <returns>Return either function result or false in case of catch</returns>
        [Template("dotnetJs.safe({fn})")]
        public static extern bool SafeFunc(NativeFunction<bool> fn);

        [Template("dotnetJs.isNode")]
        public static readonly bool IsNode;

        [Template("dotnetJs.Deconstruct({obj}, {t1})")]
        public extern static void Deconstruct<T1>(object obj, out T1 t1);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2})")]
        public extern static void Deconstruct<T1, T2>(object obj, out T1 t1, out T2 t2);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3})")]
        public extern static void Deconstruct<T1, T2, T3>(object obj, out T1 t1, out T2 t2, out T3 t3);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3}, {t4})")]
        public extern static void Deconstruct<T1, T2, T3, T4>(object obj, out T1 t1, out T2 t2, out T3 t3, out T4 t4);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3}, {t4}, {t5})")]
        public extern static void Deconstruct<T1, T2, T3, T4, T5>(object obj, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3}, {t4}, {t5}, {t6})")]
        public extern static void Deconstruct<T1, T2, T3, T4, T5, T6>(object obj, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3}, {t4}, {t5}, {t6}, {t7})")]
        public extern static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(object obj, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7);

        [Template("dotnetJs.Deconstruct({obj}, {t1}, {t2}, {t3}, {t4}, {t5}, {t6}, {t7}, {rest})")]
        public extern static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, TRest>(object obj, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7, out TRest rest);

        [Name("_")]
        [Unbox(false)]
        public static object Discard;
    }
}
