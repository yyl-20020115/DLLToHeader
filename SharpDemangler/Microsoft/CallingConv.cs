namespace SharpDemangler.Microsoft;

public enum CallingConv : int
{
    None,
    Cdecl,
    Pascal,
    Thiscall,
    Stdcall,
    Fastcall,
    Clrcall,
    Eabi,
    Vectorcall,
    Regcall
}
