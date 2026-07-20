//using NetJs;
//using System;
//using System.Collections.Generic;
//using Window;

//namespace Microsoft.AspNetCore.Components.Discovery
//{
//    public abstract record ComponentMarker(string type, string? prerenderId = null, MarkerKey? key = null);

//    public record MarkerKey(string locationHash, string? formattedComponentKey = null);

//    public record ServerComponentMarker(
//        int sequence,
//        string descriptor,
//        string? prerenderId = null,
//        MarkerKey? key = null
//    ) : ComponentMarker("server", prerenderId, key);

//    public record WebAssemblyComponentMarker(
//        string typeName,
//        string assembly,
//        string parameterDefinitions,
//        string parameterValues,
//        string? prerenderId = null,
//        MarkerKey? key = null
//    ) : ComponentMarker("webassembly", prerenderId, key);

//    public record AutoComponentMarker(
//        int sequence,
//        string descriptor,
//        string typeName,
//        string assembly,
//        string parameterDefinitions,
//        string parameterValues,
//        string? prerenderId = null,
//        MarkerKey? key = null
//    ) : ComponentMarker("auto", prerenderId, key);

//    public record ComponentEndMarker(string prerenderId);

//    public record WebAssemblyServerOptions(
//        string environmentName,
//        Dictionary<string, string> environmentVariables
//    );

//    public class RawComponentPayload
//    {
//        public string type { get; set; } = string.Empty;
//        public string? prerenderId { get; set; }
//        public MarkerKey? key { get; set; }
//        public int? sequence { get; set; }
//        public string? descriptor { get; set; }
//        public string? typeName { get; set; }
//        public string? assembly { get; set; }
//        public string? parameterDefinitions { get; set; }
//        public string? parameterValues { get; set; }
//    }

//    public abstract class ComponentDescriptor
//    {
//        public int uniqueId { get; set; }
//        public string type { get; }
//        public string? prerenderId { get; }
//        public MarkerKey? key { get; }
//        public Node start { get; }
//        public Node? end { get; set; }

//        protected ComponentDescriptor(string type, Node start, Node? end, string? prerenderId, MarkerKey? key)
//        {
//            this.type = type;
//            this.start = start;
//            this.end = end;
//            this.prerenderId = prerenderId;
//            this.key = key;
//        }
//    }

//    public class ServerComponentDescriptor : ComponentDescriptor
//    {
//        public int sequence { get; set; }
//        public string descriptor { get; set; }

//        public ServerComponentDescriptor(ServerComponentMarker marker, Node start, Node? end)
//            : base("server", start, end, marker.prerenderId, marker.key)
//        {
//            this.sequence = marker.sequence;
//            this.descriptor = marker.descriptor;
//        }
//    }

//    public class WebAssemblyComponentDescriptor : ComponentDescriptor
//    {
//        public string typeName { get; set; }
//        public string assembly { get; set; }
//        public string parameterDefinitions { get; set; }
//        public string parameterValues { get; set; }

//        public WebAssemblyComponentDescriptor(WebAssemblyComponentMarker marker, Node start, Node? end)
//            : base("webassembly", start, end, marker.prerenderId, marker.key)
//        {
//            this.typeName = marker.typeName;
//            this.assembly = marker.assembly;
//            this.parameterDefinitions = marker.parameterDefinitions;
//            this.parameterValues = marker.parameterValues;
//        }
//    }

//    public class AutoComponentDescriptor : ComponentDescriptor
//    {
//        public int sequence { get; set; }
//        public string descriptor { get; set; }
//        public string typeName { get; set; }
//        public string assembly { get; set; }
//        public string parameterDefinitions { get; set; }
//        public string parameterValues { get; set; }

//        public AutoComponentDescriptor(AutoComponentMarker marker, Node start, Node? end)
//            : base("auto", start, end, marker.prerenderId, marker.key)
//        {
//            this.sequence = marker.sequence;
//            this.descriptor = marker.descriptor;
//            this.typeName = marker.typeName;
//            this.assembly = marker.assembly;
//            this.parameterDefinitions = marker.parameterDefinitions;
//            this.parameterValues = marker.parameterValues;
//        }
//    }

//    public static partial class ComponentDiscoveryEngine
//    {
//        private static int nextUniqueDescriptorId = 0;

