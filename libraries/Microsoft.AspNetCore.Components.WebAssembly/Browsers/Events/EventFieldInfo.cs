//using System;
//using Window;

//namespace Microsoft.AspNetCore.Components.RenderTree
//{
//    public class EventFieldInfo
//    {
//        public int componentId;
//        public object fieldValue; // Maps loosely to JS (string | boolean)

//        public EventFieldInfo(int componentId, object fieldValue)
//        {
//            this.componentId = componentId;
//            this.fieldValue = fieldValue;
//        }

//        public static EventFieldInfo? fromEvent(int componentId, Event @event)
//        {
//            var elem = @event.target;
//            if (elem is Element)
//            {
//                var fieldData = getFormFieldData((Element)elem);
//                if (fieldData != null)
//                {
//                    return new EventFieldInfo(componentId, fieldData.value);
//                }
//            }
            
//            return null;
//        }

//        public static FormFieldData? getFormFieldData(Element elem)
//        {
//            if (elem is HTMLInputElement)
//            {
//                var inputElem = (HTMLInputElement)elem;
//                return (inputElem.type != null && inputElem.type.NativeToLower() == "checkbox")
//                    ? new FormFieldData { value = inputElem.@checked }
//                    : new FormFieldData { value = inputElem.value };
//            }

//            if (elem is HTMLSelectElement)
//            {
//                var selectElem = (HTMLSelectElement)elem;
//                return new FormFieldData { value = selectElem.value };
//            }

//            if (elem is HTMLTextAreaElement)
//            {
//                var textAreaElem = (HTMLTextAreaElement)elem;
//                return new FormFieldData { value = textAreaElem.value };
//            }

//            return null;
//        }
//    }

//    [NetJs.ObjectLiteral]
//    public class FormFieldData
//    {
//        public object? value;
//    }
//}
