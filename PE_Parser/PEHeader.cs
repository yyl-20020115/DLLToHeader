namespace PEParser;

public struct PEHeader
{
    public uint signature;
    public ushort machine;
    public ushort numberOfSections;
    public uint timeStamp;
    public uint symTablePtr;
    public uint numberOfSym;
    public ushort optionalHeaderSize;
    public ushort characteristics;
    public OptionalHeader optionalHeader;
}