//        private static readonly RegExp blazorServerStateCommentRegularExpression = new(@"^\s*Blazor-Server-Component-State:(?<state>[a-zA-Z0-9+/=]+)$");
//        private static readonly RegExp blazorWebAssemblyStateCommentRegularExpression = new(@"^\s*Blazor-WebAssembly-Component-State:(?<state>[a-zA-Z0-9+/=]+)$");
//        private static readonly RegExp blazorWebInitializerCommentRegularExpression = new(@"^\s*Blazor-Web-Initializers:(?<initializers>[a-zA-Z0-9+/=]+)$");
//        private static readonly RegExp blazorWebAssemblyOptionsCommentRegularExpression = new(@"^\s*Blazor-WebAssembly:[^{]*(?<options>.*)$");
//        private static readonly RegExp blazorCommentRegularExpression = new(@"^\s*Blazor:[^{]*(?<descriptor>.*)$");

//        public static bool isMetadataComment(Node node)
//        {
//            if (node.nodeType != NodeType.CommentNode) return false;
//            string content = (node.textContent ?? string.Empty).Trim();

//            return content.StartsWith("Blazor-Server-Component-State:", StringComparison.Ordinal) ||
//                   content.StartsWith("Blazor-WebAssembly-Component-State:", StringComparison.Ordinal) ||
//                   content.StartsWith("Blazor-Web-Initializers:", StringComparison.Ordinal) ||
//                   content.StartsWith("Blazor-WebAssembly:", StringComparison.Ordinal);
//        }

//        public static List<ComponentDescriptor> discoverComponents(Node root, string type)
//        {
//            return type switch
//            {
//                "webassembly" => discoverWebAssemblyComponents(root),
//                "server" => discoverServerComponents(root),
//                "auto" => discoverAutoComponents(root),
//                _ => new List<ComponentDescriptor>()
//            };
//        }

//        public static string? discoverServerPersistedState(Node node) => discoverBlazorComment(node, blazorServerStateCommentRegularExpression);
//        public static string? discoverWebAssemblyPersistedState(Node node) => discoverBlazorComment(node, blazorWebAssemblyStateCommentRegularExpression);
//        public static string? discoverWebInitializers(Node node) => discoverBlazorComment(node, blazorWebInitializerCommentRegularExpression, "initializers");

//        public static WebAssemblyServerOptions? discoverWebAssemblyOptions(Node root)
//        {
//            string? optionsJson = discoverBlazorComment(root, blazorWebAssemblyOptionsCommentRegularExpression, "options");
//            if (string.IsNullOrEmpty(optionsJson)) return null;
//            return NetJs.Script.JSONParse<WebAssemblyServerOptions>(optionsJson!);
//        }

//        private static string? discoverBlazorComment(Node node, RegExp comment, string captureName = "state")
//        {
//            if (node.nodeType == NodeType.CommentNode)
//            {
//                string content = node.textContent ?? string.Empty;
//                var parsedState = comment.Exec(content);
//                if (parsedState != null)
//                {
//                    string value = parsedState.Groups[captureName];
//                    node.parentNode?.removeChild(node);
//                    return value;
//                }
//                return null;
//            }

//            if (!node.hasChildNodes()) return null;

//            var nodes = node.childNodes;
//            for (int index = 0; index < nodes.length; index++)
//            {
//                string? result = discoverBlazorComment(nodes[index], comment, captureName);
//                if (result != null) return result;
//            }
//            return null;
//        }

//        private static List<ComponentDescriptor> discoverServerComponents(Node root)
//        {
//            var componentComments = resolveComponentComments(root, "server");
//            componentComments.Sort((a, b) =>
//            {
//                int seqA = a is ServerComponentDescriptor sA ? sA.sequence : (a is AutoComponentDescriptor aA ? aA.sequence : 0);
//                int seqB = b is ServerComponentDescriptor sB ? sB.sequence : (b is AutoComponentDescriptor aB ? aB.sequence : 0);
//                return seqA.CompareTo(seqB);
//            });
//            return componentComments;
//        }

//        private static List<ComponentDescriptor> discoverWebAssemblyComponents(Node node) => resolveComponentComments(node, "webassembly");
//        private static List<ComponentDescriptor> discoverAutoComponents(Node node) => resolveComponentComments(node, "auto");

//        private static List<ComponentDescriptor> resolveComponentComments(Node node, string type)
//        {
//            var result = new List<ComponentDescriptor>();
//            var childNodeIterator = new ComponentCommentIterator(node.childNodes);

//            while (childNodeIterator.next() && childNodeIterator.currentElement != null)
//            {
//                var componentComment = getComponentComment(childNodeIterator, type);
//                if (componentComment != null)
//                {
//                    result.Add(componentComment);
//                }
//                else if (childNodeIterator.currentElement.hasChildNodes())
//                {
//                    var childResults = resolveComponentComments(childNodeIterator.currentElement, type);
//                    for (int j = 0; j < childResults.Count; j++)
//                    {
//                        result.Add(childResults[j]);
//                    }
//                }
//            }
//            return result;
//        }

