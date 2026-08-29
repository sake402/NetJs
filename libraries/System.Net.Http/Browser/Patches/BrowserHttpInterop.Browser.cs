// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NetJs;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Window;

namespace System.Net.Http
{
    internal static partial class BrowserHttpInterop
    {
        class RequestSession : IDisposable
        {
            public Window.Promise<Response>? responsePromise;
            public Response? response;
            public string[]? responseHeaderNames;
            public string[]? responseHeaderValues;
            public Window.ArrayBuffer? responseBuffer;
            public ReadableStreamDefaultReader<byte>? streamReader;
            public GeneratorIteratorResult<Uint8Array>? currentStreamReaderChunk;
            public int currentBufferOffset;
            public bool IsDisposed { get; }
            public void Dispose()
            {
            }
        }

        [NetJs.NoJSImport]
        public static partial bool SupportsStreamingRequestImpl() => true;

        [NetJs.NoJSImport]
        public static partial bool SupportsStreamingResponseImpl() => true;

        [NetJs.NoJSImport]
        public static partial JSObject CreateController()
        {
            var session = new RequestSession();
            return session.As<JSObject>();
            //var js = NetJs.Script.Write<JSObject>("INTERNAL.http_wasm_create_controller()");
            //js["IsDisposed"] = false.As<object>();
            //js["_isDisposed"] = false.As<object>();
            //js["Dispose"] = () =>
            //{
            //    js["IsDisposed"] = true.As<object>();
            //    js["_isDisposed"] = true.As<object>();
            //};
            //return js;
        }

        [NetJs.NoJSImport]
        public static partial void Abort(JSObject httpController)
        {
            var session = httpController.As<RequestSession>();
            session.Dispose();
        }

        [NetJs.NoJSImport]
        public static extern partial Task TransformStreamWrite(
            JSObject httpController,
            IntPtr bufferPtr,
            int bufferLength);

        [NetJs.NoJSImport]
        public static extern partial Task TransformStreamClose(
            JSObject httpController);

        [NetJs.NoJSImport]
        private static partial string[] _GetResponseHeaderNames(
            JSObject httpController)
        {
            var session = httpController.As<RequestSession>();
            return session.responseHeaderNames!;
        }

        [NetJs.NoJSImport]
        private static partial string[] _GetResponseHeaderValues(
            JSObject httpController)
        {
            var session = httpController.As<RequestSession>();
            return session.responseHeaderValues!;
        }

        [NetJs.NoJSImport]
        public static partial int GetResponseStatus(
            JSObject httpController)
        {
            var session = httpController.As<RequestSession>();
            return session.response!.status;
        }

        [NetJs.NoJSImport]
        public static partial string GetResponseType(
            JSObject httpController)
        {
            var session = httpController.As<RequestSession>();
            return session.response!.type!;
        }

        static unsafe DataView? ToDataView(in MemoryHandle pinBuffer)
        {
            RefOrPointer<byte>? mref = NetJs.Script.Ref<byte>(pinBuffer.Pointer);
            if (mref != null)
            {
                return mref.GetDataView();
            }
            return null;
        }
        
        public static async Task DoFetch(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            object?[] optionValues,
            MemoryHandle pinBuffer,
            int bodyLength)
        {
            var session = httpController.As<RequestSession>();
            var header = new Window.Headers();
            for (int i = 0; i < headerNames.Length; i++)
            {
                unchecked
                {
                    header.append(headerNames[i], headerValues[i]);
                }
            }
            var method = "GET";
            if (optionValues != null)
            {
                var methodIndex = optionNames.ArrayIndexOf("method");
                unchecked
                {
                    if (methodIndex >= 0)
                        method = optionValues[methodIndex].As<string>();
                }
            }
            var promise = Window.Window.fetch(uri, new FetchOption
            {
                method = method,
                headers = header,
                body = ToDataView(pinBuffer)
            });
            var response = await promise;
            var responseHeaderNames = NetJs.Script.NewArray<string>();
            var responseHeaderValues = NetJs.Script.NewArray<string>();
            response!.headers.entries().forEach((k, v) =>
            {
                unchecked
                {
                    responseHeaderNames.Push(k[0]);
                    responseHeaderValues.Push(k[1]);
                }
            });
            session.responsePromise = promise;
            session.response = response;
            session.responseHeaderNames = responseHeaderNames;
            session.responseHeaderValues = responseHeaderValues;
        }

        [NetJs.NoJSImport]
        public static partial Task Fetch(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            object?[] optionValues)
        {
            return DoFetch(httpController, uri, headerNames, headerValues, optionNames, optionValues, default, 0);
        }

        [NetJs.NoJSImport]
        public static partial Task FetchStream(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            object?[] optionValues)
        {
            return DoFetch(httpController, uri, headerNames, headerValues, optionNames, optionValues, default, 0);
        }
        [NetJs.MemberReplace(nameof(FetchBytes) + "(JSObject, string, string[], string[], string[], object?[], MemoryHandle, int)")]
        public static Task FetchBytesImpl(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            object?[] optionValues,
            MemoryHandle pinBuffer,
            int bodyLength)
        {
            return DoFetch(httpController, uri, headerNames, headerValues, optionNames, optionValues, pinBuffer, bodyLength);
        }
        private static extern partial Task FetchBytes(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            object?[] optionValues,
            IntPtr bodyPtr,
            int bodyLength);

        [NetJs.MemberReplace(nameof(GetStreamedResponseBytesUnsafe))]
        public static async Task<int> GetStreamedResponseBytesUnsafeImpl(JSObject jsController, Memory<byte> buffer, MemoryHandle handle)
        {
            var session = jsController.As<RequestSession>();
            await session.responsePromise!;
            var streamReader = session.streamReader ?? await session.response!.body.getReader();
            session.streamReader = streamReader;
            var chunk = session.currentStreamReaderChunk ?? await streamReader.read();
            session.currentStreamReaderChunk = chunk;
            if (chunk.Done)
            {
                return 0;
            }
            var newLen = chunk.Value.byteLength - session.currentBufferOffset;
            var lenToRead = Math.Min(newLen, buffer.Length);
            var uint8Array = chunk.Value.subarray(session.currentBufferOffset, session.currentBufferOffset + lenToRead);
            var span = buffer.Span;
            for (int i = 0; i < lenToRead; i++)
            {
                span[i] = uint8Array[i];
            }
            session.currentBufferOffset += lenToRead;
            return lenToRead;
        }

        public static extern partial Task<int> GetStreamedResponseBytes(
            JSObject fetchResponse,
            IntPtr bufferPtr,
            int bufferLength);

        [NetJs.NoJSImport]
        public static partial async Task<int> GetResponseLength(
            JSObject fetchResponse)
        {
            var session = fetchResponse.As<RequestSession>();
            var buffer = await session.response!.arrayBuffer();
            session.responseBuffer = buffer;
            session.currentBufferOffset = 0;
            return buffer.byteLength;
        }

        public static partial int GetResponseBytes(
            JSObject fetchResponse,
            Span<byte> buffer)
        {
            var session = fetchResponse.As<RequestSession>();
            if (session.currentBufferOffset == session.responseBuffer!.byteLength)
                return 0;
            var uint8Array = new Uint8Array(session.responseBuffer, session.currentBufferOffset);
            var l = Math.Min(buffer.Length, uint8Array.length);
            for (int i = 0; i < l; i++)
            {
                buffer[i] = uint8Array[i];
            }
            session.currentBufferOffset += l;
            return l;
        }
    }
}
