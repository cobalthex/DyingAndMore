using System;
using System.Collections.Generic;

namespace Takai
{
    public class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
        where T : IEquatable<T>
    {
        public static ArrayEqualityComparer<T> Compararer { get; } = new ArrayEqualityComparer<T>();

        private ArrayEqualityComparer() { }

        public bool Equals(T[] x, T[] y)
        {
            // assumes x and y are not null
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; ++i)
            {
                if (!x[i].Equals(y[i]))
                    return false;
            }
            return true;
        }

        // Crude method for combining two hash codes
        // based on Array.GetHashCode
        // https://referencesource.microsoft.com/#mscorlib/system/array.cs,812
        public static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }

        int IEqualityComparer<T[]>.GetHashCode(T[] obj)
        {
            int ret = 0;
            for (int i = (obj.Length >= 8 ? obj.Length - 8 : 0); i < obj.Length; ++i)
                ret = CombineHashCodes(ret, obj[i].GetHashCode());
            return ret;
        }
    }
}