//        private static ComponentDescriptor? getComponentComment(ComponentCommentIterator commentNodeIterator, string type)
//        {
//            var candidateStart = commentNodeIterator.currentElement;
//            if (candidateStart == null || candidateStart.nodeType != NodeType.CommentNode) return null;

//            if (candidateStart.textContent != null)
//            {
//                var definition = blazorCommentRegularExpression.Exec(candidateStart.textContent);
//                if (definition == null) return null;

//                string json = definition.Groups["descriptor"];
//                if (string.IsNullOrEmpty(json)) return null;

//                assertNotDirectlyOnDocument(candidateStart);
//                try
//                {
//                    var componentComment = NetJs.Script.JSONParse<RawComponentPayload>(json);
//                    if (componentComment == null || (componentComment.type != "server" && componentComment.type != "webassembly" && componentComment.type != "auto"))
//                    {
//                        throw new Exception($"Invalid component type '{componentComment?.type}'.");
//                    }

//                    var candidateEnd = getComponentEndComment(componentComment, candidateStart, commentNodeIterator);
//                    if (type != componentComment.type) return null;

//                    return componentComment.type switch
//                    {
//                        "webassembly" => createWebAssemblyComponentComment(componentComment, candidateStart, candidateEnd),
//                        "server" => createServerComponentComment(componentComment, candidateStart, candidateEnd),
//                        "auto" => createAutoComponentComment(componentComment, candidateStart, candidateEnd),
//                        _ => null
//                    };
//                }
//                catch (Exception)
//                {
//                    throw new Exception($"Found malformed component comment at {candidateStart.textContent}");
//                }
//            }
//            return null;
//        }

//        private static Node? getComponentEndComment(RawComponentPayload payload, Node start, ComponentCommentIterator iterator)
//        {
//            string? prerenderId = payload.prerenderId;
//            if (string.IsNullOrEmpty(prerenderId)) return null;

//            while (iterator.next() && iterator.currentElement != null)
//            {
//                var node = iterator.currentElement;
//                if (node.nodeType != NodeType.CommentNode) continue;
//                if (node.textContent == null) continue;

//                var definition = blazorCommentRegularExpression.Exec(node.textContent);
//                if (definition == null) continue;

//                string json = definition[1];
//                if (string.IsNullOrEmpty(json)) continue;

//                validateEndComponentPayload(json, prerenderId!);
//                return node;
//            }
//            throw new Exception($"Could not find an end component comment for '{start}'.");
//        }

//        private static ServerComponentDescriptor createServerComponentComment(RawComponentPayload payload, Node start, Node? end)
//        {
//            validateServerComponentPayload(payload);
//            var marker = new ServerComponentMarker(payload.sequence!.Value, payload.descriptor!, payload.prerenderId, payload.key);
//            return new ServerComponentDescriptor(marker, start, end) { uniqueId = nextUniqueDescriptorId++ };
//        }

//        private static WebAssemblyComponentDescriptor createWebAssemblyComponentComment(RawComponentPayload payload, Node start, Node? end)
//        {
//            validateWebAssemblyComponentPayload(payload);
//            var marker = new WebAssemblyComponentMarker(payload.typeName!, payload.assembly!, payload.parameterDefinitions!, payload.parameterValues!, payload.prerenderId, payload.key);
//            return new WebAssemblyComponentDescriptor(marker, start, end) { uniqueId = nextUniqueDescriptorId++ };
//        }

//        private static AutoComponentDescriptor createAutoComponentComment(RawComponentPayload payload, Node start, Node? end)
//        {
//            validateServerComponentPayload(payload);
//            validateWebAssemblyComponentPayload(payload);
//            var marker = new AutoComponentMarker(payload.sequence!.Value, payload.descriptor!, payload.typeName!, payload.assembly!, payload.parameterDefinitions!, payload.parameterValues!, payload.prerenderId, payload.key);
//            return new AutoComponentDescriptor(marker, start, end) { uniqueId = nextUniqueDescriptorId++ };
//        }

//        private static void validateServerComponentPayload(RawComponentPayload payload)
//        {
//            if (string.IsNullOrEmpty(payload.descriptor)) throw new Exception("descriptor must be defined when using a descriptor.");
//            if (payload.sequence == null) throw new Exception("sequence must be defined when using a descriptor.");
//        }

//        private static void validateWebAssemblyComponentPayload(RawComponentPayload payload)
//        {
//            if (string.IsNullOrEmpty(payload.assembly)) throw new Exception("assembly must be defined when using a descriptor.");
//            if (string.IsNullOrEmpty(payload.typeName)) throw new Exception("typeName must be defined when using a descriptor.");

