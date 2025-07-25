namespace PEParser;

public class ExtendedDosHeader : DosHeader
{
    public PEHeader PE;
    public DataDirectory[]? DataDirectory;
    public SectionTable[]? SectionTable;
    public ExportDirectory ExportDirectory;
    public ImportDirectory[]? ImportDirectory;
}