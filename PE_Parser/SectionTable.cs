namespace PEParser;

public struct SectionTable
{
    public string? Name;
    public uint VirtualSize;
    public uint VirtualAddress;
    public uint SizeOfRawData;
    public uint PointerToRawData;
    public uint PointerToRelocation;
    public uint PointerToLineNumbers;
    public ushort NumberOfRelocation;
    public ushort NumberOfLineNumbers;
    public uint Characteristics;
}
