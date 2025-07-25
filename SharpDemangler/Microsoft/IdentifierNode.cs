using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class IdentifierNode(NodeKind kind) : Node(kind)
{
    public NodeArrayNode TemplateParams;

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