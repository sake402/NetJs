using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebSockets
{
    internal static partial class BrowserInterop
    {
        class WebSocketJSObject : JSObject
        {
            public WebSocketJSObject(string uri, string?[]? subProtocols, IntPtr responseStatusPtr)
            {
                this.uri = uri;
                this.subProtocols = subProtocols;
                this.responseStatusPtr = responseStatusPtr;
            }

            public string uri { get; }
            public string?[]? subProtocols { get; }
            public IntPtr responseStatusPtr { get; }
            public Window.WebSocket? webSocket { get; set; }
            public IntPtr receiveBufferPtr { get; set; }
            public int receiveBufferLen { get; set; }
            public TaskCompletionSource? receiveTask { get; set; }

            //public bool? wasCleanClose { get; set; }
            //public int? closeCode { get; set; }
            //public int? closeReason { get; set; }
        }
        [NetJs.NoJSImport]
        //public static partial JSObject WebSocketCreate(
        //string uri,
        //string?[]? subProtocols,
        //IntPtr responseStatusPtr);
        [NetJs.MemberReplace(nameof(WebSocketCreate))]
        public static JSObject WebSocketCreateImpl(
            string uri,
            string?[]? subProtocols,
            IntPtr responseStatusPtr)
        {
            return new WebSocketJSObject(uri, subProtocols, responseStatusPtr);
        }

        [NetJs.NoJSImport]
        //public static partial Task WebSocketOpen(
        //JSObject webSocket);
        [NetJs.MemberReplace(nameof(WebSocketOpen))]
        public static Task WebSocketOpenImpl(
            JSObject webSocket)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            ws.webSocket = new Window.WebSocket(ws.uri, ws.subProtocols);
            var tcs = new TaskCompletionSource();
            ws.webSocket.addEventListener("open", (mevent) =>
            {
                tcs.SetResult();
            });

            void UpdateReceiveStatus(int count, WebSocketMessageType type, bool endOfMessage)
            {
                var responseStatus = new int[3];
                responseStatus[0] = count;
                responseStatus[1] = type.As<int>();
                responseStatus[2] = endOfMessage ? 1 : 0;
                System.Runtime.InteropServices.Marshal.Copy(responseStatus, 0, ws.responseStatusPtr, responseStatus.Length);
            }

            ws.webSocket.addEventListener("message", async (mevent) =>
            {
                var evt = mevent.As<Window.WebSocket.MessageEvent>();
                if (NetJs.Script.TypeOf(evt.data).NativeEquals("string"))
                {
                    var str = evt.data.As<string>();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                    if (ws.receiveBufferPtr != 0)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ws.receiveBufferPtr, Math.Min(bytes.Length, ws.receiveBufferLen));
                        ws.receiveBufferPtr = 0;
                    }
                    UpdateReceiveStatus(bytes.Length, WebSocketMessageType.Text, true);
                    ws.receiveTask?.SetResult();
                }
                else if (NetJs.Script.Write<bool>("evt.data instanceof ArrayBuffer"))
                {
                    var arrayBuffer = evt.data.As<Window.ArrayBuffer>();
                    var uint8Array = new Window.Uint8Array(arrayBuffer);
                    var bytes = Array.from(uint8Array).As<byte[]>();
                    if (ws.receiveBufferPtr != 0)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ws.receiveBufferPtr, Math.Min(bytes.Length, ws.receiveBufferLen));
                        ws.receiveBufferPtr = 0;
                        UpdateReceiveStatus(bytes.Length, WebSocketMessageType.Binary, true);
                        ws.receiveTask?.SetResult();
                    }
                }
                else if (NetJs.Script.Write<bool>("evt.data instanceof Blob"))
                {
                    var blob = evt.data.As<Window.Blob>();
                    blob.arrayBuffer().then((arrayBuffer) =>
                    {
                        var uint8Array = new Window.Uint8Array(arrayBuffer);
                        var bytes = Array.from(uint8Array).As<byte[]>();
                        if (ws.receiveBufferPtr != 0)
                        {
                            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ws.receiveBufferPtr, Math.Min(bytes.Length, ws.receiveBufferLen));
                            ws.receiveBufferPtr = 0;
                            UpdateReceiveStatus(bytes.Length, WebSocketMessageType.Binary, true);
                            ws.receiveTask?.SetResult();
                        }
                    });
                }
                else
                {
                    throw null!;
                }
            });

            ws.webSocket.addEventListener("error", (error) =>
            {
                tcs.TrySetException(new Exception("WebSocket error"));
            });

            ws.webSocket.addEventListener("close", (mevent) =>
            {
                var closeEv = mevent.As<Window.WebSocket.CloseEvent>();
                ws.SetProperty("close_status", closeEv.code);
                ws.SetProperty("closeReason", closeEv.reason);
                ws.SetProperty("wasCleanClose", closeEv.wasClean);
                UpdateReceiveStatus(0, WebSocketMessageType.Close, true);
            });
            return tcs.Task;
        }

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(WebSocketSend))]
        public static unsafe Task? WebSocketSendImpl(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength,
            int messageType,
            bool endOfMessage)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            var type = messageType.As<WebSocketMessageType>();
            var data = (void*)bufferPtr;
            if (type == WebSocketMessageType.Text)
            {
                var str = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(bufferPtr, bufferLength);
                ws.webSocket.send(str);
                return Task.CompletedTask;

            }
            else if (type == WebSocketMessageType.Binary)
            {
                var bytes = new byte[bufferLength];
                System.Runtime.InteropServices.Marshal.Copy(bufferPtr, bytes, 0, bufferLength);
                var arrayBuffer = new Window.Uint8Array(bytes);
                ws.webSocket?.send(arrayBuffer);
                return Task.CompletedTask;
            }
            else
            {
                throw new NotSupportedException($"Unsupported message type: {type}");
            }
        }

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(WebSocketReceive))]
        public static Task? WebSocketReceiveImpl(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            ws.receiveBufferPtr = bufferPtr;
            ws.receiveBufferLen = bufferLength;
            ws.receiveTask = new TaskCompletionSource();
            return ws.receiveTask.Task;
        }

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(WebSocketClose))]
        public static Task? WebSocketCloseImpl(
            JSObject webSocket,
            int code,
            string? reason,
            bool waitForCloseReceived)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            ws.webSocket?.close(code, reason);
            return Task.CompletedTask;
        }


        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(WebSocketAbort))]
        public static void WebSocketAbortImpl(
            JSObject webSocket)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            ws.webSocket?.close();
        }

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(WebSocketGetState))]
        public static int WebSocketGetStateImpl(
            JSObject webSocket)
        {
            var ws = webSocket.As<WebSocketJSObject>();
            if (ws.webSocket == null)
                return Window.WebSocket.CONNECTING;
            return ws.webSocket.readyState;
        }
    }
}
