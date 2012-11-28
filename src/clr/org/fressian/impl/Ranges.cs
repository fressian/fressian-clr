//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;

namespace org.fressian.impl
{
    public class Ranges
    {
        public static readonly ulong PACKED_1_START = 0L;
        public static readonly ulong PACKED_1_END = 64;

        public static readonly ulong PACKED_2_START = 0xFFFFFFFFFFFFF000L;
        public static readonly ulong PACKED_2_END =   0x0000000000001000L;
        public static readonly ulong PACKED_3_START = 0xFFFFFFFFFFF80000L;
        public static readonly ulong PACKED_3_END =   0x0000000000080000L;
        public static readonly ulong PACKED_4_START = 0xFFFFFFFFFE000000L;
        public static readonly ulong PACKED_4_END =   0x0000000002000000L;
        public static readonly ulong PACKED_5_START = 0xFFFFFFFE00000000L;
        public static readonly ulong PACKED_5_END =   0x0000000200000000L;
        public static readonly ulong PACKED_6_START = 0xFFFFFE0000000000L;
        public static readonly ulong PACKED_6_END =   0x0000020000000000L;
        public static readonly ulong PACKED_7_START = 0xFFFE000000000000L;
        public static readonly ulong PACKED_7_END =   0x0002000000000000L;

        public static readonly int PRIORITY_CACHE_PACKED_END = 32;
        public static readonly int STRUCT_CACHE_PACKED_END = 16;
        public static readonly int BYTES_PACKED_LENGTH_END = 8;
        public static readonly int STRING_PACKED_LENGTH_END = 8;
        public static readonly int LIST_PACKED_LENGTH_END = 8;

        public static readonly int BYTE_CHUNK_SIZE = 65535;

        public static void main(string[] args)
        {
            ulong[] bounds = new ulong[]{
                        PACKED_1_START,
                        PACKED_1_END,
                        PACKED_2_START,
                        PACKED_2_END,
                        PACKED_3_START,
                        PACKED_3_END,
                        PACKED_4_START,
                        PACKED_4_END,
                        PACKED_5_START,
                        PACKED_5_END,
                        PACKED_6_START,
                        PACKED_6_END,
                        PACKED_7_START,
                        PACKED_7_END,
                };
            for (int n = 0; n < bounds.Length; n++)
            {
                for (ulong l = bounds[n] - 1; l < bounds[n] + 2; l++)
                {
                    long abs = Math.Abs((long)l);
                    Console.WriteLine(String.Format("number {0} {1} {2} bits: {3} switch: {4} - {5}"
                        , l.ToString("X")
                        , l
                        , Fns.numberOfLeadingZeros(abs)
                        , bitsneeded((long)l)
                        , switchon((long)l)
                        , abs));
                }
            }
        }

        public static int bitsneeded(long l)
        {
            if (l > 0)
            {
                return 65 - Fns.numberOfLeadingZeros(l);
            }
            else
            {
                return 65 - Fns.numberOfLeadingZeros(~l);
            }
        }
        public static int switchon(long l)
        {
            if (l > 0)
            {
                return Fns.numberOfLeadingZeros(l);
            }
            else
            {
                return Fns.numberOfLeadingZeros(~l);
            }
        }

    }
}