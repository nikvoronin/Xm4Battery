namespace WmiPnp;

/// <summary>
/// <para>The type of the Data property. The list is not exhaustive!</para>
/// <para>See also: <see cref="Reserved"/> extension method.</para>
/// <para><see href="https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpdeviceproperty"/></para>
/// </summary>
public enum DataType : uint
{
    Empty = 0,
    Null = 1,
    SByte = 2,
    Byte = 3,
    Int16 = 4,
    UInt16 = 5,
    Int32 = 6,
    Uint32 = 7,
    Int64 = 8,
    UInt64 = 9,
    Float = 10,
    Double = 11,
    Decimal = 12,
    Guid = 13,
    Currency = 14,
    Date = 15,
    FileTime = 16,
    Boolean = 17,
    String = 18,
    SecurityDescriptor = 19,
    SecurityDescriptorString = 20,
    DEVPROPKEY = 21,
    DEVPROPTYPE = 22,
    Error = 23,
    NTStatus = 24,
    StringIndirect = 25,
    Reserved = 26,
    // Reserved 26..4097
    SByteArray = 4098,
    Binary = 4099,
    Int16Array = 4100,
    UInt16Array = 4101,
    Int64Array = 4102,
    UInt64Array = 4103,
    FloatArray = 4104,
    DoubleArray = 4105,
    DecimalArray = 4106,
    GuidArray = 4107,
    CurrencyArray = 4108,
    DateArray = 4109,
    FileTimeArray = 4110,
    BooleanArray = 4111,
    StringList = 4112,
    SecurityDescriptorList = 4113,
    SecurityDescriptorStringList = 8210,
    DEVPROPKEYArray = 8211,
    DEVPROPTYPEArray = 8212,
    ErrorArray = 4117,
    NTStatusArray = 4118,
    StringIndirectList = 4119,
    Unknown = 4120,
    TBD = 8217,
    // Reserved 8218..4294967295
}
