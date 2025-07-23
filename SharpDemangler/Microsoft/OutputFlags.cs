using System;

namespace SharpDemangler.Microsoft;

[Flags]
public enum OutputFlags
{
    Default = 0,
    NoCallingConvention = 1 << 0,
    NoTagSpecifier = 1 << 1
}
