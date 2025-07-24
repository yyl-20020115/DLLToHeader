using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class IdentifierNode : Node
{
    public NodeArrayNode TemplateParams;
    public IdentifierNode(NodeKind kind) : base(kind)
    {
    }

    public void OutputTemplateParameters(OutputStream os, OutputFlags flags)
    {
        if (TemplateParams == null)
            return;
        os.Append('<');
        TemplateParams.Output(os, flags);
        os.Append('>');
    }
    public override int GetHashCode()
        => (TemplateParams?.GetHashCode() ?? 0);
    public override bool Equals(object obj)
        => (obj is IdentifierNode other) && this.Kind == other.Kind && (
            this.TemplateParams == null && other.TemplateParams == null ||
            this.TemplateParams.Equals(other.TemplateParams)
        );

}