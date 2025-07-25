namespace PEParser;

public static class PEHelper
{
    // load_file(): loads and reads pe files in current directory
    // arguments: integer representing argument count, and a string array
    // return: none
    public static void ParseAndPrint(string[] argv)
    {        
        for (int idx = 1; idx <= argv.Length; idx++)
        {
            using var fs = new FileStream(argv[idx - 1], FileMode.Open, FileAccess.Read);
            if (fs == null)
            {
                Console.WriteLine($"Can't open '{argv[idx]}' file, exiting");
                continue;
            }

            using var reader = new BinaryReader(fs);
            var dosHeader = new ExtendedDosHeader();

            // read headers
            PELoader.ReadDOSHeader(reader,  dosHeader);
            PELoader.ReadPEHeader(reader,  dosHeader);

            // making sure we have a valid/standard pe file
            if (dosHeader.PE.Signature != 0x4550)
            {
                Console.WriteLine("invalid pe signature, file is likely corrupt pe, or not a valid pe file.");
                return;
            }

            PELoader.ReadDataDir(reader,  dosHeader);
            PELoader.ReadSections(reader,  dosHeader);
            PELoader.ReadDataOffset( dosHeader);
            PELoader.ReadExportDir(reader,  dosHeader);
            PELoader.ReadImportDir(reader,  dosHeader);

            // test printing information
            Console.WriteLine($"Parsing File: {argv[idx - 1]} \n");

            PrintHeaders( dosHeader);
            PrintDataTables( dosHeader);
            PrintSections(dosHeader);
            PrintExports( dosHeader);
            PrintImports( dosHeader);

            // cleanup
            PELoader.Cleanup( dosHeader);
        }
    }

    // print_dataTables(): prints a list of data tables in a pe file
    // arguments: a dos_header_t object
    // return: none
    public static void PrintDataTables(ExtendedDosHeader dosHeader)
    {
        // Data Directories Types
        string[] dataTable = [ "Export Table", "Import Table",
                               "Resource Table", "Exception Table",
                               "Certificate ", "Base Relocation",
                               "Debug Table", "Architecture",
                               "Global Ptr Table", "TLS Table",
                               "Load Config ", "Bound Import",
                               "Import Address", "Delay Import Desc.",
                               "CLR Runtime Header", "Reserved, must be zero" ];

        uint offset, vAddress, sections, tables;
        sections = dosHeader.PE.NumberOfSections;

        tables = dosHeader.PE.OptionalHeader.NumberOfRvaAndSizes;

        Console.WriteLine("\nData Tables: ");
        for (int idx = 0; idx < tables; idx++)
        {
            vAddress = dosHeader.DataDirectory![idx].VirtualAddress;

            // skipping empty directories
            if (vAddress == 0) continue;

            Console.WriteLine($"  {dataTable[idx]}: ");

            offset = (uint)PELoader.RvaToOffset(sections, vAddress, dosHeader.SectionTable!);

            Console.WriteLine($"     Address: 0x{vAddress:X} \tOffset: 0x{offset:X}");
            Console.WriteLine($"        Size: 0x{dosHeader.DataDirectory[idx].Size:X} ");
        }
    }

    // Function to print DLL characteristics
    public static void PrintDllCharacteristics(ushort ch)
    {
        string[] image_dll_str = [
            "IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA",
        "IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE",
        "IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY",
        "IMAGE_DLLCHARACTERISTICS_NX_COMPAT",
        "IMAGE_DLLCHARACTERISTICS_NO_ISOLATION",
        "IMAGE_DLLCHARACTERISTICS_NO_SEH",
        "IMAGE_DLLCHARACTERISTICS_NO_BIND",
        "IMAGE_DLLCHARACTERISTICS_APPCONTAINER",
        "IMAGE_DLLCHARACTERISTICS_WDM_DRIVER",
        "IMAGE_DLLCHARACTERISTICS_GUARD_CF",
        "IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE"
        ];

        ushort[] image_dll_arr = [
            0x0020, 0x0040, 0x0080, 0x0100, 0x0200, 0x0400, 0x0800, 0x1000,
        0x2000, 0x4000, 0x8000
        ];

        for (int idx = 0; idx < 11; idx++)
        {
            if ((ch & image_dll_arr[idx]) != 0)
                Console.WriteLine($"     {image_dll_str[idx]}");
        }
    }

