namespace AM.GZipperLib
{
    public interface IBlockTaskDispatcher
    {
        void Cancel();
        void RunAndWait();
    }
}