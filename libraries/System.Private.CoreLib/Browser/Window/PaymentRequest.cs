using System;

namespace Window
{
    [NetJs.External]
    public class PaymentRequest
    {
        public extern PaymentRequest(object methodData, object details, object? options = null);
        public extern Promise<PaymentResponse> show();
        public extern Promise<object> abort();
    }

    [NetJs.External]
    public class PaymentResponse
    {
        public extern string? methodName { get; }
        public extern object? details { get; }
        public extern Promise<object> complete(string result = "success");
    }
}