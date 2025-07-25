namespace PEParser;

public struct OptionalHeader
{
    public ushort magic;
    public byte majorLinkerVer;
    public byte minorLinkerVer;
    public uint sizeOfCode;
    public uint sizeOfInitializedData;
    public uint sizeOfUninitializedData;
    public uint entryPoint;
    public uint baseOfCode;
    public uint baseOfData;
    public ulong imageBase;
    public uint sectionAlignment;
    public uint fileAlignment;
    public ushort majorOSVer;
    public ushort minorOSVer;
    public ushort majorImageVer;
    public ushort minorImageVer;
    public ushort majorSubsystemVer;
    public ushort minorSubsystemVer;
    public uint win32VersionVal;
    public uint sizeOfImage;
    public uint sizeOfHeaders;
    public uint checkSum;
    public ushort subsystem;
    public ushort dllCharacteristics;
    public ulong sizeOfStackReserve;
    public ulong sizeOfStackCommit;
    public ulong sizeOfHeapReserve;
    public ulong sizeOfHeapCommit;
    public uint loaderFlags;
    public uint numberOfRvaAndSizes;
}