    // print_exports(): prints a list of exports in a pe file
    // arguments: a dos_header_t object
    // return: none
    public static void PrintExports(ExtendedDosHeader dosHeader)
    {
        Console.WriteLine("\nExport Directory ");
        Console.WriteLine($"    Flags:           0x{dosHeader.ExportDirectory.ExportFlags:X}");
        Console.WriteLine($"    TimeStamp:       0x{dosHeader.ExportDirectory.TimeStamp:X}");
        Console.WriteLine($"    MajorVersion:    0x{dosHeader.ExportDirectory.MajorVersion:X}");
        Console.WriteLine($"    MinorVersion:    0x{dosHeader.ExportDirectory.MinorVersion:X}");
        Console.WriteLine($"    Name RVA:        0x{dosHeader.ExportDirectory.NameRVA:X}");
        Console.WriteLine($"    OrdinalBase:     0x{dosHeader.ExportDirectory.OrdinalBase:X}");
        Console.WriteLine($"    AddressTable Entries:  0x{dosHeader.ExportDirectory.AddressTableEntries:X}");
        Console.WriteLine($"    NumberOfNames:         0x{dosHeader.ExportDirectory.NumberOfNamePointers:X}");
        Console.WriteLine($"    ExportTable Entries:   0x{dosHeader.ExportDirectory.ExportAddrTableRVA:X}");
        Console.WriteLine($"    AddressOfNames:        0x{dosHeader.ExportDirectory.NamePtrRVA:X}");
        Console.WriteLine($"    OrdinalTable RVA:      0x{dosHeader.ExportDirectory.OrdinalTableRVA:X}");

        Console.WriteLine("\nExported functions: ");

        // skipping none IMAGE_FILE_DLL
        if ((dosHeader.PE.Characteristics & 0x2000) == 0) return;

        for (int i = 0; i < dosHeader.ExportDirectory.NumberOfNamePointers; i++)
        {
            Console.WriteLine($"   {dosHeader.ExportDirectory.Exports![i].Names}");
        }
    }

