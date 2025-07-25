using SharpDemangler.Microsoft;

namespace DLLToHeader;

public class NodeArrayNodeComparer : IComparer<NodeArrayNode>
{
    public int Compare(NodeArrayNode? x, NodeArrayNode? y)
        => (x is null || y is null) ? 0 : y.Nodes.Length - x.Nodes.Length;
}
