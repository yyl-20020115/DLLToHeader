using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class VcallThunkIdentifierNode : IdentifierNode
{
    public ulong OffsetInVtable = 0;

    public VcallThunkIdentifierNode() : base(NodeKind.VcallThunkIdentifier)
    {
    }

    public override void Output(OutputStream os, OutputFlags flags)
    {
        os.Append("`vcall'{");
        os.Append(OffsetInVtable);
        os.Append(", {flat}}");
    }
}
