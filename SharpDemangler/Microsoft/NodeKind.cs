namespace SharpDemangler.Microsoft;

public enum NodeKind : int
{
    Unknown,
    Md5Symbol,
    PrimitiveType,
    FunctionSignature,
    Identifier,
    NamedIdentifier,
    VcallThunkIdentifier,
    LocalStaticGuardIdentifier,
    IntrinsicFunctionIdentifier,
    ConversionOperatorIdentifier,
    DynamicStructorIdentifier,
    StructorIdentifier,
    LiteralOperatorIdentifier,
    ThunkSignature,
    PointerType,
    TagType,
    ArrayType,
    Custom,
    IntrinsicType,
    NodeArray,
    QualifiedName,
    TemplateParameterReference,
    EncodedStringLiteral,
    IntegerLiteral,
    RttiBaseClassDescriptor,
    LocalStaticGuardVariable,
    FunctionSymbol,
    VariableSymbol,
    SpecialTableSymbol
}
