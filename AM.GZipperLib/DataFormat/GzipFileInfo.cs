using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AM.GZipperLib.DataFormat
{
    [Flags]
    public enum GFlags { FTEXT = 1, FHCRC = 2, FEXTRA = 4, FNAME = 8, FCOMMENT = 16 }
    public enum CompressionTypes { DEFLATE = 8 }
    public enum GOperatingSystem { FAT, Amiga, VMS, Unix, VM_CMS, AtariTOS, HPFS, Macintosh, Z_System, CP_M, TOPS_20, NTFS, QDOS, AcornRISCOS, unknown = 255}
    public enum DeflateAlgorithm { MaxCompression = 2, Fastest = 4 }

    public class GzipFileInfo
    {
        #region Format Constants
        public const Int32 HEADER_MINIMAL_SIZE = 10;
        public const Int32 MINIMAL_SIZE = 18;
        public const Int32 TAIL_SIZE = 8;
        public const byte ID1 = 0x1F;
        public const byte ID2 = 0x8B;
        #endregion

        private Stream _stream;
        private Int64 _offset;
        private Int64 _streamSize;
        
        private UInt16 _headerCRC;    

        public UInt32 HeaderSize { get; private set; }
        public UInt32 OriginalFileSize { get; private set; }
        public UInt32 DataCRC { get; private set; }
        public CompressionTypes CompressionMethod { get; private set; }
        public GFlags Flags { get; private set; }
        public DateTime OriginalTimeStamp { get; private set; }
        public DeflateAlgorithm AlgorithmType { get; set; }
        public GOperatingSystem OperatingSystem { get; private set; }
        public string OriginalFileName { get; private set; }
        public string Comment { get; private set; }
        public List<GZipExtraField> ExtraFields { get; private set; }

        public static GzipFileInfo FromCompressedData(byte[] data) => new GzipFileInfo(data);

        public static GzipFileInfo FromCompressedFile(string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open))
            {
                return new GzipFileInfo(fileStream);
            }
        }

        public static GzipFileInfo FromCompressionTarget(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            return new GzipFileInfo(fileInfo);
        }

        public GzipFileInfo(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                CreateFromStream(ms, 0);
            }
        }

        public GzipFileInfo(Stream stream) => CreateFromStream(stream, 0);

        public GzipFileInfo(Stream stream, Int64 offset) => CreateFromStream(stream, offset);

        public GzipFileInfo(FileInfo fileInfo)
        {
            OriginalFileSize = (UInt32)fileInfo.Length;
            CompressionMethod = CompressionTypes.DEFLATE;
            Flags = GFlags.FNAME;
            OriginalTimeStamp = fileInfo.CreationTime;
            AlgorithmType = DeflateAlgorithm.Fastest;
            OperatingSystem = GOperatingSystem.NTFS;
            OriginalFileName = fileInfo.Name;
            HeaderSize = (UInt32)CreateHeader().Length;
            ExtraFields = new List<GZipExtraField>();
        }

        public void SetOriginalFileName(string fileName)
        {
            OriginalFileName = fileName;
            Flags = Flags | GFlags.FNAME;
            RevalidateHeaderSize();
        }

        public void SetComment(string comment)
        {
            Comment = comment;
            Flags = Flags | GFlags.FCOMMENT;
            RevalidateHeaderSize();
        }

        public void AddExtraField(GZipExtraField field)
        {
            Flags = Flags | GFlags.FEXTRA;
            ExtraFields.Add(field);
            RevalidateHeaderSize();
        }

        public byte[] CreateHeader()
        {
            var result = new List<byte>();

            result.Add(ID1);
            result.Add(ID2);
            result.Add((byte)CompressionMethod);
            result.Add((byte)Flags);
            result.AddRange(Utilities.GetBytesOfUInt(Utilities.ToUnixTimeStamp(OriginalTimeStamp)));
            result.Add((byte)AlgorithmType);
            result.Add((byte)OperatingSystem);

            if (Flags.HasFlag(GFlags.FEXTRA))
            {
                var extrafields = new List<byte>();

                foreach (var field in ExtraFields)
                {
                    extrafields.AddRange(field.ToByteArray());
                }
                result.AddRange(Utilities.GetBytesOfUInt((UInt16)extrafields.Count));
                result.AddRange(extrafields);
            }

            if (Flags.HasFlag(GFlags.FNAME)) result.AddRange(Utilities.StringToNullTerminatedBytes(OriginalFileName));
            if (Flags.HasFlag(GFlags.FCOMMENT)) result.AddRange(Utilities.StringToNullTerminatedBytes(Comment));

            return result.ToArray();
        }

        public byte[] CreateTail(UInt32 crc32)
        {
            var result = new List<byte>();

            result.AddRange(Utilities.GetBytesOfUInt(crc32));
            result.AddRange(Utilities.GetBytesOfUInt(OriginalFileSize));

            return result.ToArray();
        }

        public GZipExtraField FindFirstFieldOrDefault(byte si1, byte si2) => 
            ExtraFields.FirstOrDefault(f =>
                f.SI1 == si1 &&
                f.SI2 == si2);

        private void CreateFromStream(Stream stream, Int64 offset)
        {
            _stream = stream;
            _offset = offset;
            _stream.Seek(_offset, SeekOrigin.Begin);
            _streamSize = _stream.Length;
            if (_streamSize - offset < MINIMAL_SIZE) throw new FormatException("File doesn't match gzip format");
            ExtraFields = new List<GZipExtraField>();
            ReadInfo();
            _stream = null;
        }

        private void ReadInfo()
        {
            ReadHeader();
            ReadTail();
            _stream.Seek(HeaderSize, SeekOrigin.Begin);
        }

        private void ReadHeader()
        {
            var header = new byte[HEADER_MINIMAL_SIZE];
            _stream.Read(header, 0, HEADER_MINIMAL_SIZE);
            if (!CheckFormatValidity(header)) throw new FormatException("File doesn't match gzip format");

            ReadHeaderMainPart(header);
            ReadHeaderAdditionalPart();
        }

        private void ReadTail()
        {
            _stream.Seek(_stream.Length - TAIL_SIZE, SeekOrigin.Begin);
            var tail = new byte[TAIL_SIZE];
            _stream.Read(tail, 0, TAIL_SIZE);

            DataCRC = Utilities.GetUIntFromBytes<UInt32>(tail, 0);
            OriginalFileSize = Utilities.GetUIntFromBytes<UInt32>(tail, 4);
        }

        private bool CheckFormatValidity(byte[] header) =>
            header[0] == ID1 && header[1] == ID2;

        private void ReadHeaderMainPart(byte[] header)
        {
            CompressionMethod = (CompressionTypes)header[2];
            Flags = (GFlags)header[3];
            OriginalTimeStamp = GetOriginalTimeStamp(header);
            AlgorithmType = (DeflateAlgorithm)header[8];
            OperatingSystem = (GOperatingSystem)header[9];
        }

        private void ReadHeaderAdditionalPart()
        {
            Int32 totalHeaderSize = HEADER_MINIMAL_SIZE;

            if (Flags.HasFlag(GFlags.FEXTRA))
            {
                byte[] XLEN = new byte[2];
                _stream.Read(XLEN, 0, 2);
                UInt16 ExtraFieldsLength = Utilities.GetUIntFromBytes<UInt16>(XLEN, 0);
                totalHeaderSize += 2 + ExtraFieldsLength;

                var extraFields = new byte[ExtraFieldsLength];
                _stream.Read(extraFields, 0, ExtraFieldsLength);
                ExtraFields = GZipExtraFieldExtractor.ExtractAllFields(extraFields);
            }

            if (Flags.HasFlag(GFlags.FNAME))
            {
                Int32 originalNameStringLength = Utilities.ReadNullTerminatedString(_stream, (Int32)_offset + totalHeaderSize, out string originalName);
                totalHeaderSize += originalNameStringLength;
                OriginalFileName = originalName;
            }

            if (Flags.HasFlag(GFlags.FCOMMENT))
            {
                Int32 commentStringLength = Utilities.ReadNullTerminatedString(_stream, (Int32)_offset + totalHeaderSize, out string comment);
                totalHeaderSize += commentStringLength;
                Comment = comment;
            }

            if (Flags.HasFlag(GFlags.FHCRC))
            {
                byte[] headerCRC = new byte[2];
                _stream.Read(headerCRC, 0, 2);
                _headerCRC = Utilities.GetUIntFromBytes<UInt16>(headerCRC, 0);
                totalHeaderSize += 2;
            }

            HeaderSize = (UInt32)totalHeaderSize;
        }

        private void RevalidateHeaderSize()
        {
            Int32 totalHeaderSize = HEADER_MINIMAL_SIZE;

            if (Flags.HasFlag(GFlags.FEXTRA))
            {
                totalHeaderSize += 2;

                foreach (var field in ExtraFields)
                {
                    totalHeaderSize += field.FieldTotalLength;
                }
            }

            if (Flags.HasFlag(GFlags.FNAME))
            {
                totalHeaderSize += Encoding.Default.GetBytes(OriginalFileName).Length + 1;
            }

            if (Flags.HasFlag(GFlags.FCOMMENT))
            {
                totalHeaderSize += Encoding.Default.GetBytes(Comment).Length + 1;
            }

            if (Flags.HasFlag(GFlags.FHCRC))
            {
                totalHeaderSize += 2;
            }

            HeaderSize = (UInt32)totalHeaderSize;
        }

        private DateTime GetOriginalTimeStamp(byte[] header)
        {
            UInt32 unixTimeStamp = Utilities.GetUIntFromBytes<UInt32>(header, 4);
            return Utilities.FromUnixStamp(unixTimeStamp);
        }


    }

}
