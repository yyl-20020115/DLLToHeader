using SharpDemangler.Common;
using System.Linq;

namespace SharpDemangler.Microsoft;

public class QualifiedNameNode : Node
{
    public NodeArrayNode Components;

    public static QualifiedNameNode From(NodeArrayNode nodes)
    {
       return new QualifiedNameNode() { Kind = NodeKind.QualifiedName, Components = nodes };
    }
    public QualifiedNameNode() : base(NodeKind.QualifiedName)
    {
    }

    public IdentifierNode UnqualifiedIdentifier => (IdentifierNode)Components[Components.Count() - 1];

    public override void Output(OutputStream os, OutputFlags flags)
    {
        Components.Output(os, flags, "::");
    }
    public override int GetHashCode() => this.Components.GetHashCode();
    public override bool Equals(object obj) => obj is QualifiedNameNode node &&
            ((this.Components == null && node.Components == null) ||
                this.Components.Nodes.Length == node.Components.Nodes.Length
                && this.Components.Nodes.SequenceEqual(node.Components.Nodes)
            );


}