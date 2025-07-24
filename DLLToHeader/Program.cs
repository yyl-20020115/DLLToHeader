using PEParser;
using SharpDemangler.Common;
using SharpDemangler.Microsoft;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using static PEParser.PEHeader;

namespace DLLToHeader;

public class Program
{
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
    public class NodeArrayNodeComparer : IComparer<NodeArrayNode>
    {
        public int Compare(NodeArrayNode? x, NodeArrayNode? y)
            => (x is null || y is null) ? 0 : y.Nodes.Length - x.Nodes.Length;
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
    public static List<NodeArrayNode> ExtractExports(ExportAddressName[]? exports, List<SymbolNode> asts,
        Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>> namespace_classes)
    {
        if (exports != null)
        {
            for (var i = 0; i < exports.Length; i++)
            {
                var demangler = new MicrosoftDemangler();
                var export = exports[i];
                var ast = demangler.Parse(export.names ?? "");
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

    public static readonly char[] InitalChars = ['C', 'I', 'E'];
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
        var builder = new StringBuilder();
        builder.Append(name);
        builder.Append(val);
        var text = builder.ToString();
        return text;
    }
    public static void NameParameters(NodeArrayNode? parameters)
    {
        if (parameters != null)
        {
            var names = new Dictionary<char, int>();
            
            for (int ip = 0; ip < parameters.Nodes.Length; ip++)
            {
                var p = parameters.Nodes[ip];
                switch (p)
                {
                    case PointerTypeNode pointerTypeNode:
                        {
                            if (pointerTypeNode.Pointee is TagTypeNode tg)
                            {
                                if (tg.QualifiedName.Components.FirstOrDefault() is NamedIdentifierNode j)
                                {
                                    tg.Name = new NamedIdentifierNode
                                    {
                                        Kind = NodeKind.Identifier,
                                        Name = new StringView(GetName(GetInitialChar(j.Name), names))
                                    };
                                }
                            }
                            else if (pointerTypeNode.Pointee is PrimitiveTypeNode primitiveTypeNode)
                            {
                                pointerTypeNode.Pointee = primitiveTypeNode = primitiveTypeNode.Clone();

                                var primeType = primitiveTypeNode.PrimKind.ToString();

                                primitiveTypeNode.Name = new NamedIdentifierNode
                                {
                                    Kind = NodeKind.Identifier,
                                    Name =
                                    new StringView(GetName(GetInitialChar(primeType), names))
                                };
                                parameters[ip] = primitiveTypeNode.Clone();
                            }
                            break;
                        }
                    case TagTypeNode tagTypeNode:
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
                        parameters.Nodes[ip] = primitiveTypeNode = primitiveTypeNode.Clone();
                        if (primitiveTypeNode.PrimKind != PrimitiveKind.Void)
                        {
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
            }
            foreach (var p in parameters)
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
                            var c = primitiveTypeNode.Name.Name[0];
                            if (names.TryGetValue(c, out var cnt))
                            {
                                if (cnt == 1)
                                {
                                    primitiveTypeNode.Name.Name = c.ToString();
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
                            var c = primitiveTypeNode.Name.Name[0];
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

        }

    }
    public static void TrimParameters(NodeArrayNode? parameters, List<NodeArrayNode> namespaces, Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces)
    {
        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                if (p is PrimitiveTypeNode primitiveTypeNode && primitiveTypeNode.PrimKind== PrimitiveKind.Void)
                {
                    primitiveTypeNode.PrimKind = PrimitiveKind.None;
                }
                else
                {
                    TrimTypeNode(p as TypeNode, namespaces, class_namespaces);
                }
            }
        }

    }
    public static void TrimTypeNode(TypeNode? type_node, List<NodeArrayNode> namespaces, Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces)
    {
        if (type_node is PointerTypeNode pn && pn.Pointee is TagTypeNode tn1
            )
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
    public static void CompileAsts(
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
                                ast.Name.Components = GetLeftFunctionPart(ast.Name.Components, namespaces);
                            }
                            else
                            {
                            }
                        }

                        var @namespace = GetNamespacePart(astname, namespaces);
                        if (@namespace.Nodes.Length >= 0)
                        {
                            var @class = GetClassPart(astname, namespaces);
                            var @name = GetLeftClassPart(astname, namespaces);
                            if (@name.Nodes.Length == 0)
                            {
                                @name = new NodeArrayNode { Kind = NodeKind.NodeArray, Nodes = [astname.Nodes[^1]] };
                                @class = new NodeArrayNode { Kind = NodeKind.NodeArray, Nodes = [.. astname.Nodes.Take(astname.Nodes.Length - 1)] };
                            }
                            var @full_class = GetFullClassPart(astname, namespaces);
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
                TrimTypeNode(fc.Signature.ReturnType, namespaces, class_namespaces);

                TrimParameters(fc.Signature.Params, namespaces, class_namespaces);

                NameParameters(fc.Signature.Params);
            }
            else if (ast is VariableSymbolNode vc)
            {
                TrimTypeNode(vc.Type, namespaces, class_namespaces);
            }
        }

    }
    public static int GenerateLibFile(string libfile, string deffile, string machine/* = "x86"*/)
    {
        if (!string.IsNullOrEmpty(libfile) && File.Exists(deffile))
        {
            try
            {
                var p = Process.Start("link.exe", $"/LIB /DEF:{deffile} /OUT:{libfile} /MACHINE:{machine}");
                p.WaitForExit();
                return p.ExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        return -1;
    }
    public static bool GenerateDefFile(string deffile, List<SymbolNode> asts, bool use_oridinal = false)
    {
        if (!string.IsNullOrEmpty(deffile))
        {
            using var writer = new StreamWriter(deffile);
            writer.WriteLine("; Generated by DLLToHeader");

            writer.WriteLine("LIBRARY \"" + Path.GetFileNameWithoutExtension(deffile) + "\"");
            writer.WriteLine("EXPORTS");
            // write exports
            foreach (var ast in asts)
            {
                if (ast.Kind != NodeKind.Identifier)
                {
                    var text = ast.ToString();
                    writer.WriteLine(";\t" + text);
                }
                writer.Write("\t" + ast.Text);
                if (use_oridinal)
                {
                    writer.Write($" @{ast.Ordinal}");
                }
                writer.WriteLine();
            }
            return true;
        }
        return false;
    }
    public static void GenerateHeaderFile(
        Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>> namespace_classes,
        List<NodeArrayNode> namespaces,
        Dictionary<NodeArrayNode, NodeArrayNode> class_namespaces,
        Dictionary<NodeArrayNode, SymbolNode> global_functions,
        Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>> class_bases,
        List<SymbolNode> plains,
        string hdrfile, string libfile)
    {
        using var writer = new StreamWriter(hdrfile);
        writer.WriteLine("#pragma once");
        writer.WriteLine("#define DLLIMPORT __declspec(dllimport)");
        writer.WriteLine($"#pragma comment(lib,\"{libfile}\")");

        if (plains.Count > 0)
        {
            writer.WriteLine("extern \"C\"");
            writer.WriteLine("{");
            foreach (var ast in plains)
            {
                writer.WriteLine($"\tDLLIMPORT void {ast}();");
            }
            writer.WriteLine("}");
            writer.WriteLine();
        }
        foreach (var ns in namespace_classes)
        {
            var q = new QualifiedNameNode() { Kind = NodeKind.QualifiedName, Components = ns.Key };
            writer.WriteLine($"namespace {q}");
            writer.WriteLine("{");

            foreach (var ks in ns.Value)
            {
                var full = new NodeArrayNode
                {
                    Kind = NodeKind.NodeArray,
                    Nodes = [.. ns.Key.Nodes, .. ks.Key.Nodes]
                };
                if (!global_functions.ContainsKey(full))
                {
                    writer.WriteLine($"\tclass {QualifiedNameNode.From(ks.Key)};");
                }
            }
            foreach (var cs in ns.Value)
            {
                var full = new NodeArrayNode
                {
                    Kind = NodeKind.NodeArray,
                    Nodes = [.. ns.Key.Nodes, .. cs.Key.Nodes]
                };
                if (!global_functions.ContainsKey(full))
                {

                    writer.WriteLine($"\tclass DLLIMPORT {QualifiedNameNode.From(cs.Key)}");
                    if (class_bases.TryGetValue(full, out var deps) && deps.Count > 0)
                    {
                        var any = false;
                        foreach (var dep in deps)
                        {
                            if (dep != null)
                            {
                                dep.Components = TrimNamespace(dep.Components, namespaces, class_namespaces);
                                writer.Write("\t\t");
                                writer.Write(any ? ", " : ": ");
                                writer.WriteLine(dep);
                                any = true;
                            }
                        }
                    }

                    writer.WriteLine("\t{");

                    var publics = cs.Value.Where(v => v is FunctionSymbolNode fs && (fs.Signature.FunctionClass & FuncClass.Public) != 0).ToArray();
                    var protecteds = cs.Value.Where(v => v is FunctionSymbolNode fs && (fs.Signature.FunctionClass & FuncClass.Protected) != 0).ToArray();
                    var privates = cs.Value.Where(v => v is FunctionSymbolNode fs && (fs.Signature.FunctionClass & FuncClass.Private) != 0).ToArray();
                    var variables = cs.Value.Where(v => v is VariableSymbolNode vs).ToList();

                    if (variables.Count > 0)
                    {
                        var values = cs.Value.ToList();
                        var v_function_local = values.Where(v => v is VariableSymbolNode fs && (fs.sc == StorageClass.FunctionLocalStatic)).ToArray();
                        var v_publics = values.Where(v => v is VariableSymbolNode fs && (fs.sc == StorageClass.PublicStatic)).ToArray();
                        var v_protecteds = values.Where(v => v is VariableSymbolNode fs && (fs.sc == StorageClass.ProtectedStatic)).ToArray();
                        var v_privates = values.Where(v => v is VariableSymbolNode fs && (fs.sc & StorageClass.PrivateStatic) != 0 && fs.LocalFunctionName == null).ToArray();
                        if (v_publics.Length > 0)
                        {
                            writer.WriteLine("\tpublic:");
                            foreach (VariableSymbolNode vs in v_publics)
                            {
                                vs.sc = StorageClass.None;
                                writer.WriteLine($"\t\tstatic {vs};");
                            }
                        }
                        if (v_protecteds.Length > 0)
                        {
                            writer.WriteLine("\tprotected:");
                            foreach (VariableSymbolNode vs in v_protecteds)
                            {
                                vs.sc = StorageClass.None;
                                writer.WriteLine($"\t\tstatic {vs};");
                            }
                        }
                        if (v_privates.Length > 0)
                        {
                            writer.WriteLine("\tprivate:");
                            foreach (VariableSymbolNode vs in v_privates)
                            {
                                vs.sc = StorageClass.None;
                                writer.WriteLine($"\t\tstatic {vs};");
                            }
                        }
                        if (v_function_local.Length > 0)
                        {
                            writer.WriteLine("\t//function local static");
                            foreach (VariableSymbolNode vs in v_function_local)
                            {
                                vs.sc = StorageClass.None;
                                writer.WriteLine($"\t\t//{vs};");
                            }
                        }

                    }
                    if (publics.Length > 0)
                    {
                        writer.WriteLine("\tpublic:");
                        foreach (FunctionSymbolNode ts in publics)
                        {
                            ts.Signature.FunctionClass &= ~FuncClass.Public;
                            writer.WriteLine($"\t\t{ts};");

                        }
                    }
                    if (protecteds.Length > 0)
                    {
                        writer.WriteLine("\tprotected:");
                        foreach (FunctionSymbolNode ts in protecteds)
                        {
                            ts.Signature.FunctionClass &= ~FuncClass.Protected;
                            writer.WriteLine($"\t\t{ts};");

                        }
                    }
                    if (privates.Length > 0)
                    {
                        writer.WriteLine("\tprivate:");
                        foreach (FunctionSymbolNode ts in privates)
                        {
                            ts.Signature.FunctionClass &= ~FuncClass.Private;
                            writer.WriteLine($"\t\t{ts};");

                        }
                    }

                    writer.WriteLine("\t};");
                }
                else
                {
                    //function:
                    foreach (var ts in cs.Value)
                    {
                        writer.WriteLine($"\t{ts};");
                    }
                }
                writer.WriteLine();
            }

            writer.WriteLine("}");
            writer.WriteLine();
        }

    }


    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("DLLToHeader <DllFile.dll> [HdrFile.h] [DefFile.def] [LibFile.lib] [machine:x86/x64]");
            return 0;
        }

        var dllfile = args[0];
        var hdrfile = args.Length > 1 ? args.FirstOrDefault(a => a.ToLower().EndsWith(".h")) : null;
        hdrfile ??= Path.ChangeExtension(dllfile, ".h");
        var deffile = args.Length > 1 ? args.FirstOrDefault(a => a.ToLower().EndsWith(".def")) : null;
        deffile ??= Path.ChangeExtension(dllfile, ".def");
        var libfile = args.Length > 1 ? args.FirstOrDefault(a => a.ToLower().EndsWith(".lib")) : null;
        libfile ??= Path.ChangeExtension(dllfile, ".lib");
        var machine = args.Length > 1 ? args.FirstOrDefault(a => a.ToLower().StartsWith("machine:"))?.Substring(8) : null;
        machine ??= "x64";

        var header = Misc.LoadFrom(args[0]);
        var asts = new List<SymbolNode>();
        var class_bases = new Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>>();
        var namespace_classes = new Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>>();
        var class_namespaces = new Dictionary<NodeArrayNode, NodeArrayNode>();

        var global_functions = new Dictionary<NodeArrayNode, SymbolNode>();
        var variables = new HashSet<SymbolNode>();
        var functions = new HashSet<SymbolNode>();
        var plains = new List<SymbolNode>();
        var namespaces = ExtractExports(header.exportDir.exportAddr_name_t, asts, namespace_classes);

        CompileAsts(namespace_classes, class_namespaces, global_functions, class_bases, variables, functions, plains, namespaces, asts);

        GenerateHeaderFile(namespace_classes, namespaces, class_namespaces, global_functions, class_bases, plains, hdrfile, libfile);

        if (GenerateDefFile(deffile, asts, true))
        {
            GenerateLibFile(libfile, deffile, machine);
        }
        return 0;
    }
}