    // print_headers(): prints the values of a DOS header object
    // arguments: a dos_header_t object
    // return: none
    public static void PrintHeaders(ExtendedDosHeader dosHeader)
    {
        Console.WriteLine($"magic bytes: \t\t{(char)(0xff & dosHeader.magic)}{(char)(dosHeader.magic >> 8)}");
        Console.WriteLine($"pe Offset    \t\t{dosHeader.e_lfanew:X}");

        Console.WriteLine("\nPE header information");
        Console.WriteLine($" signature:   \t\t0x{dosHeader.PE.Signature:X} {(char)(0xff & dosHeader.PE.Signature)}{(char)(0xff & (dosHeader.PE.Signature >> 8))} ");
        Console.Write(" Machine:  \t\t");
        PEHelper.PrintMachine(dosHeader.PE.Machine);
        Console.WriteLine($" Sections: \t\t{dosHeader.PE.NumberOfSections}");
        Console.WriteLine($" Time Stamp: \t\t0x{dosHeader.PE.TimeStamp:X}");
        Console.WriteLine($" Symbol Table Pointer:  0x{dosHeader.PE.SymbolTablePointer:X}");
        Console.WriteLine($" Symbols:               {dosHeader.PE.NumberOfSymbols}");
        Console.WriteLine($" optionalHeader Size:    {dosHeader.PE.OptionalHeaderSize} (0x{dosHeader.PE.OptionalHeaderSize:X})");
        Console.WriteLine($" characteristics:       0x{dosHeader.PE.Characteristics:X}");
        PEHelper.PrintPeCharacteristics(dosHeader.PE.Characteristics);

        Console.WriteLine("\nOptional Header");
        Console.Write("magic:      ");
        PEHelper.PrintMagic(dosHeader.PE.OptionalHeader.Magic);
        Console.WriteLine($"MajorLinkerVersion:      0x{dosHeader.PE.OptionalHeader.MajorLinkerVersion:X}");
        Console.WriteLine($"MinorLinkerVersion:      0x{dosHeader.PE.OptionalHeader.MinorLinkerVersion:X}");
        Console.WriteLine($"SizeOfCode:              0x{dosHeader.PE.OptionalHeader.SizeOfCode:X}");
        Console.WriteLine($"SizeOfInitializedData:   0x{dosHeader.PE.OptionalHeader.SizeOfInitializedData:X}");
        Console.WriteLine($"SizeOfUninitializedData: 0x{dosHeader.PE.OptionalHeader.SizeOfUninitializedData:X}");
        Console.WriteLine($"EntryPoint:              0x{dosHeader.PE.OptionalHeader.EntryPoint:X}");
        Console.WriteLine($"BaseOfCode:              0x{dosHeader.PE.OptionalHeader.BaseOfCode:X}");
        if (dosHeader.PE.OptionalHeader.Magic == 0x10b)
        {
            Console.WriteLine($"BaseOfData:              0x{dosHeader.PE.OptionalHeader.BaseOfData:X}");
        }
        Console.WriteLine($"ImageBase:               {dosHeader.PE.OptionalHeader.ImageBase-1:X}");
        Console.WriteLine($"SectionAlignment:        0x{dosHeader.PE.OptionalHeader.SectionAlignment:X}");
        Console.WriteLine($"FileAlignment:           0x{dosHeader.PE.OptionalHeader.FileAlignment:X}");
        Console.WriteLine($"MajorOSVersion:          0x{dosHeader.PE.OptionalHeader.MajorOSVersion:X}");
        Console.WriteLine($"MinorOSVersion:          0x{dosHeader.PE.OptionalHeader.MinorOSVersion:X}");
        Console.WriteLine($"MajorImageVersion:       0x{dosHeader.PE.OptionalHeader.MajorImageVersion:X}");
        Console.WriteLine($"MinorImageVersion:       0x{dosHeader.PE.OptionalHeader.MinorImageVersion:X}");
        Console.WriteLine($"MajorSubsysVersion:      0x{dosHeader.PE.OptionalHeader.MajorSubsystemVersion:X}");
        Console.WriteLine($"MinorSubsysVersion:      0x{dosHeader.PE.OptionalHeader.MinorSubsystemVersion:X}");
        Console.WriteLine($"Win32VersionValue:       0x{dosHeader.PE.OptionalHeader.Win32VersionValue:X}");
        Console.WriteLine($"SizeOfImage:             0x{dosHeader.PE.OptionalHeader.SizeOfImage:X}");
        Console.WriteLine($"SizeOfHeaders:           0x{dosHeader.PE.OptionalHeader.SizeOfHeaders:X}");
        Console.WriteLine($"CheckSum:                0x{dosHeader.PE.OptionalHeader.CheckSum:X}");
        Console.Write("Subsystem:             ");
        PEHelper.PrintSubsystem(dosHeader.PE.OptionalHeader.Subsystem);
        Console.WriteLine("DllCharacteristics:           ");
        PEHelper.PrintDllCharacteristics(dosHeader.PE.OptionalHeader.DllCharacteristics);

        Console.WriteLine($"SizeOfStackReserve:      {dosHeader.PE.OptionalHeader.SizeOfStackReserve:X}");
        Console.WriteLine($"SizeOfStackCommit:       {dosHeader.PE.OptionalHeader.SizeOfStackCommit:X}");
        Console.WriteLine($"SizeOfHeapReserve:       {dosHeader.PE.OptionalHeader.SizeOfHeapReserve:X}");
        Console.WriteLine($"SizeOfHeapCommit:        {dosHeader.PE.OptionalHeader.SizeOfHeapCommit:X}");

        Console.WriteLine($"LoaderFlags:             0x{dosHeader.PE.OptionalHeader.LoaderFlags:X}");
        Console.WriteLine($"NumberOfRvaAndSizes:     {dosHeader.PE.OptionalHeader.NumberOfRvaAndSizes}");
    }

    // print_imports(): prints a list of imports in a pe file
    // arguments: a dos_header_t object
    // return: none
    public static void PrintImports(ExtendedDosHeader dosHeader)
    {
        uint? tableEntries;

        tableEntries = (dosHeader.DataDirectory![1].Size / 20) - 1;
        Console.WriteLine("\nImport Directory ");

        for (uint idx = 0; idx < tableEntries; idx++)
        {
            Console.WriteLine($"  Import Lookup table RVA: {dosHeader.ImportDirectory![idx].ImportLookupTableRVA:X}");
            Console.WriteLine($"  Time Stamp:              {dosHeader.ImportDirectory[idx].TimeStamp:X}");
            Console.WriteLine($"  Forwarder Chain:         {dosHeader.ImportDirectory[idx].ForwarderChain:X}");
            Console.WriteLine($"  Name RVA:                {dosHeader.ImportDirectory[idx].NameRVA:X}");
            Console.WriteLine($"  Import Address table RVA: {dosHeader.ImportDirectory[idx].ImportAddressRVA:X}");
        }
    }

