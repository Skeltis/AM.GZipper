using System;

namespace AM.GZipperLib
{
    public interface IProgressInformer
    {
        void IncrementValue();
        void SetOverallValue(Int64 value);
        void Reset();
    }
}
