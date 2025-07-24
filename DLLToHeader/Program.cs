using PE_Parser;
using SharpDemangler.Microsoft;
using System.Diagnostics;
using static PE_Parser.PEHeader;

namespace DLLToHeader;

public class Program
{
    public static NodeArrayNode ExtractNamespacePart(NodeArrayNode node)
    {
        int p = -1;
        for (int i = 0; i < node.Nodes.Length; i++)
        {
            if (node.Nodes[i] is StructorIdentifierNode n && !n.IsDestructor)
            {
                p = i - 1;
                break;
            }
        }
        if (p >= 0)
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
    public static void CompileAsts(
        Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>> namespace_classes,
        Dictionary<NodeArrayNode, SymbolNode> global_functions,
        Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>> class_bases,
        HashSet<SymbolNode> variables,
        HashSet<SymbolNode> functions,
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
                    break;
                case NodeKind.Identifier://common function or variable
                                         //unable to generate c++/c header file
                    break;
            }
        }

    }
    public static int GenerateLibFile(string libfile, string deffile, string machine = "x86")
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
        Dictionary<NodeArrayNode, SymbolNode> global_functions,
        Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>> class_bases,
        string hdrfile, string libfile)
    {
        using var writer = new StreamWriter(hdrfile);
        writer.WriteLine("#pragma once");
        writer.WriteLine("#define DLLIMPORT __declspec(dllimport)");
        writer.WriteLine($"#pragma comment(lib,\"{libfile}\")");

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
                    var q2 = new QualifiedNameNode() { Kind = NodeKind.QualifiedName, Components = ks.Key };
                    writer.WriteLine($"\tclass {q2};");
                }

            }
            foreach (var cs in ns.Value)
            {
                var q2 = new QualifiedNameNode() { Kind = NodeKind.QualifiedName, Components = cs.Key };
                var full = new NodeArrayNode
                {
                    Kind = NodeKind.NodeArray,
                    Nodes = [.. ns.Key.Nodes, .. cs.Key.Nodes]
                };
                if (!global_functions.ContainsKey(full))
                {
                    writer.WriteLine($"\tclass {q2}");
                    if (class_bases.TryGetValue(full, out var deps) && deps.Count > 0)
                    {
                        var any = false;
                        foreach (var dep in deps)
                        {
                            if (dep != null)
                            {
                                writer.Write("\t\t");
                                writer.Write(any ? ", " : ": ");
                                writer.WriteLine(dep);
                                any = true;
                            }
                        }
                    }

                    writer.WriteLine("\t{");

                    foreach (var ts in cs.Value)
                    {
                        writer.WriteLine($"\t\t{ts};");

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
            }
            writer.WriteLine("}");
        }

    }


    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("DLLToHeader <DllFile.dll> [HdrFile.h] [DefFile.def] [LibFile.lib]");
            return 0;
        }

        var dllfile = args[0];
        var hdrfile = args.Length > 1 ? args[1] : Path.ChangeExtension(dllfile, ".h");
        var deffile = args.Length > 2 ? args[2] : Path.ChangeExtension(dllfile, ".def");
        var libfile = args.Length > 3 ? args[3] : Path.ChangeExtension(dllfile, ".lib");

        var header = Misc.LoadFrom(args[0]);
        var asts = new List<SymbolNode>();
        var class_bases = new Dictionary<NodeArrayNode, HashSet<QualifiedNameNode>>();
        var namespace_classes = new Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>>();
        var global_functions = new Dictionary<NodeArrayNode, SymbolNode>();
        var variables = new HashSet<SymbolNode>();
        var functions = new HashSet<SymbolNode>();

        var namespaces = ExtractExports(header.exportDir.exportAddr_name_t, asts, namespace_classes);

        CompileAsts(namespace_classes, global_functions, class_bases, variables, functions, namespaces, asts);

        GenerateHeaderFile(namespace_classes, global_functions, class_bases, hdrfile, libfile);

        if (GenerateDefFile(deffile, asts, true))
        {
            GenerateLibFile(libfile, deffile);
        }
        return 0;
    }
}