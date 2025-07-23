namespace SharpDemangler.Microsoft;

public enum SpecialIntrinsicKind : int
{
    None,
    Vftable,
    Vbtable,
    Typeof,
    VcallThunk,
    LocalStaticGuard,
    StringLiteralSymbol,
    UdtReturning,
    Unknown,
    DynamicInitializer,
    DynamicAtexitDestructor,
    RttiTypeDescriptor,
    RttiBaseClassDescriptor,
    RttiBaseClassArray,
    RttiClassHierarchyDescriptor,
    RttiCompleteObjLocator,
    LocalVftable,
    LocalStaticThreadGuard
}
