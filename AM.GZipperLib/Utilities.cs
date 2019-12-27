using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AM.GZipperLib
{
    public static class Utilities
    {
        public static T GetUIntFromBytes<T>(byte[] buffer, Int32 startPosition) where T : struct
        {
            UInt64 value = 0;
            for (Int32 i = 0; i < Marshal.SizeOf(default(T)); i++)
            {
                value |= ((UInt64)buffer[startPosition + i] << i * 8);
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static byte[] GetBytesOfUInt<T>(T value) where T : struct
        {
            var result = new List<byte>();
            UInt64 convValue = (UInt64)Convert.ChangeType(value, typeof(UInt64));
            Int32 typeLength = Marshal.SizeOf(default(T));
            for (Int32 i = 0; i < typeLength; i++)
            {
                byte current = (byte)(convValue << (typeLength - 1 - i) * 8 >> (typeLength - 1) * 8);
                result.Add(current);
            }
            return result.ToArray();
        }

        public static DateTime FromUnixStamp(UInt32 seconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) + TimeSpan.FromSeconds(seconds);
        }

        public static Int32 ToUnixTimeStamp(DateTime timeStamp)
        {
            return (int)(timeStamp - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static Int32 ReadNullTerminatedString(Stream stream, Int32 start, out string ntString)
        {
            var accumulatedBytes = new List<byte>();

            stream.Seek(start, SeekOrigin.Begin);
            byte currentByte = 0x00;
            do
            {
                currentByte = (byte)stream.ReadByte();
                accumulatedBytes.Add(currentByte);
            }
            while (currentByte != 0x00);

            ntString = Encoding.Default.GetString(accumulatedBytes.ToArray(), 0, accumulatedBytes.Count - 1);
            return accumulatedBytes.Count;
        }

        public static Int32 ReadNullTerminatedString(byte[] data, Int32 start, out string ntString)
        {
            using (var ms = new MemoryStream(data))
            {
                return ReadNullTerminatedString(ms, start, out ntString);
            }
        }

        public static byte[] StringToNullTerminatedBytes(string inputString)
        {
            var list = new List<byte>();

            list.AddRange(Encoding.Default.GetBytes(inputString));
            list.Add(0x00);

            return list.ToArray();
        }
    }
}
