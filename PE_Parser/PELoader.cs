using System.Text;

namespace PEParser;

public static class PELoader
{
    // Helper functions to read data from the file
    public static ushort Read16LE(BinaryReader reader)
    {
        return (ushort)(reader.ReadByte() | (reader.ReadByte() << 8));
    }

    public static uint Read32LE(BinaryReader reader) => (uint)(reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16) | (reader.ReadByte() << 24));

    public static ulong Read64LE(BinaryReader reader) => (ulong)(reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16) | (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 32) | (reader.ReadByte() << 40) | (reader.ReadByte() << 48) | (reader.ReadByte() << 56));

    public static string ReadStr(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }

    // Function to clean allocated memory inside structs
    public static void Cleanup(ExtendedDosHeader dosHeader)
    {
        dosHeader.DataDirectory = null;

        if (dosHeader.SectionTable != null)
        {
            for (int i = 0; i < dosHeader.SectionTable.Length; i++)
            {
                dosHeader.SectionTable[i].Name = null;
            }
        }

        dosHeader.ExportDirectory.Exports = null;
        dosHeader.SectionTable = null;
        dosHeader.ImportDirectory = null;
    }


    // Function to convert an RVA address to a file offset
    public static ulong RvaToOffset(uint numberOfSections, uint rva, SectionTable[] sections)
    {
        if (rva == 0) return 0;
        ulong sumAddr;

        for (uint idx = 0; idx < numberOfSections; idx++)
        {
            sumAddr = sections[idx].VirtualAddress + sections[idx].SizeOfRawData;

            if (rva >= sections[idx].VirtualAddress && (rva <= sumAddr))
            {
                return sections[idx].PointerToRawData + (rva - sections[idx].VirtualAddress);
            }
        }
        return ulong.MaxValue;
    }

    // Function to read DOS Header values from a file
    public static void ReadDOSHeader(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        // Reading DOS Header
        dosHeader.magic = Read16LE(reader);
        dosHeader.e_cblp = Read16LE(reader);
        dosHeader.e_cp = Read16LE(reader);
        dosHeader.e_crlc = Read16LE(reader);
        dosHeader.e_cparhdr = Read16LE(reader);
        dosHeader.e_minalloc = Read16LE(reader);
        dosHeader.e_maxalloc = Read16LE(reader);
        dosHeader.e_ss = Read16LE(reader);
        dosHeader.e_sp = Read16LE(reader);
        dosHeader.e_csum = Read16LE(reader);
        dosHeader.e_ip = Read16LE(reader);
        dosHeader.e_cs = Read16LE(reader);
        dosHeader.e_lfarlc = Read16LE(reader);
        dosHeader.e_ovno = Read16LE(reader);

        // some of the next fields are reserved/aren't used
        dosHeader.e_res = Read64LE(reader);
        dosHeader.e_oemid = Read16LE(reader);
        dosHeader.e_oeminfo = Read16LE(reader);
        dosHeader.e_res2 = Read64LE(reader); // this is repeated on purpose since
        dosHeader.e_res2 = Read64LE(reader); // most PE files have this field as zero
        dosHeader.e_res2 = Read32LE(reader); // i'll fix it later.
        /////////////////////////////////////////////
        dosHeader.e_lfanew = Read32LE(reader);
    }

    // Function to read PE header information
    public static void ReadPEHeader(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        reader.BaseStream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

        // PE header
        dosHeader.PE.Signature = Read32LE(reader);
        dosHeader.PE.Machine = Read16LE(reader);
        dosHeader.PE.NumberOfSections = Read16LE(reader);
        dosHeader.PE.TimeStamp = Read32LE(reader);
        dosHeader.PE.SymbolTablePointer = Read32LE(reader);
        dosHeader.PE.NumberOfSymbols = Read32LE(reader);
        dosHeader.PE.OptionalHeaderSize = Read16LE(reader);
        dosHeader.PE.Characteristics = Read16LE(reader);

        // optional header (Standard Fields)
        dosHeader.PE.OptionalHeader.Magic = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MajorLinkerVersion = reader.ReadByte();
        dosHeader.PE.OptionalHeader.MinorLinkerVersion = reader.ReadByte();
        dosHeader.PE.OptionalHeader.SizeOfCode = Read32LE(reader);
        dosHeader.PE.OptionalHeader.SizeOfInitializedData = Read32LE(reader);
        dosHeader.PE.OptionalHeader.SizeOfUninitializedData = Read32LE(reader);
        dosHeader.PE.OptionalHeader.EntryPoint = Read32LE(reader);
        dosHeader.PE.OptionalHeader.BaseOfCode = Read32LE(reader);
        if (dosHeader.PE.OptionalHeader.Magic == 0x20B)
        {
            dosHeader.PE.OptionalHeader.ImageBase = Read64LE(reader);
        }
        else
        {
            dosHeader.PE.OptionalHeader.BaseOfData = Read32LE(reader);
            dosHeader.PE.OptionalHeader.ImageBase = Read32LE(reader);
        }

        dosHeader.PE.OptionalHeader.SectionAlignment = Read32LE(reader);
        dosHeader.PE.OptionalHeader.FileAlignment = Read32LE(reader);
        dosHeader.PE.OptionalHeader.MajorOSVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MinorOSVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MajorImageVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MinorImageVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MajorSubsystemVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.MinorSubsystemVersion = Read16LE(reader);
        dosHeader.PE.OptionalHeader.Win32VersionValue = Read32LE(reader);
        dosHeader.PE.OptionalHeader.SizeOfImage = Read32LE(reader);
        dosHeader.PE.OptionalHeader.SizeOfHeaders = Read32LE(reader);
        dosHeader.PE.OptionalHeader.CheckSum = Read32LE(reader);
        dosHeader.PE.OptionalHeader.Subsystem = Read16LE(reader);
        dosHeader.PE.OptionalHeader.DllCharacteristics = Read16LE(reader);

        if (dosHeader.PE.OptionalHeader.Magic == 0x20B)
        {
            dosHeader.PE.OptionalHeader.SizeOfStackReserve = Read64LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfStackCommit = Read64LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfHeapReserve = Read64LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfHeapCommit = Read64LE(reader);
        }
        else
        {
            dosHeader.PE.OptionalHeader.SizeOfStackReserve = Read32LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfStackCommit = Read32LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfHeapReserve = Read32LE(reader);
            dosHeader.PE.OptionalHeader.SizeOfHeapCommit = Read32LE(reader);
        }
        dosHeader.PE.OptionalHeader.LoaderFlags = Read32LE(reader);
        dosHeader.PE.OptionalHeader.NumberOfRvaAndSizes = Read32LE(reader);
    }

    // Function to read Data Directories information
    public static void ReadDataDir(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        uint dirs = dosHeader.PE.OptionalHeader.NumberOfRvaAndSizes;

        // Reading Data Directories
        dosHeader.DataDirectory = new DataDirectory[dirs];

        for (int idx = 0; idx < dirs; idx++)
        {
            dosHeader.DataDirectory[idx].VirtualAddress = Read32LE(reader);
            dosHeader.DataDirectory[idx].Size = Read32LE(reader);
            // dosHeader.dataDirectory[idx].offset = RvaToOffset(dosHeader.pe.numberOfSections,
            //                               dosHeader.dataDirectory[idx].virtualAddr,
            //                               dosHeader.section_table);
        }
    }

    public static void ReadDataOffset(ExtendedDosHeader dosHeader)
    {
        uint dirs = dosHeader.PE.OptionalHeader.NumberOfRvaAndSizes;

        for (int idx = 0; idx < dirs; idx++)
        {
            dosHeader.DataDirectory![idx].Offset = (long)RvaToOffset(dosHeader.PE.NumberOfSections,
                                      dosHeader.DataDirectory![idx].VirtualAddress,
                                      dosHeader.SectionTable!);
        }
    }

    // Function to read sections information
    public static void ReadSections(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        int sections = dosHeader.PE.NumberOfSections;
        // Reading Sections data
        dosHeader.SectionTable = new SectionTable[sections];

        for (int idx = 0; idx < sections; idx++)
        {
            dosHeader.SectionTable[idx].Name = ReadStr(reader, 8);
            dosHeader.SectionTable[idx].VirtualSize = Read32LE(reader);
            dosHeader.SectionTable[idx].VirtualAddress = Read32LE(reader);
            dosHeader.SectionTable[idx].SizeOfRawData = Read32LE(reader);
            dosHeader.SectionTable[idx].PointerToRawData = Read32LE(reader);
            dosHeader.SectionTable[idx].PointerToRelocation = Read32LE(reader);
            dosHeader.SectionTable[idx].PointerToLineNumbers = Read32LE(reader);
            dosHeader.SectionTable[idx].NumberOfRelocation = Read16LE(reader);
            dosHeader.SectionTable[idx].NumberOfLineNumbers = Read16LE(reader);
            dosHeader.SectionTable[idx].Characteristics = Read32LE(reader);
        }
    }

    // Function to read Export directory information
    public static void ReadExportDir(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        uint offset;

        offset = (uint)dosHeader.DataDirectory![0].Offset;

        if (offset == uint.MaxValue) return;

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        dosHeader.ExportDirectory.ExportFlags = Read32LE(reader);
        dosHeader.ExportDirectory.TimeStamp = Read32LE(reader);
        dosHeader.ExportDirectory.MajorVersion = Read16LE(reader);
        dosHeader.ExportDirectory.MinorVersion = Read16LE(reader);
        dosHeader.ExportDirectory.NameRVA = Read32LE(reader);
        dosHeader.ExportDirectory.OrdinalBase = Read32LE(reader);
        dosHeader.ExportDirectory.AddressTableEntries = Read32LE(reader);
        dosHeader.ExportDirectory.NumberOfNamePointers = Read32LE(reader);
        dosHeader.ExportDirectory.ExportAddrTableRVA = Read32LE(reader);
        dosHeader.ExportDirectory.NamePtrRVA = Read32LE(reader);
        dosHeader.ExportDirectory.OrdinalTableRVA = Read32LE(reader);

        ReadExportNames(reader, dosHeader);
    }

    // Function to read the ascii names of exported functions
    public static void ReadExportNames(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        uint tableOffset;
        uint nameOffset;
        uint nameRVA;
        uint tableSize;
        var buffer = new char[8192];

        tableSize = dosHeader.ExportDirectory.NumberOfNamePointers;
        tableOffset = (uint)RvaToOffset(dosHeader.PE.NumberOfSections,
                                       dosHeader.ExportDirectory.NamePtrRVA,
                                       dosHeader.SectionTable!);
        dosHeader.ExportDirectory.Exports = new ExportAddressName[tableSize];

        // reading Import table entries (per DLL)
        for (uint idx = 0; idx < tableSize; idx++)
        {
            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
            nameRVA = Read32LE(reader);
            nameOffset = (uint)RvaToOffset(dosHeader.PE.NumberOfSections,
                  nameRVA, dosHeader.SectionTable!);
            reader.BaseStream.Seek(nameOffset, SeekOrigin.Begin);
            reader.Read(buffer, 0, buffer.Length);
            var pe = Array.IndexOf(buffer, '\0'); // find the first null terminator
            var name = new string(buffer);
            if (pe >= 0)
            {
                name = name.Substring(0, pe); // trim the string to the null terminator
            }
            dosHeader.ExportDirectory.Exports[idx].Names = name;

            tableOffset += 4; // after reading 4 bytes, jump to next 4 bytes
        }
    }

    // Function to read the imports table entries
    public static void ReadImportDir(BinaryReader reader, ExtendedDosHeader dosHeader)
    {
        uint tableEntries;

        // each import entry has 5 fields, 4 bytes per field (20 bytes per entry)
        // minus 1 because the final table will be empty signaling the end of entries
        tableEntries = (dosHeader.DataDirectory![1].Size / 20) - 1;
        reader.BaseStream.Seek(dosHeader.DataDirectory[1].Offset, SeekOrigin.Begin);

        dosHeader.ImportDirectory = new ImportDirectory[tableEntries];

        for (uint idx = 0; idx < tableEntries; idx++)
        {
            dosHeader.ImportDirectory[idx].ImportLookupTableRVA = Read32LE(reader);
            dosHeader.ImportDirectory[idx].TimeStamp = Read32LE(reader);
            dosHeader.ImportDirectory[idx].ForwarderChain = Read32LE(reader);
            dosHeader.ImportDirectory[idx].NameRVA = Read32LE(reader);
            dosHeader.ImportDirectory[idx].ImportAddressRVA = Read32LE(reader);
        }
    }
    public static ExtendedDosHeader LoadFrom(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs, System.Text.Encoding.ASCII);
        ExtendedDosHeader dosHeader = new();

        // read headers
        ReadDOSHeader(reader, dosHeader);
        ReadPEHeader(reader, dosHeader);
        ReadDataDir(reader, dosHeader);
        ReadSections(reader, dosHeader);
        ReadDataOffset(dosHeader);
        ReadExportDir(reader, dosHeader);
        ReadImportDir(reader, dosHeader);

        return dosHeader;
    }

    // read16_le(): reads a 16bit little-endian integer
    // arguments: a StreamReader to read from
    // return: a 16 bit integer
    public static ushort Read16Le(StreamReader inStream)
    {
        ushort value = (byte)inStream.Read();
        value |= (ushort)(inStream.Read() << 8);
        return value;
    }

    // read32_le(): reads a 32bit little-endian integer
    // arguments: a StreamReader to read from
    // return: a 32 bit integer
    public static uint Read32Le(StreamReader inStream)
    {
        uint value = (byte)inStream.Read();
        value |= (uint)(inStream.Read() << 8);
        value |= (uint)(inStream.Read() << 16);
        value |= (uint)(inStream.Read() << 24);
        return value;
    }

    // read64_le(): reads a 64bit little-endian integer
    // arguments: a StreamReader to read from
    // return: a 64 bit integer
    public static ulong Read64Le(StreamReader inStream)
    {
        ulong value = (byte)inStream.Read();
        value |= ((ulong)inStream.Read() << 8);
        value |= ((ulong)inStream.Read() << 16);
        value |= ((ulong)inStream.Read() << 24);
        value |= ((ulong)inStream.Read() << 32);
        value |= ((ulong)inStream.Read() << 40);
        value |= ((ulong)inStream.Read() << 48);
        value |= ((ulong)inStream.Read() << 56);
        return value;
    }

    // read8_le(): reads an 8bit integer
    // arguments: a StreamReader to read from
    // return: an 8 bit integer
    public static byte Read8Le(StreamReader inStream) => (byte)inStream.Read();
    // read_str(): reads a 'count' of characters from a file
    // arguments: StreamReader to read from, count of characters to read
    // returns: string of characters.
    public static string ReadStr(StreamReader inStream, int count)
    {
        var chArray = new char[count];
        inStream.Read(chArray, 0, count);
        return new string(chArray);
    }
}