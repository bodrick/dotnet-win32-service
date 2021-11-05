using System.Collections.Generic;
using System.Linq;

namespace DasMulli.Win32.ServiceUtils
{
    /// <summary>
    /// Simplifies the work of hashing.
    /// Taken from https://rehansaeed.com/gethashcode-made-easy/ and modified with ReSharper.
    /// </summary>
    internal struct HashCode
    {
        private readonly int _value;

        private HashCode(int value) => _value = value;

        public static implicit operator int(HashCode hashCode) => hashCode._value;

        public static HashCode Of<T>(T item) => new(GetHashCode(item));

        public HashCode And<T>(T item) => new(CombineHashCodes(_value, GetHashCode(item)));

        public HashCode AndEach<T>(IEnumerable<T> items)
        {
            var hashCode = items.Select(GetHashCode).Aggregate(CombineHashCodes);
            return new HashCode(CombineHashCodes(_value, hashCode));
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            unchecked
            {
                // Code copied from System.Tuple so it must be the best way to combine hash codes or at least a good one.
                return ((h1 << 5) + h1) ^ h2;
            }
        }

        private static int GetHashCode<T>(T item) => item == null ? 0 : item.GetHashCode();
    }
}
