using SharpDemangler.Common;
using System.Linq;

namespace SharpDemangler.Microsoft;

public class VariableSymbolNode : SymbolNode
{
    public StorageClass sc = StorageClass.None;
    public TypeNode Type = null;
    public QualifiedNameNode LocalFunctionName
        => ((((this.Name.Components.FirstOrDefault() as NamedIdentifierNode)?.Scope) as FunctionSymbolNode)?.Name);

    public VariableSymbolNode() : base(NodeKind.VariableSymbol)
    {
    }

    public override void Output(OutputStream os, OutputFlags flags)
    {
        switch (sc)
        {
            case StorageClass.PrivateStatic:
                os.Append("private: static ");
                break;
            case StorageClass.PublicStatic:
                os.Append("public: static ");
                break;
            case StorageClass.ProtectedStatic:
                os.Append("protected: static ");
                break;
            default:
                break;
        }

        if (Type != null)
        {
            Type.OutputPre(os, flags);
            OutputSpaceIfNecessary(os);
        }

        Name.Output(os, flags);

        if (Type != null)
            Type.OutputPost(os, flags);
    }
}