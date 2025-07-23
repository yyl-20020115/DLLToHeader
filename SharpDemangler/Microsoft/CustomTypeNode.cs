using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class CustomTypeNode : TypeNode
{
    public IdentifierNode Identifier;

    public CustomTypeNode() : base(NodeKind.Custom)
    {
    }

    public override void OutputPre(OutputStream os, OutputFlags flags)
    {
        Identifier.Output(os, flags);
    }

    public override void OutputPost(OutputStream os, OutputFlags flags)
    {
    }
}
