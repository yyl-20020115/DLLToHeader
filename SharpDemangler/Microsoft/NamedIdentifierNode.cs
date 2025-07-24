using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class NamedIdentifierNode : IdentifierNode
{
    public StringView Name;
    public Node Scope;
    public NamedIdentifierNode() : base(NodeKind.NamedIdentifier)
    {
    }
    public NamedIdentifierNode Clone() => new NamedIdentifierNode
    {
        Kind = this.Kind,
        Name = new StringView(this.Name),
        Scope = this.Scope,
        TemplateParams = TemplateParams
    };
    public override void Output(OutputStream os, OutputFlags flags)
    {
        os.Append(Name);
        OutputTemplateParameters(os, flags);
    }
    public override int GetHashCode() => this.Name.GetHashCode();
    public override bool Equals(object obj)
        => (obj is NamedIdentifierNode other) && this.Name.Equals(other.Name);

}