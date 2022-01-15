#nullable enable

namespace Elffy.Effective
{
    /// <summary>Indicates how to copy memory.</summary>
    [System.Obsolete]
    public enum MemoryCopyMode : byte
    {
        /// <summary>Deep copy, i.e., recursively copy all memory and create an object that is independent of the original object.</summary>
        DeepCopy = 0,
        /// <summary>Copy the array, but the elements of it are not copied.</summary>
        ArrayOnly = 1,
    }
}
