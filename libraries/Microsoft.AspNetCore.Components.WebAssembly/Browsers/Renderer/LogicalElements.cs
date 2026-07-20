//// LogicalElements.Core.cs
//using Microsoft.AspNetCore.Components.Discovery;
//using NetJs;
//using System;
//using System.Reflection.Metadata;
//using Window;

//namespace Microsoft.AspNetCore.Components.RenderTree
//{
//    [NetJs.External]
//    public interface LogicalElement
//    {
//        object LogicalElement__DO_NOT_IMPLEMENT { get; set; }
//    }

//    public static partial class LogicalElements
//    {
//        private static readonly Symbol logicalChildrenPropname = new Symbol();
//        private static readonly Symbol logicalParentPropname = new Symbol();
//        private static readonly Symbol logicalRootDescriptorPropname = new Symbol();

//        public static LogicalElement toLogicalRootCommentElement(ComponentDescriptor descriptor)
//        {
//            var start = descriptor.start;
//            var end = descriptor.end;

//            var existingDescriptor = start[logicalRootDescriptorPropname] as ComponentDescriptor;
//            if (existingDescriptor != null)
//            {
//                if (existingDescriptor != descriptor)
//                {
//                    throw new Exception("The start component comment was already associated with another component descriptor.");
//                }
//                return (LogicalElement)start;
//            }

//            var parent = start.parentNode;
//            if (parent == null)
//            {
//                throw new Exception("Comment not connected to the DOM " + start.textContent);
//            }

//            var parentLogicalElement = toLogicalElement(parent, true);
//            var children = getLogicalChildrenArray(parentLogicalElement);

//            start[logicalParentPropname] = parentLogicalElement;
//            start[logicalRootDescriptorPropname] = descriptor;
//            var startLogicalElement = toLogicalElement(start);

//            if (end != null)
//            {
//                var rootCommentChildren = getLogicalChildrenArray(startLogicalElement);
//                int startNextChildIndex = children.NativeIndexOf(startLogicalElement) + 1;
//                LogicalElement? lastMovedChild = null;

//                while (lastMovedChild != (LogicalElement)end)
//                {
//                    var childToMove = children.Splice(startNextChildIndex, 1)?[0];
//                    if (childToMove == null)
//                    {
//                        throw new Exception("Could not find the end component comment in the parent logical node list");
//                    }
//                    childToMove[logicalParentPropname] = start;
//                    rootCommentChildren.Push(childToMove);
//                    lastMovedChild = childToMove;
//                }
//            }

//            return startLogicalElement;
//        }

//        public static LogicalElement toLogicalElement(Node element, bool allowExistingContents = false)
//        {
//            if (NetJs.Script.In(element, logicalChildrenPropname))
//            {
//                return (LogicalElement)element;
//            }

//            var childrenArray = NetJs.Script.NewArray<LogicalElement>();

//            if (element.childNodes.length > 0)
//            {
//                if (!allowExistingContents)
//                {
//                    throw new Exception("New logical elements must start empty, or allowExistingContents must be true");
//                }

//                element.childNodes.forEach(child =>
//                {
//                    if (ComponentDiscoveryEngine.isMetadataComment(child))
//                    {
//                        return;
//                    }

//                    var childLogicalElement = toLogicalElement(child, true);
//                    childLogicalElement[logicalParentPropname] = element;
//                    childrenArray.Push(childLogicalElement);
//                });
//            }

//            element[logicalChildrenPropname] = childrenArray;
//            return (LogicalElement)element;
//        }

//        public static void emptyLogicalElement(LogicalElement element)
//        {
//            var childrenArray = getLogicalChildrenArray(element);
//            while (childrenArray.Length > 0)
//            {
//                removeLogicalChild(element, 0);
//            }
//        }

//        public static LogicalElement createAndInsertLogicalContainer(LogicalElement parent, int childIndex)
//        {
//            var containerElement = Window.Document.Instance.createComment("!");
//            insertLogicalChild(containerElement, parent, childIndex);
//            return (LogicalElement)containerElement;
//        }

//        public static void insertLogicalChildBefore(Node child, LogicalElement parent, LogicalElement? before)
//        {
//            var childrenArray = getLogicalChildrenArray(parent);
//            int childIndex;
//            if (before != null)
//            {
//                childIndex = childrenArray.NativeIndexOf(before);
//                if (childIndex < 0)
//                {
//                    throw new Exception("Could not find logical element in the parent logical node list");
//                }
//            }
//            else
//            {
//                childIndex = childrenArray.Length;
//            }
//            insertLogicalChild(child, parent, childIndex);
//        }

