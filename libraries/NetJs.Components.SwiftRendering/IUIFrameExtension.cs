using H5;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;
using static H5.Core.dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace BlazorJs.Core
{
    public static partial class IUIFrameExtension
    {
        //internal static void Render(this IUIContent reference)
        //{
        //    var renderer = reference.State.Renderer;
        //    if (reference is ComponentBase component)
        //    {
        //        component.WithErrorHandling((icomponent) =>
        //        {
        //            renderer.Render(icomponent);
        //        }, ComponentLifeCycle.OnRendering);
        //    }
        //    else
        //    {
        //        renderer.Render(reference);
        //    }
        //}

        //internal static async Task RenderOnAsyncCompletion(this IUIContent reference, Task task, ComponentLifeCycle taskLifeCycle = ComponentLifeCycle.Unknown)
        //{
        //    if (!task.IsCompleted)
        //    {
        //        try
        //        {
        //            await task;
        //        }
        //        catch (Exception e)
        //        {
        //            if (taskLifeCycle != ComponentLifeCycle.Unknown && reference is ComponentBase component)
        //            {
        //                bool handled = component.HandleError(e, taskLifeCycle);
        //                if (!handled)
        //                    throw;
        //            }
        //        }
        //        reference.Render();
        //    }
        //}

        internal static IUIContent GetBefore(this IUIContent reference)
        {
            var siblings = reference.State.ParentFrame?.State.Children;
            if (siblings == null)
                return null;
            if (!siblings.Any(s => s != null && s.State.Id < reference.State.Id))
                return null;
            var before = siblings.Where(s => s != null && s.State.Id < reference.State.Id).MaxBy(s => s.State.Id);
            //IUIContent before = null;
            //if (siblings != null)
            //{
            //    foreach (var sib in siblings)
            //    {
            //        if (sib != null && sib.State.Id < reference.State.Id)
            //        {
            //            if (before == null || sib.State.Id > before.State.Id)
            //                before = sib;
            //        }
            //    }
            //}
            return before;
            //var refNode = reference.Parent.Chlidren.Find(reference)!;
            //return refNode.Previous?.Value;
        }

        internal static IUIContent GetAfter(this IUIContent reference)
        {
            var siblings = reference.State.ParentFrame?.State.Children;
            if (siblings == null)
                return null;
            if (!siblings.Any(s => s != null && s.State.Id > reference.State.Id))
                return null;
            var after = siblings.Where(s => s != null && s.State.Id > reference.State.Id).MinBy(s => s.State.Id);
            //IUIContent after = null;
            //if (siblings != null)
            //{
            //    foreach (var sib in siblings)
            //    {
            //        if (sib != null && sib.State.Id > reference.State.Id)
            //        {
            //            if (after == null || sib.State.Id < after.State.Id)
            //                after = sib;
            //        }
            //    }
            //}
            return after;
            //var refNode = reference.Parent.Chlidren.Find(reference)!;
            //return refNode.Next?.Value;
        }

        //public static FragmentConstructor[] Fragments(this IUIFrame parent, params FragmentConstructor[] childFragments)
        //{
        //    return childFragments;
        //}

        public static HTMLElement Element(this IUIFrame frame, string tag,
            ElementAttributeBuilder attributeBuilder = null,
            RenderFragment contentBuilder = null,
            object key = null,
            [CallerLineNumber] int sequenceNumber = 0)
        {
            do
            {
                var uiElement = frame.State.GetOrCreate(sequenceNumber, (id) =>
                {
                    var muiElement = new UIElement(frame.State.Renderer, frame, id, tag, key);
                    return muiElement;
                }, key);
                //if we change the element tag on the same sequenceNumber, dispose the current state and element, rebuild
                if (uiElement.Tag != tag)
                {
                    uiElement.Dispose();
                    continue;
                }
                uiElement.AttributeBuilder = attributeBuilder;
                uiElement.ContentBuilder = contentBuilder;
                //uiElement.TearDown = tearDown;
                uiElement.Build(key);
                return uiElement.Elements[0].As<HTMLElement>();
            } while (true);
        }

        public static Node Text(
            this IUIFrame frame,
            object text,
            object key = null,
            [CallerLineNumber] int sequenceNumber = 0)
        {
            var uiText = frame.State.GetOrCreate(sequenceNumber, (id) =>
            {
                var muiText = new UIText(frame.State.Renderer, frame, id, key);
                return muiText;
            }, key);
            uiText.Text = text;
            uiText.Build(key);
            return uiText.Elements[0].As<Node>();
        }

        public static Node Text(this IUIFrame frame, Func<object> text, object key = null, [CallerLineNumber] int sequenceNumber = 0)
        {
            var uiText = frame.State.GetOrCreate(sequenceNumber, (id) =>
            {
                var muiText = new UIText(frame.State.Renderer, frame, id, key);
                return muiText;
            }, key);
            uiText.Text = text;
            uiText.Build(key);
            return uiText.Elements[0].As<Node>();
        }

        public static void Fragment(this IUIFrame frame, RenderFragment view)
        {
            view?.Invoke(frame);
        }

        public static void Frame(this IUIFrame frame, RenderFragment view, object key = null, [CallerLineNumber] int sequenceNumber = 0)
        {
            var uiFrame = frame.State.GetOrCreate(sequenceNumber, (id) =>
            {
                var muiFrame = new UIFrame(frame, view, key, id);
                return muiFrame;
            }, key);
            uiFrame.ContentBuilder = view;
            uiFrame.Build(key);
            //return uiFrame;
        }

        public static Node[] Markup(this IUIFrame frame, MarkupString view, object key = null, [CallerLineNumber] int sequenceNumber = 0)
        {
            var uiMarkup = frame.State.GetOrCreate(sequenceNumber, (id) =>
            {
                var muiMarkup = new UIMarkup(frame.State.Renderer, frame, id, view.Html, key);
                return muiMarkup;
            }, key);
            uiMarkup.Markup = view.Html;
            uiMarkup.Build(key);
            return uiMarkup.Elements.As<Node[]>();
        }

        public static void Content<T>(this IUIFrame frame, T content, object key = null, [CallerLineNumber] int sequenceNumber = 0)
        {
            if (typeof(T) == typeof(RenderFragment))
            {
                //if (key == null)
                //{
                //    Fragment(frame, (RenderFragment)(object)content);
                //    return null;
                //}
                //else
                //{
                Frame(frame, (RenderFragment)(object)content, key: key, sequenceNumber: sequenceNumber);
                //}
            }
            else if (typeof(T) == typeof(MarkupString))
            {
                Markup(frame, (MarkupString)(object)content, key: key, sequenceNumber: sequenceNumber);
            }
            else
            {
                Text(frame, content, key: key, sequenceNumber: sequenceNumber);
            }
        }

        public static IComponent Component(this IUIFrame frame, Type componentType, Action<IComponent> attributeBuilder, object key = null, [CallerLineNumber] int sequenceNumber = 0)
        {
            do
            {
                var state = frame.State.GetOrCreate(sequenceNumber, (id) =>
                {
                    var renderer = (BrowserNativeRenderer)frame.State.Renderer;
                    var mcomponent = renderer.ComponentActivator.CreateInstance(componentType);
                    var mstate = new UIFrameState(renderer, frame, id, key);
                    mstate.Component = mcomponent;
                    mstate.ComponentType = componentType;
                    mcomponent.Attach(new RenderHandle(renderer, id));
                    renderer.Register(id, mstate);
                    if (mcomponent is ComponentBase mcb)
                    {
                        mcb.WithErrorHandling((icomponent) =>
                        {
                            icomponent.InjectServices(renderer.Services);
                            icomponent.CascadeParameters();
                        }, ComponentLifeCycle.OnInjectingService);
                    }
                    renderer.CreateComponent(mcomponent);
                    //return mcomponent;
                    return mstate;
                }, key);
                //if we change the component type on the same sequenceNumber, dispose the current state and component, rebuild
                if (state.ComponentType != componentType)
                {
                    state.Dispose();
                    continue;
                }
                attributeBuilder?.Invoke(state.Component);
                Task task = state.Component.SetParametersAsync(default);
                return state.Component;
            } while (true);
        }

        public static T Component<T>(this IUIFrame frame, Action<T> attributeBuilder, object key = null, [CallerLineNumber] int sequenceNumber = 0)
            where T : IComponent
        {
            return (T)frame.Component(typeof(T), attributeBuilder != null ? ab => attributeBuilder((T)ab) : (Action<IComponent>)null, key, sequenceNumber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T InferType<TComponent, T>(this IUIFrame context, T property, T value, string expression)
        {
            return value;
        }
    }
}
