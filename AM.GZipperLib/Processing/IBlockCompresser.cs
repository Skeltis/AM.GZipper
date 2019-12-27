using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    public interface IBlockCompresser
    {
        IBlock Compress(IBlock data);
        IBlock Decompress(IBlock data);
    }
}