using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    public static partial class Utility
    {
        public const int LiteRouter_Layout_SequenceNumber = 1;
        public const int LiteRouter_Page_SequenceNumber = 2;

        public const int DynamicComponent_SequenceNumber = 3;

        public const int CascadingValue_SequenceNumber = 4;

        public const int Router_View_SequenceNumber = 5;

        public const int AuthorizeRouteView_LayoutView_SequenceNumber = 5;
        public const int AuthorizeRouteView_AuthorizeRouteViewCore_SequenceNumber = 6;

        public const int AuthorizeViewCore_Authorizing_SequenceNumber = 7;
        public const int AuthorizeViewCore_Authorized_SequenceNumber = 8;
        public const int AuthorizeViewCore_NotAuthorized_SequenceNumber = 9;

        public const int LayoutView_Layout_SequenceNumber = 10;
        public const int LayoutView_Fragment_SequenceNumber = 11;

        public const int RouteView_LayoutView_SequenceNumber = 12;
        public const int RouteView_Page_SequenceNumber = 13;

        public const int Virtualize_DefaultPlaceholder_SequenceNumber = 14;
        public const int Virtualize_SpacerElementBefore_SequenceNumber = 15;
        public const int Virtualize_PlaceholderBefore_SequenceNumber = 16;
        public const int Virtualize_EmptyContent_SequenceNumber = 17;
        public const int Virtualize_Item_SequenceNumber = 18;
        public const int Virtualize_PlaceholderAfter_SequenceNumber = 19;
        public const int Virtualize_SpacerElementAfter_SequenceNumber = 20;

        
        public const int ErrorBoundary_ChildContent_SequenceNumber = 21;
        public const int ErrorBoundary_ErrorContent_SequenceNumber = 22;
        public const int ErrorBoundary_DefaultContent_SequenceNumber = 23;

        public static void FireAndForget(this Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Console.WriteLine(t.Exception);
                }
            });
        }

        public static bool Equal(object t1, object t2)
        {
            if (t1 == null && t2 == null)
                return true;
            if ((t1 == null && t2 != null) || (t1 != null && t2 == null) || !t1.Equals(t2))
            {
                return false;
            }
            return true;
        }

        public static bool ContainsKey(this object container, string propertyName)
        {
            return container.HasOwnProperty(propertyName.As<object>());
        }

        public static bool TryGetValue(this object container, string propertyName, out object obj)
        {
            if (container.HasOwnProperty(propertyName.As<object>()))
            {
                obj = container[propertyName.As<string>()];
                return true;
            }
            obj = null;
            return false;
        }

        public static bool TryGetValue(this object container, int propertyName, out object obj)
        {
            if (container.HasOwnProperty(propertyName.As<object>()))
            {
                obj = container[propertyName.As<string>()];
                return true;
            }
            obj = null;
            return false;
        }
        
        public static void SetValue(this object container, string propertyName, object obj)
        {
            container[propertyName] = obj;
        }

        public static void SetValue(this object container, int propertyName, object obj)
        {
            container[propertyName.As<string>()] = obj;
        }

        public static void Remove(this object container, int propertyName)
        {
            H5.Script.Delete(container, propertyName.As<string>());
            //container[propertyName.ToString()] = null;
        }
    }
}