//        public static void insertLogicalChild(Node child, LogicalElement parent, int childIndex)
//        {
//            var childAsLogicalElement = (LogicalElement)child;
//            var nodeToInsert = child;

//            if (child is Comment)
//            {
//                var existingGrandchildren = getLogicalChildrenArray(childAsLogicalElement);
//                if (existingGrandchildren != null && existingGrandchildren.Length > 0)
//                {
//                    var lastNodeToInsert = findLastDomNodeInRange(childAsLogicalElement);
//                    var range = new Window.Range();
//                    range.setStartBefore(child);
//                    range.setEndAfter(lastNodeToInsert);
//                    nodeToInsert = range.extractContents();
//                }
//            }

//            var existingLogicalParent = getLogicalParent(childAsLogicalElement);
//            if (existingLogicalParent != null)
//            {
//                var existingSiblingArray = getLogicalChildrenArray(existingLogicalParent);
//                int existingChildIndex = existingSiblingArray.NativeIndexOf(childAsLogicalElement);
//                existingSiblingArray.Splice(existingChildIndex, 1);
//                NetJs.Script.Delete(childAsLogicalElement, logicalParentPropname);
//            }

//            var newSiblings = getLogicalChildrenArray(parent);
//            if (childIndex < newSiblings.Length)
//            {
//                var nextSibling = (Node)newSiblings[childIndex];
//                nextSibling.parentNode.insertBefore(nodeToInsert, nextSibling);
//                newSiblings.Splice(childIndex, 0, childAsLogicalElement);
//            }
//            else
//            {
//                appendDomNode(nodeToInsert, parent);
//                newSiblings.Push(childAsLogicalElement);
//            }

//            childAsLogicalElement[logicalParentPropname] = parent;
//            if (!NetJs.Script.In(childAsLogicalElement, logicalChildrenPropname))
//            {
//                childAsLogicalElement[logicalChildrenPropname] = NetJs.Script.NewArray<LogicalElement>();
//            }
//        }

//        public static void removeLogicalChild(LogicalElement parent, int childIndex)
//        {
//            var childrenArray = getLogicalChildrenArray(parent);
//            var childToRemove = childrenArray.Splice(childIndex, 1);

//            if (childToRemove is Comment)
//            {
//                var grandchildrenArray = getLogicalChildrenArray(childToRemove.As<LogicalElement>());
//                if (grandchildrenArray != null)
//                {
//                    while (grandchildrenArray.Length > 0)
//                    {
//                        removeLogicalChild(childToRemove.As<LogicalElement>(), 0);
//                    }
//                }
//            }

//            var domNodeToRemove = childToRemove.As<Node>();
//            domNodeToRemove.parentNode!.removeChild(domNodeToRemove);
//        }

//        public static LogicalElement? getLogicalParent(LogicalElement element)
//        {
//            return (LogicalElement?)element[logicalParentPropname];
//        }

//        public static LogicalElement getLogicalChild(LogicalElement parent, int childIndex)
//        {
//            return getLogicalChildrenArray(parent)[childIndex];
//        }

//        public static ComponentDescriptor? getLogicalRootDescriptor(LogicalElement element)
//        {
//            return (ComponentDescriptor?)element[logicalRootDescriptorPropname];
//        }

//        public static bool isSvgElement(LogicalElement element)
//        {
//            var closestElement = getClosestDomElement(element);
//            return closestElement.namespaceURI == "http://w3.org" && closestElement["tagName"] != "foreignObject";
//        }

//        public static LogicalElement[] getLogicalChildrenArray(LogicalElement element)
//        {
//            return (LogicalElement[])element[logicalChildrenPropname]!;
//        }

//        public static LogicalElement? getLogicalNextSibling(LogicalElement element)
//        {
//            var siblings = getLogicalChildrenArray(getLogicalParent(element));
//            int siblingIndex = siblings.NativeIndexOf(element);
//            return siblings[siblingIndex + 1];
//        }

//        public static bool isLogicalElement(Node element)
//        {
//            return NetJs.Script.In(element, logicalChildrenPropname);
//        }

//        public static IEnumerable<LogicalElement> depthFirstNodeTreeTraversal(LogicalElement element)
//        {
//            var children = getLogicalChildrenArray(element);
//            for (int index = 0; index < children.Length; index++)
//            {
//                var child = children[index];
//                foreach (var descendant in depthFirstNodeTreeTraversal(child))
//                {
//                    yield return descendant;
//                }
//            }
//            yield return element;
//        }

