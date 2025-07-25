namespace PEParser;

public struct SectionTable
{
    public string? name;
    public uint virtualSize;
    public uint virtualAddr;
    public uint sizeOfRawData;
    public uint ptrToRawData;
    public uint ptrToReloc;
    public uint ptrToLineNum;
    public ushort numberOfReloc;
    public ushort numberOfLineNum;
    public uint characteristics;
}
