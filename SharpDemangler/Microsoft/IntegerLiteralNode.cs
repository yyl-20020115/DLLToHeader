using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class IntegerLiteralNode(ulong value, bool isNegative) : Node(NodeKind.IntegerLiteral)
{
    public ulong Value = value;
    public bool IsNegative = isNegative;

    public override void Output(OutputStream os, OutputFlags flags)
    {
        if (IsNegative)
            os.Append('-');
        os.Append(Value);
    }
}
