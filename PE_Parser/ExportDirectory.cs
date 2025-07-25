namespace PEParser;

public struct ExportDirectory
{
    public uint ExportFlags;
    public uint TimeStamp;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint NameRVA;
    public uint OrdinalBase;
    public uint AddressTableEntries;
    public uint NumberOfNamePointers;
    public uint ExportAddrTableRVA;
    public uint NamePtrRVA;
    public uint OrdinalTableRVA;
    public ExportAddressName[]? Exports;
}
