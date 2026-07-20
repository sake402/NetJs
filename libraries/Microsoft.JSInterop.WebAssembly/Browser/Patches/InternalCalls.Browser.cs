using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using NetJs;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;


namespace WebAssembly.JSInterop;

internal static partial class InternalCalls
{
    //[NoJSImport]
    public static extern partial string InvokeJSJson(
        string identifier,
        long targetInstanceId,
        int resultType,
        string argsJson,
        long asyncHandle,
        int callType);
    //{
    //    var _resultType = resultType.As<JSCallResultType>();
    //    var _callType = callType.As<JSCallType>();
    //    var jsObject = NetJs.Script.JSONParse<object>(argsJson);
    //    var lwindow = Window.Window.Instance.As<SimpleDictionary<object>>();
    //    object? result = null;
    //    switch (_callType)
    //    {
    //        case JSCallType.FunctionCall:
    //            var method = lwindow.GetNested(identifier).As<NativeFunction<object, object>>();
    //            if (NetJs.Script.IsDefined(method))
    //            {
    //                if (Array.Is(jsObject, out var array))
    //                {
    //                    NetJs.Script.Apply(null, method, array);
    //                }
    //                else
    //                {
    //                    result = method(jsObject);
    //                }
    //            }
    //            else
    //                throw new InvalidOperationException();
    //            break;
    //        case JSCallType.ConstructorCall:
    //            var prototype = lwindow.GetNested(identifier).As<TypePrototype>();
    //            result = prototype.New(jsObject.As<object[]>());
    //            break;
    //        case JSCallType.GetValue:
    //            result = lwindow.GetNested(identifier);
    //            break;
    //        case JSCallType.SetValue:
    //            lwindow.SetNested(identifier, jsObject);
    //            break;
    //    }
    //    return NetJs.Script.JSONStringify(result);
    //    //switch (_resultType)
    //    //{
    //    //    case JSCallResultType.
    //    //}
    //}

    public static extern partial void EndInvokeDotNetFromJS(
        string? id,
        bool success,
        string jsonOrError);

    public static extern partial void ReceiveByteArray(
        int id,
        byte[] data);
}
