using System;
using System.Runtime.InteropServices;

namespace RabbitChallenge
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CharacterDistribution : IEquatable<CharacterDistribution>
    {
        public const int Length = 26;
        public static readonly CharacterDistribution Empty;
        public int Rank;
        private fixed byte _distributionTable[Length];

        private CharacterDistribution(byte[] distributionTable, int rank)
        {
            Rank = rank;

            fixed (byte* a = distributionTable, b = _distributionTable)
            {
                Buffer.MemoryCopy(a, b, Length, Length);
            }
        }

        public bool CanContain(CharacterDistribution other)
        {
            if (other.Rank > Rank)
            {
                return false;
            }

            fixed (byte* aFixed = _distributionTable)
            {
                var a = aFixed;
                var b = other._distributionTable;

                for (var i = 0; i < Length; i++)
                {
                    if (*a < *b)
                    {
                        return false;
                    }

                    a++;
                    b++;
                }
            }

            return true;
        }

        public static CharacterDistribution FromString(string str)
        {
            var dis = new byte[Length];

            foreach (var c in str)
            {
                var i = c - 'a';

                if (i < 0 || i >= Length)
                {
                    return Empty;
                }

                dis[i]++;
            }

            return new CharacterDistribution(dis, str.Length);
        }

        /// <inheritdoc />
        public bool Equals(CharacterDistribution other)
        {
            if (Rank != other.Rank)
            {
                return false;
            }

            fixed (byte* aFixed = _distributionTable)
            {
                var a = aFixed;
                var b = other._distributionTable;

                for (var i = 0; i < Length; i++)
                {
                    if (*a != *b)
                    {
                        return false;
                    }

                    a++;
                    b++;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj is CharacterDistribution other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                var hashCode = Rank;

                // ReSharper disable once NonReadonlyMemberInGetHashCode
                fixed (byte* aFixed = _distributionTable)
                {
                    var a = aFixed;

                    for (var i = 0; i < Length; i++)
                    {
                        hashCode = (hashCode * 397) ^ *a;
                        a++;
                    }
                }

                return hashCode;
            }
        }


        public static bool operator ==(CharacterDistribution left, CharacterDistribution right)
        {
            return left.Equals(right);
        }

        public static CharacterDistribution operator -(CharacterDistribution left, CharacterDistribution right)
        {
            var result = new CharacterDistribution();
            var rank = 0;

            var a = left._distributionTable;
            var b = right._distributionTable;
            var c = result._distributionTable;

            for (var i = 0; i < Length; i++)
            {
                if (*a > *b)
                {
                    var n = (byte) (*a - *b);
                    *c = n;
                    rank += n;
                }
                else
                {
                    *c = 0;
                }

                a++;
                b++;
                c++;
            }

            result.Rank = rank;

            return result;
        }

        public static bool operator !=(CharacterDistribution left, CharacterDistribution right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var str = "";

            for (var i = 0; i < Length; i++)
            {
                str += new string((char) ('a' + i), _distributionTable[i]);
            }

            return str;
        }
    }
}