    // Function to print the machine type of a PE image
    public static void PrintMachine(ushort mach)
    {
        switch (mach)
        {
            case 0x0000:
                Console.WriteLine("(0000)  IMAGE_FILE_MACHINE_UNKNOWN");
                break;
            case 0x0200:
                Console.WriteLine("(0200)  IMAGE_FILE_MACHINE_IA64");
                break;
            case 0x014C:
                Console.WriteLine("(014C)  IMAGE_FILE_MACHINE_I386");
                break;
            case 0x8664:
                Console.WriteLine("(8664)  IMAGE_FILE_MACHINE_AMD64");
                break;
            case 0x01C0:
                Console.WriteLine("(01C0)  IMAGE_FILE_MACHINE_ARM");
                break;
            case 0xAA64:
                Console.WriteLine("(AA64)  IMAGE_FILE_MACHINE_ARM64");
                break;
            case 0x01C4:
                Console.WriteLine("(01C4)  IMAGE_FILE_MACHINE_ARMNT");
                break;
            case 0x0EBC:
                Console.WriteLine("(0EBC)  IMAGE_FILE_MACHINE_EBC");
                break;
            default:
                break;
        }
    }

    // Function to print the type of a PE image
    public static void PrintMagic(ushort magic)
    {
        switch (magic)
        {
            case 0x10B:
                Console.WriteLine("10B (PE)");
                break;
            case 0x20B:
                Console.WriteLine("20B (PE+)");
                break;
            default:
                Console.WriteLine("0 (Error)");
                break;
        }
    }

    // Function to print PE characteristics
    public static void PrintPeCharacteristics(ushort ch)
    {
        string[] image_file_str = [
            "IMAGE_FILE_RELOCS_STRIPPED", "IMAGE_FILE_EXECUTABLE_IMAGE",
        "IMAGE_FILE_LINE_NUMS_STRIPPED", "IMAGE_FILE_LOCAL_SYMS_STRIPPED",
        "IMAGE_FILE_AGGRESSIVE_WS_TRIM", "IMAGE_FILE_LARGE_ADDRESS_AWARE",
        "IMAGE_FILE_BYTES_REVERSED_LO", "IMAGE_FILE_32BIT_MACHINE",
        "IMAGE_FILE_DEBUG_STRIPPED", "IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP",
        "IMAGE_FILE_NET_RUN_FROM_SWAP", "IMAGE_FILE_SYSTEM", "IMAGE_FILE_DLL",
        "IMAGE_FILE_UP_SYSTEM_ONLY", "IMAGE_FILE_BYTES_REVERSED_HI"
        ];

        ushort[] image_file_arr = [
            0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0080, 0x0100,
            0x0200, 0x0400, 0x0800, 0x1000, 0x2000, 0x4000, 0x8000
        ];

        for (int idx = 0; idx < 15; idx++)
        {
            if ((ch & image_file_arr[idx]) != 0)
                Console.WriteLine($"     {image_file_str[idx]}");
        }
    }

