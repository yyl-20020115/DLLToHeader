namespace PEParser;

public struct PEHeader
{
    public uint Signature;
    public ushort Machine;
    public ushort NumberOfSections;
    public uint TimeStamp;
    public uint SymbolTablePointer;
    public uint NumberOfSymbols;
    public ushort OptionalHeaderSize;
    public ushort Characteristics;
    public OptionalHeader OptionalHeader;
}
