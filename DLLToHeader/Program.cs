using PE_Parser;
using SharpDemangler.Common;
using SharpDemangler.Microsoft;
using System.Diagnostics.CodeAnalysis;

namespace DLLToHeader;

public class Program
{
    public static PEHeader.DosHeader Load(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);
        PEHeader.DosHeader dosHeader = new();

        // read headers
        PEHeader.ReadDos(reader, ref dosHeader);
        PEHeader.ReadPe(reader, ref dosHeader);
        PEHeader.ReadDataDir(reader, ref dosHeader);
        PEHeader.ReadSections(reader, ref dosHeader);
        PEHeader.ReadDataOffset(ref dosHeader);
        PEHeader.ReadExportDir(reader, ref dosHeader);
        PEHeader.ReadImportDir(reader, ref dosHeader);

        return dosHeader;
    }

    public class NameEqualtyComparer : IEqualityComparer<QualifiedNameNode>
    {
        public bool Equals(QualifiedNameNode? x, QualifiedNameNode? y)
            => (x is null || y is null) || (x is not null && y is not null && x.Equals(y));

        public int GetHashCode([DisallowNull] QualifiedNameNode obj) => obj.GetHashCode();
    }
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("DLLToHeader <File.dll> [Header.h]");
        }

        var header = Misc.LoadFrom(args[0]);
        var kinds = new HashSet<NodeKind>();
        if (header.exportDir.exportAddr_name_t != null)
        {
            var asts = new List<SymbolNode>();
            for (var i = 0; i < header.exportDir.exportAddr_name_t.Length; i++)
            {
                var demangler = new MicrosoftDemangler();
                var export = header.exportDir.exportAddr_name_t[i];
                var ast = demangler.Parse(export.names ?? "");
                if (ast != null)
                {
                    kinds.Add(ast.Kind);
                    asts.Add(ast);
                    var text = demangler.Format(ast);
                    Console.WriteLine($"{text}");
                }
                else
                {
                    Console.Error.WriteLine($"Error:{export.names}");
                }
            }

            //asts.Where(ast=>ast.Kind== NodeKind.VariableSymbol|| ast.Kind== NodeKind.FunctionSymbol)
            //    .ToList()
            var class_bases = new Dictionary<QualifiedNameNode, HashSet<QualifiedNameNode>>(
                new NameEqualtyComparer()
                );
            foreach (var ast in asts)
            {
                switch (ast.Kind)
                {
                    case NodeKind.VariableSymbol:
                        break;
                    case NodeKind.FunctionSymbol:
                        break;
                    case NodeKind.SpecialTableSymbol:
                        if (ast is SpecialTableSymbolNode sp)
                        {
                            if (class_bases.TryGetValue(sp.Name, out var set))
                            {
                                set.Add(sp.TargetName);
                            }
                            else
                            {
                                class_bases[sp.Name] = [sp.TargetName];
                            }

                        }
                        break;
                    case NodeKind.Identifier://common function or variable
                        break;
                }
            }
            class_bases.Clear();
        }
        return 0;
    }

}