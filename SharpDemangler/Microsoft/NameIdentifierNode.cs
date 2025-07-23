using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class NameIdentifierNode : IdentifierNode
{
    public StringView Name;

    public NameIdentifierNode() : base(NodeKind.NamedIdentifier)
    {
    }

    public override void Output(OutputStream os, OutputFlags flags)
    {
        os.Append(Name);
        OutputTemplateParameters(os, flags);
    }
    public override int GetHashCode() => this.Name.GetHashCode();
    public override bool Equals(object obj) 
        => (obj is NameIdentifierNode other) && this.Name.Equals(other.Name);
}