    // Function to print the flags set on a section
    public static void PrintSectionCharacteristics(uint ch)
    {
        string[] section_flags_str = [
            "IMAGE_SCN_TYPE_NO_PAD", "IMAGE_SCN_CNT_CODE", "IMAGE_SCN_CNT_INITIALIZED_DATA",
            "IMAGE_SCN_CNT_UNINITIALIZED_ DATA", "IMAGE_SCN_LNK_OTHER", "IMAGE_SCN_LNK_INFO",
            "IMAGE_SCN_LNK_REMOVE", "IMAGE_SCN_LNK_COMDAT", "IMAGE_SCN_GPREL", "IMAGE_SCN_MEM_PURGEABLE",
            "IMAGE_SCN_MEM_16BIT", "IMAGE_SCN_MEM_LOCKED", "IMAGE_SCN_MEM_PRELOAD", "IMAGE_SCN_ALIGN_1BYTES",
            "IMAGE_SCN_ALIGN_2BYTES", "IMAGE_SCN_ALIGN_4BYTES", "IMAGE_SCN_ALIGN_8BYTES", "IMAGE_SCN_ALIGN_16BYTES",
            "IMAGE_SCN_ALIGN_32BYTES", "IMAGE_SCN_ALIGN_64BYTES", "IMAGE_SCN_ALIGN_128BYTES", "IMAGE_SCN_ALIGN_256BYTES",
            "IMAGE_SCN_ALIGN_512BYTES", "IMAGE_SCN_ALIGN_1024BYTES", "IMAGE_SCN_ALIGN_2048BYTES", "IMAGE_SCN_ALIGN_4096BYTES",
            "IMAGE_SCN_ALIGN_8192BYTES", "IMAGE_SCN_LNK_NRELOC_OVFL", "IMAGE_SCN_MEM_DISCARDABLE", "IMAGE_SCN_MEM_NOT_CACHED",
            "IMAGE_SCN_MEM_NOT_PAGED", "IMAGE_SCN_MEM_SHARED", "IMAGE_SCN_MEM_EXECUTE", "IMAGE_SCN_MEM_READ", "IMAGE_SCN_MEM_WRITE"
        ];

        uint[] section_flags_arr = [
            0x00000008, 0x00000020, 0x00000040, 0x00000080, 0x00000100, 0x00000200, 0x00000800, 0x00001000,
            0x00008000, 0x00020000, 0x00020000, 0x00040000, 0x00080000, 0x00100000, 0x00200000, 0x00300000,
            0x00400000, 0x00500000, 0x00600000, 0x00700000, 0x00800000, 0x00900000, 0x00A00000, 0x00B00000,
            0x00C00000, 0x00D00000, 0x00E00000, 0x01000000, 0x02000000, 0x04000000, 0x08000000, 0x10000000,
            0x20000000, 0x40000000, 0x80000000
        ];

        for (int i = 0; i < 35; i++)
        {
            if ((ch & section_flags_arr[i]) != 0)
            {
                Console.WriteLine($"          {section_flags_str[i]}");
            }
        }
    }
    // print_sections(): prints pe sections info
    // arguments: a dos_header_t object
    // return: none
    public static void PrintSections(ExtendedDosHeader dosHeader)
    {
        var sections = dosHeader.SectionTable;
        Console.WriteLine("\nSections: ");

        for (int idx = 0; idx < dosHeader.PE.NumberOfSections; idx++)
        {
            Console.WriteLine($"   Name: {sections![idx].Name}");
            Console.WriteLine($"       VirtualAddress:        {sections[idx].VirtualAddress:X}");
            Console.WriteLine($"       VirtualSize:           {sections[idx].VirtualSize:X}");
            Console.WriteLine($"       SizeOfRawData:         {sections[idx].SizeOfRawData:X}");
            Console.WriteLine($"       PointerToRawData:      {sections[idx].PointerToRawData:X}");
            Console.WriteLine($"       PointerToRelocations:  {sections[idx].PointerToRelocation:X}");
            Console.WriteLine($"       PointerToLineNumbers:  {sections[idx].PointerToLineNumbers:X}");
            Console.WriteLine($"       NumberOfRelocations:   {sections[idx].NumberOfRelocation:X}");
            Console.WriteLine($"       NumberOfLineNumbers:   {sections[idx].NumberOfLineNumbers:X}");
            Console.WriteLine($"       characteristics:       {sections[idx].Characteristics:X}");
            PEHelper.PrintSectionCharacteristics(sections[idx].Characteristics);
        }
    }

    // Function to print the subsystem of a PE
    public static void PrintSubsystem(ushort system)
    {
        switch (system)
        {
            case 0x0000:
                Console.WriteLine("  (0000)   IMAGE_SUBSYSTEM_UNKNOWN");
                break;
            case 0x0001:
                Console.WriteLine("  (0001)   IMAGE_SUBSYSTEM_NATIVE");
                break;
            case 0x0002:
                Console.WriteLine("  (0002)   IMAGE_SUBSYSTEM_WINDOWS_GUI");
                break;
            case 0x0003:
                Console.WriteLine("  (0003)   IMAGE_SUBSYSTEM_WINDOWS_CUI");
                break;
            case 0x0005:
                Console.WriteLine("     IMAGE_SUBSYSTEM_OS2_CUI");
                break;
            case 0x0007:
                Console.WriteLine("     IMAGE_SUBSYSTEM_POSIX_CUI");
                break;
            case 0x0008:
                Console.WriteLine("     IMAGE_SUBSYSTEM_NATIVE_WINDOWS");
                break;
            case 0x0009:
                Console.WriteLine("     IMAGE_SUBSYSTEM_WINDOWS_CE_GUI");
                break;
            case 0x000A:
                Console.WriteLine("     IMAGE_SUBSYSTEM_EFI_APPLICATION");
                break;
            case 0x000B:
                Console.WriteLine("     IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER");
                break;
            case 0x000C:
                Console.WriteLine("     IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER");
                break;
            case 0x000D:
                Console.WriteLine("     IMAGE_SUBSYSTEM_EFI_ROM");
                break;
            case 0x0010:
                Console.WriteLine("     IMAGE_SUBSYSTEM_XBOX");
                break;
            case 0x0014:
                Console.WriteLine("     IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION");
                break;
            default:
                break;
        }
    }
}