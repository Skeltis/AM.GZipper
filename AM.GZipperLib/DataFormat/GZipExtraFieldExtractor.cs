using System;
using System.Collections.Generic;
using System.Linq;

namespace AM.GZipperLib.DataFormat
{
    public static class GZipExtraFieldExtractor
    {
        private static Dictionary<Tuple<byte, byte>, Type> _register = new Dictionary<Tuple<byte, byte>, Type>()
        {
            { Tuple.Create(BlockInfoField.SI1_BlockNumber, BlockInfoField.SI2_BlockNumber), typeof(BlockInfoField) },
            { Tuple.Create(OriginalInfoField.SI1_OriginalInfo, OriginalInfoField.SI2_OriginalInfo), typeof(OriginalInfoField) },
        };

        public static List<GZipExtraField> ExtractAllFields(byte[] extraFields)
        {
            int position = 0;
            var fields = new List<GZipExtraField>();

            do
            {
                GZipExtraField gzField = ExtractField(extraFields, position);
                position += gzField.FieldTotalLength;
                fields.Add(gzField);
            }
            while (position < extraFields.Length);

            return fields;
        }

        public static GZipExtraField ExtractField(byte[] extraFields, int offset)
        {
            if (extraFields.Length < 4) throw new FormatException("Invalid gzip format");
            byte si1 = extraFields[offset];
            byte si2 = extraFields[offset + 1];
            UInt16 length = Utilities.GetUIntFromBytes<UInt16>(extraFields, offset + 2);
            if (offset + length + 4 > extraFields.Length) throw new FormatException("Inconsistent extra field");
            byte[] data = extraFields.Skip(offset + 4).Take(length).ToArray();

            var key = Tuple.Create(si1, si2);
            GZipExtraField extraFieldType = _register.ContainsKey(key) ?
                (GZipExtraField)Activator.CreateInstance(_register[key], new object[] { data }) :
                new GZipExtraField(si1, si2, data);

            return extraFieldType;
        }
    }
}
