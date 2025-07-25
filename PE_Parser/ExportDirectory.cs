namespace PEParser;

public struct ExportDirectory
{
    public uint exportFlags;
    public uint timeStamp;
    public ushort majorVer;
    public ushort minorVer;
    public uint nameRVA;
    public uint ordinalBase;
    public uint addrTableEntries;
    public uint numberOfNamePointers;
    public uint exportAddrTableRVA;
    public uint namePtrRVA;
    public uint ordinalTableRVA;
    public ExportAddressName[]? exportAddr_name_t;
}
