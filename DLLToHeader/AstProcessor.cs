using PEParser;
using SharpDemangler.Common;
using SharpDemangler.Microsoft;

namespace DLLToHeader;

public static class AstProcessor
{

    public static readonly char[] InitalChars = ['C', 'I', 'E'];
    public static void Compile(
        Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>> namespace_classes,
        Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces,
        Dictionary<NodeArrayNode, SymbolNode> global_functions,
        Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>> class_bases,
        HashSet<SymbolNode> variables,
        HashSet<SymbolNode> functions,
        List<SymbolNode> plains,
        List<NodeArrayNode> namespaces,
        List<SymbolNode> asts)
    {
        foreach (var ast in asts)
        {
            switch (ast.Kind)
            {
                case NodeKind.VariableSymbol:
                case NodeKind.FunctionSymbol:
                    {
                        var astname = ast.Name.Components;
                        if (ast is VariableSymbolNode vn && vn.sc == StorageClass.FunctionLocalStatic)
                        {
                            astname = vn.LocalFunctionName?.Components;
                            astname ??= ast.Name.Components;
                        }
                        else if (ast is FunctionSymbolNode fc)
                        {
                            if (FuncClass.Global == (fc.Signature.FunctionClass & FuncClass.Global))
                            {
                                global_functions[astname] = ast;
                                ast.Name.Components = AstProcessor.GetLeftFunctionPart(ast.Name.Components, namespaces);
                            }
                            else
                            {
                            }
                        }

                        var @namespace = AstProcessor.GetNamespacePart(astname, namespaces);
                        if (@namespace.Nodes.Length >= 0)
                        {
                            var @class = AstProcessor.GetClassPart(astname, namespaces);
                            var @name = AstProcessor.GetLeftClassPart(astname, namespaces);
                            if (@name.Nodes.Length == 0)
                            {
                                @name = new NodeArrayNode { Kind = NodeKind.NodeArray, Nodes = [astname.Nodes[^1]] };
                                @class = new NodeArrayNode { Kind = NodeKind.NodeArray, Nodes = [.. astname.Nodes.Take(astname.Nodes.Length - 1)] };
                            }
                            var @full_class = AstProcessor.GetFullClassPart(astname, namespaces);
                            class_namespaces[@full_class] = @namespace;

                            if (ast is FunctionSymbolNode fc && FuncClass.Global != (fc.Signature.FunctionClass & FuncClass.Global))
                            {
                                ast.Name.Components = @name;
                            }
                            if (ast is FunctionSymbolNode fn)
                            {

                                //remove __thiscall
                                fn.Signature.CallConvention &= ~CallingConv.Thiscall;
                            }
                            @class ??= new NodeArrayNode() { Kind = NodeKind.Identifier, Nodes = [] };
                            if (namespace_classes.TryGetValue(@namespace, out var set))
                            {
                                //null means self
                                //if (@class != null)
                                if (set.TryGetValue(@class, out var list))
                                {
                                    list.Add(ast);
                                }
                                else
                                {
                                    set[@class] = [ast];
                                }
                            }
                        }
                        if (ast.Kind == NodeKind.VariableSymbol)
                        {
                            variables.Add(ast);
                        }
                        else
                        {
                            functions.Add(ast);
                        }
                    }
                    break;
                case NodeKind.SpecialTableSymbol:
                    if (ast is SpecialTableSymbolNode sp)
                    {
                        var astname = sp.Name.Components;
                        if (astname?.LastOrDefault()?.ToString() == "`vftable'")
                        {
                            astname.Nodes
                                = [.. astname.Take(astname.Nodes.Length - 1)];
                        }
                        if (astname != null)
                        {
                            if (class_bases.TryGetValue(astname, out var set))
                            {
                                //null means self
                                set.Add(sp.TargetName);
                            }
                            else
                            {
                                class_bases[astname] = [sp.TargetName];
                            }
                        }
                    }
                    break;
                case NodeKind.Identifier://common function or variable
                                         //unable to generate c++/c header file
                    plains.Add(ast);
                    break;
            }
        }

        foreach (var ast in asts)
        {
            if (ast is FunctionSymbolNode fc)
            {
                AstProcessor.TrimTypeNode(fc.Signature.ReturnType, namespaces, class_namespaces);
                fc.Signature.Params = AstProcessor.TrimParameters(fc.Signature.Params, namespaces, class_namespaces);
                fc.Signature.Params = AstProcessor.NameParameters(fc.Signature.Params);
            }
            else if (ast is VariableSymbolNode vc)
            {
                AstProcessor.TrimTypeNode(vc.Type, namespaces, class_namespaces);
            }
        }

    }
    public static List<NodeArrayNode> ExtractExports(ExportAddressName[]? exports, List<SymbolNode> asts,
        Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>> namespace_classes)
    {
        if (exports != null)
        {
            for (var i = 0; i < exports.Length; i++)
            {
                var demangler = new MicrosoftDemangler();
                var export = exports[i];
                var ast = demangler.Parse(export.Names ?? "");
                if (ast != null)
                {
                    ast.Ordinal = i + 1;
                    asts.Add(ast);
                }
            }

        }

        foreach (var ast in asts)
        {
            switch (ast.Kind)
            {
                case NodeKind.FunctionSymbol:
                    {
                        var astname = ast.Name.Components;
                        if (ast is VariableSymbolNode vn && vn.sc == StorageClass.FunctionLocalStatic)
                        {
                            astname ??= vn.LocalFunctionName?.Components;
                            astname ??= ast.Name.Components;
                        }
                        var @namespace = ExtractNamespacePart(astname);
                        if (@namespace.Nodes.Length > 0)
                            namespace_classes[@namespace] = [];
                    }
                    break;
            }
        }
        var namespaces = namespace_classes.Keys.ToList();
        namespaces.Sort(new NodeArrayNodeComparer());
        return namespaces;
    }
    public static NodeArrayNode ExtractNamespacePart(NodeArrayNode node)
    {
        int p = -1;
        var any_structor = false;
        for (int i = 0; i < node.Nodes.Length; i++)
        {
            if (node.Nodes[i] is StructorIdentifierNode n)
            {
                any_structor = true;
                p = i - 1;
                break;
            }
        }
        if (any_structor)
        {
            return new NodeArrayNode
            {
                Kind = NodeKind.NodeArray,
                Nodes = [.. node.Nodes.Take(p)]
            };
        }
        else if (node.Nodes.Length == 2)
        {
            return new NodeArrayNode
            {
                Kind = NodeKind.NodeArray,
                Nodes = [node.Nodes[0]]
            };
        }
        return [];
    }
    public static NodeArrayNode GetClassPart(NodeArrayNode node, List<NodeArrayNode> namespaces)
    {
        for (int i = 0; i < namespaces.Count; i++)
        {
            if (namespaces[i].Nodes.Length <= node.Nodes.Length)
            {
                var taken = node.Nodes.Take(namespaces[i].Nodes.Length);
                if (Enumerable.SequenceEqual(namespaces[i].Nodes, taken))
                {
                    return
                        new NodeArrayNode()
                        {
                            Kind = NodeKind.NodeArray,
                            Nodes = [.. node.Nodes.Skip(namespaces[i].Nodes.Length).Take(1)]
                        };
                }
            }
        }
        return [];
    }
    public static NodeArrayNode GetFullClassPart(NodeArrayNode node, List<NodeArrayNode> namespaces)
    {
        for (int i = 0; i < namespaces.Count; i++)
        {
            if (namespaces[i].Nodes.Length <= node.Nodes.Length)
            {
                var taken = node.Nodes.Take(namespaces[i].Nodes.Length);
                if (Enumerable.SequenceEqual(namespaces[i].Nodes, taken))
                {
                    return
                        new NodeArrayNode()
                        {
                            Kind = NodeKind.NodeArray,
                            Nodes = [.. node.Nodes.Take(namespaces[i].Nodes.Length + 1)]
                        };
                }
            }
        }
        return [];
    }
    public static char GetInitialChar(string text)
    {
        return text.Length switch
        {
            1 => char.ToLower(text[0]),
            >= 2 => (InitalChars.Contains(text[0]))
                && char.IsUpper(text[1]) ? char.ToLower(text[1]) : char.ToLower(text[0]),
            _ => 'p',
        };
    }
    public static NodeArrayNode GetLeftClassPart(NodeArrayNode node, List<NodeArrayNode> namespaces)
    {
        for (int i = 0; i < namespaces.Count; i++)
        {
            if (namespaces[i].Nodes.Length <= node.Nodes.Length)
            {
                var taken = node.Nodes.Take(namespaces[i].Nodes.Length);
                if (Enumerable.SequenceEqual(namespaces[i].Nodes, taken))
                {
                    return
                        new NodeArrayNode()
                        {
                            Kind = NodeKind.NodeArray,
                            Nodes = [.. node.Nodes.Skip(namespaces[i].Nodes.Length + 1)]
                        };
                }
            }
        }
        return [];
    }

