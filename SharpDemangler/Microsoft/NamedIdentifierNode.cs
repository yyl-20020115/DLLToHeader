using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class NamedIdentifierNode : IdentifierNode
{
    public StringView Name;

    public NamedIdentifierNode() : base(NodeKind.NamedIdentifier)
    {
    }

    public override void Output(OutputStream os, OutputFlags flags)
    {
        os.Append(Name);
        OutputTemplateParameters(os, flags);
    }
    public override int GetHashCode() => this.Name.GetHashCode();
    public override bool Equals(object obj)
        => (obj is NamedIdentifierNode other) && this.Name.Equals(other.Name);

}