using System;
using System.Runtime.InteropServices;

namespace RabbitChallenge
{
    /// <summary>
    ///     Represents a character distribution table inside memory; can be used as a describer for a word
    ///     or as a way to describe a filter
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CharacterDistribution : IEquatable<CharacterDistribution>
    {
        /// <summary>
        ///     The length of the table; 26 is the number of english characters
        /// </summary>
        public const int Length = 26;

        private const int IndexX = 'a' - 'a';
        private const int IndexI = 'i' - 'a';
        private const int IndexO = 'o' - 'a';

        /// <summary>
        ///     Represents an empty <see cref="CharacterDistribution" />
        /// </summary>
        public static readonly CharacterDistribution Empty;

        /// <summary>
        ///     The total number of characters
        /// </summary>
        public int Rank;

        /// <summary>
        ///     The in-memory table of character occurrences
        /// </summary>
        private fixed byte _distributionTable[Length];

        private CharacterDistribution(byte[] distributionTable, int rank)
        {
            Rank = rank;

            // Copy the passed character distribution byte array
            fixed (byte* a = distributionTable, b = _distributionTable)
            {
                Buffer.MemoryCopy(a, b, Length, Length);
            }
        }

        /// <summary>
        ///     Indicates if this object represents a valid character distribution
        /// </summary>
        /// <returns>if invalid false; otherwise true.</returns>
        // TODO: This should be optimized further
        public bool ShouldConsiderAsValid()
        {
            if (Rank > 1)
            {
                return true;
            }

            if (Rank == 0)
            {
                return false;
            }

            // Only consider single character distribution if the character is 'a', 'i' or 'o'
            if (_distributionTable[IndexX] > 0 ||
                _distributionTable[IndexI] > 0 ||
                _distributionTable[IndexO] > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Checks to see if a <see cref="CharacterDistribution" /> can be contained in (and is a subset of) this
        ///     <see cref="CharacterDistribution" />.
        /// </summary>
        /// <param name="other">The other <see cref="CharacterDistribution" />.</param>
        /// <returns>
        ///     true if passed <see cref="CharacterDistribution" /> is a subset of this <see cref="CharacterDistribution" />;
        ///     otherwise false
        /// </returns>
        public bool CanContain(CharacterDistribution other)
        {
            if (other.Rank > Rank || other.Rank == 0)
            {
                return false;
            }

            // Fix the distribution table
            fixed (byte* aFixed = _distributionTable)
            {
                var a = aFixed;
                var b = other._distributionTable;

                // Go through each byte
                for (var i = 0; i < Length; i++)
                {
                    // If there are more characters in the other object, this one can not contain it
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

        /// <summary>
        ///     Creates a new <see cref="CharacterDistribution" /> from an <see cref="string" />.
        /// </summary>
        /// <param name="str">The <see cref="string" /> to create a <see cref="CharacterDistribution" /> from.</param>
        /// <returns>
        ///     The newly created <see cref="CharacterDistribution" /> if process succeeds; otherwise <see cref="Empty" />
        /// </returns>
        public static CharacterDistribution FromString(string str)
        {
            var dis = new byte[Length];

            foreach (var c in str)
            {
                var i = c - 'a';

                // if character is not an english character between 
                // a -z (lower case), consider the whole word invalid
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
            // If the number of total characters are not equal, 
            // we can be sure that this two objects are not equal
            if (Rank != other.Rank)
            {
                return false;
            }

            // otherwise, we need to go through each byte to
            // make sure that these two objects are equal
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
            // Creating a solid hash code is required as this structure is going
            // to be used with Dictionary class which is a HashTable.
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

            // Subtract two objects by subtracting number of each character occurrence
            // from the first object by the second one. If the second object contains 
            // more characters, then we are going to ignore those
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

            // Creating an string representation of this object by 
            // returning every character represented by it
            for (var i = 0; i < Length; i++)
            {
                str += new string((char) ('a' + i), _distributionTable[i]);
            }

            return str;
        }
    }
}