    public static NodeArrayNode GetLeftFunctionPart(NodeArrayNode node, List<NodeArrayNode> namespaces)
    {
        for (int i = 0; i < namespaces.Count; i++)
        {
            if (namespaces[i].Nodes.Length <= node.Nodes.Length)
            {
                var taken = node.Nodes.Take(namespaces[i].Nodes.Length);
                if (Enumerable.SequenceEqual(namespaces[i].Nodes, taken))
                {
                    return
                        new NodeArrayNode()
                        {
                            Kind = NodeKind.NodeArray,
                            Nodes = [.. node.Nodes.Skip(namespaces[i].Nodes.Length)]
                        };
                }
            }
        }
        return [];
    }

    public static string GetName(char name, Dictionary<char, int> names)
    {
        if (names.TryGetValue(name, out var val))
        {
            names[name] = ++val;
        }
        else
        {
            names.Add(name, val = 1);
        }
        return $"{name}{val}";
    }

    public static NodeArrayNode GetNamespacePart(NodeArrayNode node, List<NodeArrayNode> namespaces)
    {
        for (int i = 0; i < namespaces.Count; i++)
        {
            if (namespaces[i].Nodes.Length <= node.Nodes.Length)
            {
                var taken = node.Nodes.Take(namespaces[i].Nodes.Length);
                if (Enumerable.SequenceEqual(namespaces[i].Nodes, taken))
                {
                    return
                        new NodeArrayNode()
                        {
                            Kind = NodeKind.NodeArray,
                            Nodes = [.. namespaces[i].Nodes]
                        };
                }
            }
        }
        return [];
    }
    public static NodeArrayNode? NameParameters(NodeArrayNode? parameters)
    {
        var results = new List<Node>();
        if (parameters != null)
        {
            var names = new Dictionary<char, int>();
            foreach (var ip in parameters.ToList())
            {
                var p = ip;
                switch (ip)
                {
                    case PointerTypeNode pointerTypeNode:
                        {
                            if (pointerTypeNode.Pointee is TagTypeNode tg)
                            {
                                p = pointerTypeNode = pointerTypeNode.Clone();

                                if (tg.QualifiedName.Components.FirstOrDefault() is NamedIdentifierNode j)
                                {
                                    pointerTypeNode.Pointee = tg = tg.Clone();

                                    tg.Name = new NamedIdentifierNode
                                    {
                                        Kind = NodeKind.Identifier,
                                        Name = new StringView(GetName(GetInitialChar(j.Name), names))
                                    };
                                }
                            }
                            else if (pointerTypeNode.Pointee is PrimitiveTypeNode primitiveTypeNode)
                            {
                                p = pointerTypeNode = pointerTypeNode.Clone();
                                pointerTypeNode.Pointee = primitiveTypeNode = primitiveTypeNode.Clone();

                                var primeType = primitiveTypeNode.PrimKind.ToString();

                                primitiveTypeNode.Name = new NamedIdentifierNode
                                {
                                    Kind = NodeKind.Identifier,
                                    Name =
                                    new StringView(GetName(GetInitialChar(primeType), names))
                                };
                            }
                            else if (pointerTypeNode.Pointee is PointerTypeNode pn)
                            {
                                p = pointerTypeNode = pn.Clone();
                                pointerTypeNode.Pointee
                                   = NameParameters(new NodeArrayNode { Kind = NodeKind.NodeArray, Nodes = [pn] })?.Nodes?[0] as TypeNode;
                            }
                            break;
                        }
                    case TagTypeNode tagTypeNode:
                        p = tagTypeNode = tagTypeNode.Clone();
                        if (tagTypeNode.QualifiedName.Components.FirstOrDefault() is NamedIdentifierNode i)
                        {
                            tagTypeNode.Name = new NamedIdentifierNode
                            {
                                Kind = NodeKind.Identifier,
                                Name =
                                new StringView(GetName(GetInitialChar(i.Name), names))
                            };
                        }
                        break;
                    case PrimitiveTypeNode primitiveTypeNode:
                        if (primitiveTypeNode.PrimKind != PrimitiveKind.Void)
                        {
                            p = primitiveTypeNode = primitiveTypeNode.Clone();
                            var primeType = primitiveTypeNode.PrimKind.ToString();
                            primitiveTypeNode.Name = new NamedIdentifierNode
                            {
                                Kind = NodeKind.Identifier,
                                Name =
                                new StringView(GetName(GetInitialChar(primeType), names))
                            };

                        }
                        break;
                    default:

                        break;
                }
                results.Add(p);
            }
            foreach (var p in results)
            {
                switch (p)
                {
                    case PointerTypeNode pointerTypeNode:
                        if (pointerTypeNode.Pointee is TagTypeNode tg)
                        {
                            //tg.Name.Name;
                            var c = tg.Name.Name[0];
                            if (names.TryGetValue(c, out var cnt))
                            {
                                if (cnt == 1)
                                {
                                    tg.Name.Name = c.ToString();
                                }
                            }
                        }
                        else if (pointerTypeNode.Pointee is PrimitiveTypeNode primitiveTypeNode)
                        {
                            var c = char.ToLower(primitiveTypeNode.PrimKind.ToString()[0]);
                            if (names.TryGetValue(c, out var cnt))
                            {
                                if (cnt == 1)
                                {
                                    primitiveTypeNode.Name = new NamedIdentifierNode
                                    {
                                        Kind = NodeKind.Identifier,
                                        Name = c.ToString()
                                    };
                                }
                            }
                        }
                        break;
                    case TagTypeNode tagTypeNode:
                        if (tagTypeNode.QualifiedName.Components.FirstOrDefault() is NamedIdentifierNode i)
                        {
                            var c = tagTypeNode.Name.Name[0];
                            if (names.TryGetValue(c, out var cnt))
                            {
                                if (cnt == 1)
                                {
                                    tagTypeNode.Name.Name = c.ToString();
                                }
                            }
                        }
                        break;
                    case PrimitiveTypeNode primitiveTypeNode:
                        if (primitiveTypeNode.PrimKind != PrimitiveKind.Void
                            && primitiveTypeNode.Name != null)
                        {
                            var c = char.ToLower(primitiveTypeNode.PrimKind.ToString()[0]);
                            if (names.TryGetValue(c, out var cnt))
                            {
                                if (cnt == 1)
                                {
                                    primitiveTypeNode.Name.Name = c.ToString();
                                }
                            }
                        }
                        break;
                    default:

                        break;
                }
            }

            parameters.Nodes = [.. results];
            return parameters;
            //return new NodeArrayNode { Kind = parameters.Kind, Nodes = [.. results] };
        }
        return parameters;
    }
    public static NodeArrayNode TrimNamespace(NodeArrayNode name,
        List<NodeArrayNode> namespaces,
        Dictionary<NodeArrayNode, NodeArrayNode> class_namespacs,
        HashSet<NodeArrayNode>? undefineds = null)
    {
        var ns = GetNamespacePart(name, namespaces);
        if (class_namespacs.TryGetValue(name, out var result) && result.Equals(ns))
        {
            name.Nodes = [.. name.Nodes.Skip(result.Nodes.Length)];
        }
        else
        {
            var ret = GetClassPart(name, namespaces);
            if (ret.Nodes.Length > 0)
            {
                name = ret;
            }
            else if (undefineds != null)
            {
                undefineds.Add(name);
            }
        }
        return name;
    }