//            // Passes directly through your runtime's native JavaScript macro bindings
//            payload.parameterDefinitions = payload.parameterDefinitions != null ? atob(payload.parameterDefinitions) : null;
//            payload.parameterValues = payload.parameterValues != null ? atob(payload.parameterValues) : null;
//        }

//        private static void validateEndComponentPayload(string json, string prerenderId)
//        {
//            var payload = NetJs.Script.JSONParse<ComponentEndMarker>(json);
//            if (payload == null || string.IsNullOrEmpty(payload.prerenderId))
//            {
//                throw new Exception($"Invalid end of component comment: '{json}'");
//            }
//            if (payload.prerenderId != prerenderId)
//            {
//                throw new Exception($"End of component comment prerendered property must match the start comment prerender id: '{prerenderId}', '{payload.prerenderId}'");
//            }
//        }

//        private static void assertNotDirectlyOnDocument(Node marker)
//        {
//            if (marker.parentNode is Document)
//            {
//                throw new Exception("Root components cannot be marked as interactive. The <html> element must be rendered statically so that scripts are not evaluated multiple times.");
//            }
//        }

//        public static ComponentMarker descriptorToMarker(ComponentDescriptor descriptor)
//        {
//            return descriptor.type switch
//            {
//                "server" => new ServerComponentMarker(((ServerComponentDescriptor)descriptor).sequence, ((ServerComponentDescriptor)descriptor).descriptor, descriptor.prerenderId, descriptor.key),
//                "webassembly" => new WebAssemblyComponentMarker(((WebAssemblyComponentDescriptor)descriptor).typeName, ((WebAssemblyComponentDescriptor)descriptor).assembly, ((WebAssemblyComponentDescriptor)descriptor).parameterDefinitions, ((WebAssemblyComponentDescriptor)descriptor).parameterValues, descriptor.prerenderId, descriptor.key),
//                "auto" => new AutoComponentMarker(((AutoComponentDescriptor)descriptor).sequence, ((AutoComponentDescriptor)descriptor).descriptor, ((AutoComponentDescriptor)descriptor).typeName, ((AutoComponentDescriptor)descriptor).assembly, ((AutoComponentDescriptor)descriptor).parameterDefinitions, ((AutoComponentDescriptor)descriptor).parameterValues, descriptor.prerenderId, descriptor.key),
//                _ => throw new Exception("Unknown descriptor type")
//            };
//        }

//        public static bool canMergeDescriptors(ComponentDescriptor target, ComponentDescriptor source)
//        {
//            if (target.type != source.type) return false;

//            var a = target.key;
//            var b = source.key;
//            if (a == null || b == null) return false;

//            return a.locationHash == b.locationHash && a.formattedComponentKey == b.formattedComponentKey;
//        }

//        public static void mergeDescriptors(ComponentDescriptor target, ComponentDescriptor source)
//        {
//            if (!canMergeDescriptors(target, source))
//            {
//                throw new Exception("Cannot merge mismatching component descriptors.");
//            }

//            target.uniqueId = source.uniqueId;

//            if (target is WebAssemblyComponentDescriptor tw && source is WebAssemblyComponentDescriptor sw)
//            {
//                tw.parameterDefinitions = sw.parameterDefinitions;
//                tw.parameterValues = sw.parameterValues;
//            }
//            else if (target is ServerComponentDescriptor ts && source is ServerComponentDescriptor ss)
//            {
//                ts.sequence = ss.sequence;
//                ts.descriptor = ss.descriptor;
//            }
//            else if (target is AutoComponentDescriptor ta && source is AutoComponentDescriptor sa)
//            {
//                ta.parameterDefinitions = sa.parameterDefinitions;
//                ta.parameterValues = sa.parameterValues;
//                ta.sequence = sa.sequence;
//                ta.descriptor = sa.descriptor;
//            }
//        }
//    }

//    public class ComponentCommentIterator
//    {
//        private readonly NodeList _childNodes;
//        private int currentIndex;
//        private readonly int length;

//        public Node? currentElement { get; private set; }

//        public ComponentCommentIterator(NodeList childNodes)
//        {
//            _childNodes = childNodes;
//            this.currentIndex = -1;
//            this.length = childNodes.length;
//        }

//        public bool next()
//        {
//            this.currentIndex++;
//            if (this.currentIndex < this.length)
//            {
//                this.currentElement = _childNodes[this.currentIndex];
//                return true;
//            }
//            this.currentElement = null;
//            return false;
//        }
//    }

//    public static partial class ComponentDiscoveryEngine
//    {
//        // Tells the NetJs compiler to translate this call directly into native window.atob()
//        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
//        public static extern string atob(string encodedData);
//    }
//}
