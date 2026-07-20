using H5;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    internal partial interface IRenderer
    {
        IServiceProvider Services { get; }
        IComponentActivator ComponentActivator { get; }
        void CreateText(UIText text);
        void RemoveText(UIText text);
        void UpdateText(UIText text);
        void CreateElement(UIElement element);
        void SetElementAttribute(UIElement element, string key, object value);
        void RemoveElement(UIElement element);
        void CreateRegion(UIFrame frame);
        void RemoveRegion(UIFrame frame);
        void CreateComponent(IComponent component);
        void RemoveComponent(IComponent component); 
        void CreateMarkup(UIMarkup markup);
        void UpdateMarkup(UIMarkup markup);
        void RemoveMarkup(UIMarkup markup);
        void Flush();
    }
    
    //public static partial class RendererExtension
    //{
    //    internal static void Render(this IRenderer renderer, IUIContent ui)
    //    {
    //        ui.Build();
    //        renderer.Flush();
    //    }
    //}
}
