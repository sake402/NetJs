namespace Window
{
    [NetJs.External]
    public enum NodeType
    {
        ElementNode = 1,
        AttributeNode = 2,
        TextNode = 3,
        CommentNode = 8,
        DocumentNode = 9,
        DocumentFragmentNode = 11
    }
}