using SharpDemangler.Common;

namespace SharpDemangler.Microsoft;

public class SymbolNode : Node
{
    public QualifiedNameNode Name;

    public SymbolNode(NodeKind kind) : base(kind)
    {
    }

    public override void Output(OutputStream os, OutputFlags flags)
    {
        Name.Output(os, flags);
    }
    public override string ToString()
    {
        var os = new OutputStream();
        this.Output(os, OutputFlags.Default);
        return os.ToString();
    }
    public override bool Equals(object obj) => obj is SymbolNode other && Name.Equals(other.Name);
    public override int GetHashCode() => base.GetHashCode() ^ (Name?.GetHashCode() ?? 0);
}