    public static NodeArrayNode? TrimParameters(NodeArrayNode? parameters, List<NodeArrayNode> namespaces, Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces)
    {
        if (parameters != null)
        {
            var results = new List<Node>();
            foreach (var p in parameters)
            {
                if (p is PrimitiveTypeNode primitiveTypeNode && primitiveTypeNode.PrimKind == PrimitiveKind.Void)
                {
                    primitiveTypeNode.PrimKind = PrimitiveKind.None;
                }
                else
                {
                    TrimTypeNode(p as TypeNode, namespaces, class_namespaces);
                }
                results.Add(p);
            }
            parameters = new NodeArrayNode { Kind = parameters.Kind, Nodes = [.. results] };
        }
        return parameters;

    }
    public static void TrimTypeNode(TypeNode? type_node, List<NodeArrayNode> namespaces, Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces)
    {
        if (type_node is PointerTypeNode pn && pn.Pointee is TagTypeNode tn1)
        {
            tn1.QualifiedName.Components = TrimNamespace(
            tn1.QualifiedName.Components, namespaces, class_namespaces);
            tn1.Tag = TagKind.None;

        }
        else if (type_node is TagTypeNode tn2)
        {
            tn2.QualifiedName.Components = TrimNamespace(
            tn2.QualifiedName.Components, namespaces, class_namespaces);
            tn2.Tag = TagKind.None;
        }
        else if (type_node is PrimitiveTypeNode sn)
        {

        }
        else if (type_node is FunctionSignatureNode fn)
        {

        }
        else if (type_node != null)
        {

        }
    }
}