//        public static void permuteLogicalChildren(LogicalElement parent, PermutationListEntry[] permutationList)
//        {
//            var siblings = getLogicalChildrenArray(parent);

//            permutationList.ForEach((listEntry) =>
//            {
//                listEntry.moveRangeStart = siblings[listEntry.fromSiblingIndex];
//                listEntry.moveRangeEnd = findLastDomNodeInRange(listEntry.moveRangeStart);
//            });

//            permutationList.ForEach((listEntry) =>
//            {
//                var marker = Window.Document.Instance.createComment("marker");
//                listEntry.moveToBeforeMarker = marker;
//                var insertBeforeNode = (Node)siblings[listEntry.toSiblingIndex + 1];
//                if (insertBeforeNode != null)
//                {
//                    insertBeforeNode.parentNode.insertBefore(marker, insertBeforeNode);
//                }
//                else
//                {
//                    appendDomNode(marker, parent);
//                }
//            });

//            permutationList.ForEach((listEntry) =>
//            {
//                var insertBefore = listEntry.moveToBeforeMarker;
//                var parentDomNode = insertBefore.parentNode;
//                var elementToMove = listEntry.moveRangeStart;
//                var moveEndNode = listEntry.moveRangeEnd;
//                Node? nextToMove = (Node)elementToMove;
//                while (nextToMove != null)
//                {
//                    var nextNext = nextToMove.nextSibling;
//                    parentDomNode.insertBefore(nextToMove, insertBefore);

//                    if (nextToMove == moveEndNode)
//                    {
//                        break;
//                    }
//                    else
//                    {
//                        nextToMove = nextNext;
//                    }
//                }

//                parentDomNode.removeChild(insertBefore);
//            });

//            permutationList.ForEach((listEntry) =>
//            {
//                siblings[listEntry.toSiblingIndex] = listEntry.moveRangeStart;
//            });
//        }

//        public static Element getClosestDomElement(LogicalElement logicalElement)
//        {
//            if (logicalElement is Element || logicalElement is DocumentFragment)
//            {
//                return (Element)logicalElement;
//            }
//            else if (logicalElement is Comment)
//            {
//                var commentNode = (Comment)logicalElement;
//                return (Element)commentNode.parentNode!;
//            }
//            else
//            {
//                throw new Exception("Not a valid logical element");
//            }
//        }

//        private static void appendDomNode(Node child, LogicalElement parent)
//        {
//            if (parent is Element || parent is DocumentFragment)
//            {
//                var parentNode = (Node)parent;
//                parentNode.appendChild(child);
//            }
//            else if (parent is Comment)
//            {
//                var commentNode = (Comment)parent;
//                var parentLogicalNextSibling = getLogicalNextSibling(commentNode.As<LogicalElement>()).As<Node>();
//                if (parentLogicalNextSibling != null)
//                {
//                    parentLogicalNextSibling.parentNode!.insertBefore(child, parentLogicalNextSibling);
//                }
//                else
//                {
//                    appendDomNode(child, getLogicalParent(commentNode.As<LogicalElement>())!);
//                }
//            }
//            else
//            {
//                throw new Exception("Cannot append node because the parent is not a valid logical element.");
//            }
//        }

//        private static Node findLastDomNodeInRange(LogicalElement element)
//        {
//            if (element is Element || element is DocumentFragment)
//            {
//                return (Node)element;
//            }

//            var nextSibling = getLogicalNextSibling(element);
//            if (nextSibling != null)
//            {
//                var nextSiblingNode = (Node)nextSibling;
//                return nextSiblingNode.previousSibling;
//            }
//            else
//            {
//                var logicalParent = getLogicalParent(element);
//                if (logicalParent is Element || logicalParent is DocumentFragment)
//                {
//                    var parentNode = (Node)logicalParent;
//                    return parentNode.lastChild;
//                }
//                return findLastDomNodeInRange(logicalParent);
//            }
//        }
//    }

//    [ObjectLiteral]
//    public class PermutationListEntry
//    {
//        public int fromSiblingIndex { get; set; }
//        public int toSiblingIndex { get; set; }

//        public LogicalElement? moveRangeStart { get; set; }
//        public Node? moveRangeEnd { get; set; }
//        public Node? moveToBeforeMarker { get; set; }
//    }
//}
