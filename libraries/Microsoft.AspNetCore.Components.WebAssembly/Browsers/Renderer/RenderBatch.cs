//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.

//namespace Microsoft.AspNetCore.Components.RenderTree
//{
//    public interface IRenderBatch
//    {
//        ArrayRange<RenderTreeDiff> UpdatedComponents();
//        ArrayRange<RenderTreeFrame> ReferenceFrames();
//        ArrayRange<int> DisposedComponentIds();
//        ArrayRange<int> DisposedEventHandlerIds();

//        RenderTreeDiff UpdatedComponentsEntry(ArrayValues<RenderTreeDiff> values, int index);
//        RenderTreeFrame ReferenceFramesEntry(ArrayValues<RenderTreeFrame> values, int index);
//        int DisposedComponentIdsEntry(ArrayValues<int> values, int index);
//        int DisposedEventHandlerIdsEntry(ArrayValues<int> values, int index);

//        IRenderTreeDiffReader DiffReader { get; }
//        IRenderTreeEditReader EditReader { get; }
//        IRenderTreeFrameReader FrameReader { get; }
//        IArrayRangeReader ArrayRangeReader { get; }
//        IArrayBuilderSegmentReader ArrayBuilderSegmentReader { get; }
//    }

//    public interface IArrayRangeReader
//    {
//        int Count<T>(ArrayRange<T> arrayRange);
//        ArrayValues<T> Values<T>(ArrayRange<T> arrayRange);
//    }

//    public interface IArrayBuilderSegmentReader
//    {
//        int Offset<T>(ArrayBuilderSegment<T> arrayBuilderSegment);
//        int Count<T>(ArrayBuilderSegment<T> arrayBuilderSegment);
//        ArrayValues<T> Values<T>(ArrayBuilderSegment<T> arrayBuilderSegment);
//    }

//    public interface IRenderTreeDiffReader
//    {
//        int ComponentId(RenderTreeDiff diff);
//        ArrayBuilderSegment<RenderTreeEdit> Edits(RenderTreeDiff diff);
//        RenderTreeEdit EditsEntry(ArrayValues<RenderTreeEdit> values, int index);
//    }

//    public interface IRenderTreeEditReader
//    {
//        EditType EditType(RenderTreeEdit edit);
//        int SiblingIndex(RenderTreeEdit edit);
//        int NewTreeIndex(RenderTreeEdit edit);
//        int MoveToSiblingIndex(RenderTreeEdit edit);
//        string? RemovedAttributeName(RenderTreeEdit edit);
//    }

//    public interface IRenderTreeFrameReader
//    {
//        FrameType FrameType(RenderTreeFrame frame);
//        int SubtreeLength(RenderTreeFrame frame);
//        string? ElementReferenceCaptureId(RenderTreeFrame frame);
//        int ComponentId(RenderTreeFrame frame);
//        string? ElementName(RenderTreeFrame frame);
//        string? TextContent(RenderTreeFrame frame);
//        string MarkupContent(RenderTreeFrame frame);
//        string? AttributeName(RenderTreeFrame frame);
//        string? AttributeValue(RenderTreeFrame frame);
//        int AttributeEventHandlerId(RenderTreeFrame frame);
//    }

//    // Replaces: export interface ArrayRange<T> { ... }
//    public readonly struct ArrayRange<T>
//    {
//        private readonly object? _doNotImplement;
//    }

//    // Replaces: export interface ArrayBuilderSegment<T> { ... }
//    public readonly struct ArrayBuilderSegment<T>
//    {
//        private readonly object? _doNotImplement;
//    }

//    // Replaces: export interface ArrayValues<T> { ... }
//    public readonly struct ArrayValues<T>
//    {
//        private readonly object? _doNotImplement;
//    }

//    // Replaces: export interface RenderTreeDiff { ... }
//    public readonly struct RenderTreeDiff
//    {
//        private readonly object? _doNotImplement;
//    }

//    // Replaces: export interface RenderTreeFrame { ... }
//    public readonly struct RenderTreeFrame
//    {
//        private readonly object? _doNotImplement;
//    }

//    // Replaces: export interface RenderTreeEdit { ... }
//    public readonly struct RenderTreeEdit
//    {
//        private readonly object? _doNotImplement;
//    }

//    public enum EditType
//    {
//        PrependFrame = 1,
//        RemoveFrame = 2,
//        SetAttribute = 3,
//        RemoveAttribute = 4,
//        UpdateText = 5,
//        StepIn = 6,
//        StepOut = 7,
//        UpdateMarkup = 8,
//        PermutationListEntry = 9,
//        PermutationListEnd = 10,
//    }

//    public enum FrameType
//    {
//        Element = 1,
//        Text = 2,
//        Attribute = 3,
//        Component = 4,
//        Region = 5,
//        ElementReferenceCapture = 6,
//        Markup = 8,
//        NamedEvent = 10,
//    }
//}
