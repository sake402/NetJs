namespace Window
{
    [NetJs.External]
    public class Range
    {
        public extern Node startContainer { get; }
        public extern int startOffset { get; }
        public extern Node endContainer { get; }
        public extern int endOffset { get; }
        public extern bool collapsed { get; }
        public extern Node commonAncestorContainer { get; }
        public extern Range();
        public extern void setStartBefore(Node referenceNode);
        public extern void setEndAfter(Node referenceNode);
        public extern DocumentFragment extractContents();
        public extern void setStart(Node startNode, int startOffset);
        public extern void setEnd(Node endNode, int endOffset);
        public extern void setStartAfter(Node referenceNode);
        public extern void setEndBefore(Node referenceNode);
        public extern void deleteContents();
        public extern DocumentFragment cloneContents();
        public extern void insertNode(Node newNode);
        public extern void surroundContents(Node newParent);
        public extern void detach();
    }
}