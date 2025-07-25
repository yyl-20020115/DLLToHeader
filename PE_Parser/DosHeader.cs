namespace PEParser;

// Define the necessary structures
public struct DosHeader
{
    public ushort magic;
    public ushort e_cblp;
    public ushort e_cp;
    public ushort e_crlc;
    public ushort e_cparhdr;
    public ushort e_minalloc;
    public ushort e_maxalloc;
    public ushort e_ss;
    public ushort e_sp;
    public ushort e_csum;
    public ushort e_ip;
    public ushort e_cs;
    public ushort e_lfarlc;
    public ushort e_ovno;
    public ulong e_res;
    public ushort e_oemid;
    public ushort e_oeminfo;
    public ulong e_res2;
    public uint e_lfanew;
    public PEHeader pe;
    public DataDirectory[]? dataDirectory;
    public SectionTable[]? section_table;
    public ExportDirectory exportDir;
    public ImportDirectory[]? importDir;
}
