//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using org.fressian.handlers;

namespace org.fressian.impl
{
    public static class Fns
    {
        public static ArgumentOutOfRangeException expected(Object expected, int ch)
        {
            return new ArgumentOutOfRangeException(String.Format("expected %s at %X", expected, ch));
        }

        public static ArgumentOutOfRangeException expected(Object expected, int ch, Object got)
        {
            return new ArgumentOutOfRangeException(String.Format("expected %s at %X, got %s", expected, ch, got));
        }

        public static KeyValuePair<K, V> soloEntry<K, V>(IDictionary<K, V> m)
        {
            if ((m != null) && m.Count == 1)
            {
                var en = m.GetEnumerator();
                en.MoveNext();
                return en.Current;
            }
            throw new ArgumentOutOfRangeException(String.Format("expected a map of one entry, got %s", m));
        }

        public static byte[] UUIDtoByteArray(Guid uuid)
        {
            return uuid.ToByteArray();
        }

        public static Guid byteArrayToUUID(byte[] bytes)
        {
            return new Guid(bytes);
        }

        public static K soloKey<K, V>(IDictionary<K, V> m)
        {
            return soloEntry(m).Key;
        }

        public static V soloVal<K, V>(IDictionary<K, V> m)
        {
            return soloEntry(m).Value;
        }

        public static IDictionary<K, V> soloMap<K, V>(K k, V v)
        {
            IDictionary<K, V> m = new Dictionary<K, V>();
            m[k] = v;
            return m;
        }

        public static V lookup<K, V>(ILookup<K, V> theLookup, K k)
        {
            if (theLookup == null) return default(V);
            return theLookup.valAt(k);
        }

        public static Type getClassOrNull(Object o)
        {
            if (o == null)
            {
                return null;
            }
            else
            {
                return o.GetType();
            }
        }
        public static int intCast(long x)
        {
            int i = (int)x;
            if (i != x)
                throw new ArgumentOutOfRangeException("Value out of range for int: " + x);
            return i;
        }

        public static void readUTF8Chars(StringBuilder dest, byte[] source, int offset, int length)
        {
            for (int pos = offset; pos < length; )
            {
                int ch = (int)source[pos++] & 0xff;
                switch (ch >> 4)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        dest.Append((char)ch);
                        break;
                    case 12:
                    case 13:
                        {
                            int ch1 = source[pos++];
                            dest.Append((char)((ch & 0x1f) << 6 | ch1 & 0x3f));
                        }
                        break;
                    case 14:
                        {
                            int ch1 = source[pos++];
                            int ch2 = source[pos++];
                            dest.Append((char)((ch & 0x0f) << 12 | (ch1 & 0x3f) << 6 | ch2 & 0x3f));
                        }
                        break;
                    default:
                        throw new ApplicationException(String.Format("Invalid UTF-8: %X", ch));
                }
            }
        }

        public static int utf8EncodingSize(int ch)
        {
            if (ch <= 0x007f)
                return 1;
            else if (ch > 0x07ff)
                return 3;
            return 2;
        }

        // starting with position start in s, write as much of s as possible into byteBuffer
        // using UTF-8. 
        // returns {stringpos, bufpos}
        public static int[] bufferStringChunkUTF8(string s, int start, byte[] byteBuffer)
        {
            int bufferPos = 0;
            int stringPos = start;
            while (stringPos < s.Length)
            {
                char ch = s[stringPos];
                int encodingSize = utf8EncodingSize(ch);
                if ((bufferPos + encodingSize) > byteBuffer.Length)
                {
                    break;
                }

                switch (encodingSize)
                {
                    case 1:
                        byteBuffer[bufferPos++] = (byte)ch;
                        break;
                    case 2:
                        byteBuffer[bufferPos++] = (byte)(0xc0 | ch >> 6 & 0x1f);
                        byteBuffer[bufferPos++] = (byte)(0x80 | ch >> 0 & 0x3f);
                        break;
                    case 3:
                        byteBuffer[bufferPos++] = (byte)(0xe0 | ch >> 12 & 0x0f);
                        byteBuffer[bufferPos++] = (byte)(0x80 | ch >> 6 & 0x3f);
                        byteBuffer[bufferPos++] = (byte)(0x80 | ch >> 0 & 0x3f);
                        break;
                }
                stringPos++;
            }
            return new int[] { stringPos, bufferPos };
        }

        //FF
        public static int numberOfLeadingZeros(long i)
        {
            // HD, Figure 5-6
            if (i == 0)
                return 64;
            int n = 1;
            int x = (int)((uint)i >> 32);
            if (x == 0) { n += 32; x = (int)i; }
            if ((uint)x >> 16 == 0) { n += 16; x <<= 16; }
            if ((uint)x >> 24 == 0) { n += 8; x <<= 8; }
            if ((uint)x >> 28 == 0) { n += 4; x <<= 4; }
            if ((uint)x >> 30 == 0) { n += 2; x <<= 2; }
            n -= (int)((uint)x >> 31);
            return n;
        }

        public static int SingleToInt32Bits(this float v)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
        }

    }
}