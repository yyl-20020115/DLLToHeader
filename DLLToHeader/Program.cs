using PEParser;
using SharpDemangler.Common;
using SharpDemangler.Microsoft;
using System.Diagnostics;

namespace DLLToHeader;

public partial class Program
{
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

        var header = PELoader.LoadFrom(dllfile);
        var asts = new List<SymbolNode>();
        var class_bases = new Dictionary<NodeArrayNode, HashSet<NodeArrayNode>>();
        var namespace_classes = new Dictionary<NodeArrayNode, Dictionary<NodeArrayNode, List<SymbolNode>>>();
        var class_namespaces = new Dictionary<NodeArrayNode, NodeArrayNode>();

        var global_functions = new Dictionary<NodeArrayNode, SymbolNode>();
        var variables = new HashSet<SymbolNode>();
        var functions = new HashSet<SymbolNode>();
        var plains = new List<SymbolNode>();
        var deps = new Dictionary<NodeArrayNode, HashSet<NodeArrayNode>>();
        var namespaces = AstProcessor.ExtractExports(header.ExportDirectory.Exports, asts, namespace_classes);
        AstProcessor.
                Compile(namespace_classes, class_namespaces, global_functions, class_bases, variables, functions, plains, namespaces, asts, deps);
        FileGenerator.
                GenerateHeaderFile(namespace_classes, namespaces, class_namespaces, deps, global_functions, class_bases, plains, hdrfile, libfile);

        if (FileGenerator.GenerateDefFile(deffile, asts, true))
        {
            FileGenerator.GenerateLibFile(libfile, deffile, machine);
        }
        return 0;
    }
}