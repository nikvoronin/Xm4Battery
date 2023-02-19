using System.Management;

namespace WmiPnp
{
    public static class CimConvertExtension
    {
        private readonly static IDictionary<CimType, Type> CimToTypeMap =
            new Dictionary<CimType, Type> {
                {CimType.Boolean, typeof (bool)},
                {CimType.Char16, typeof (string)},
                {CimType.DateTime, typeof (DateTime)},
                {CimType.Object, typeof (object)},
                {CimType.Real32, typeof (decimal)},
                {CimType.Real64, typeof (decimal)},
                {CimType.Reference, typeof (object)},
                {CimType.SInt16, typeof (short)},
                {CimType.SInt32, typeof (int)},
                {CimType.SInt8, typeof (sbyte)},
                {CimType.String, typeof (string)},
                {CimType.UInt8, typeof (byte)},
                {CimType.UInt16, typeof (ushort)},
                {CimType.UInt32, typeof (uint)},
                {CimType.UInt64, typeof (ulong)}
            };

        public static Type PlatformType( this PropertyData data )
        {
            Type type = CimToTypeMap[data.Type];
            if ( data.IsArray )
                type = type.MakeArrayType();

            return type;
        }

        public static object SystemValue( this PropertyData data )
        {
            Type type = PlatformType( data );

            if ( data.Type == CimType.DateTime )
                return ManagementDateTimeConverter.ToDateTime( data.Value.ToString() );

            return Convert.ChangeType( data.Value, type );
        }
    }
}
