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
using System.Numerics;

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
                //FF - c# dictionaries do not support null keys, this is part of the workaround
                return typeof(Handlers.Null);
                //return null;
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
                        throw new ApplicationException(String.Format("Invalid UTF-8: {0}", ch));
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

        #region Int64 Number of Leading Zeros Helper
        /*
        Taken/Adapted from the following two files:
            * https://j2cstranslator.svn.sourceforge.net/svnroot/j2cstranslator/trunk/J2CSMapping/src/ILOG/Util/Int64Helper.cs
            * https://j2cstranslator.svn.sourceforge.net/svnroot/j2cstranslator/trunk/J2CSMapping/src/ILOG/Util/Math.cs
        
        Licenses:    
        
            // 
            // J2CsMapping : runtime library for J2CsTranslator
            // 
            // Copyright (c) 2008-2010 Alexandre FAU.
            // All rights reserved. This program and the accompanying materials
            // are made available under the terms of the Eclipse Public License v1.0
            // which accompanies this distribution, and is available at
            // http://www.eclipse.org/legal/epl-v10.html
            // Contributors:
            //   Alexandre FAU (IBM)
            //
         
            // 
            // J2CsMapping : runtime library for J2CsTranslator
            // 
            // Copyright (c) 2008-2010 Alexandre FAU.
            // All rights reserved. This program and the accompanying materials
            // are made available under the terms of the Eclipse Public License v1.0
            // which accompanies this distribution, and is available at
            // http://www.eclipse.org/legal/epl-v10.html
            // Contributors:
            //   Alexandre FAU (IBM)
            //
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static long URS(long a, int b)
        {
            long res = 0;
            if (a < 0)
            {
                ulong c = (ulong)a >> b;
                res = (long)c;
            }
            else
            {
                ulong c = ((ulong)a) >> b;
                res = Convert.ToInt64(c);

            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /// ToBeChecked
        public static int numberOfLeadingZeros(long lng)
        {
            lng |= lng >> 1;
            lng |= lng >> 2;
            lng |= lng >> 4;
            lng |= lng >> 8;
            lng |= lng >> 16;
            lng |= lng >> 32;
            return BitCount(~lng);
        }

        //
        // BitCount
        //
        /// <summary>
        /// <p>Counts the number of 1 bits in the <code>int</code>
        /// value passed; this is sometimes referred to as a
        /// population count.</p>
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        /// ToBeChecked
        public static int BitCount(long lng)
        {
            lng = (lng & 0x5555555555555555L) + ((lng >> 1) & 0x5555555555555555L);
            lng = (lng & 0x3333333333333333L) + ((lng >> 2) & 0x3333333333333333L);
            // adjust for 64-bit integer
            int i = (int)(URS(lng, 32) + lng);
            i = (i & 0x0F0F0F0F) + ((i >> 4) & 0x0F0F0F0F);
            i = (i & 0x00FF00FF) + ((i >> 8) & 0x00FF00FF);
            i = (i & 0x0000FFFF) + ((i >> 16) & 0x0000FFFF);
            return i;
        }
        #endregion

        public static int SingleToInt32Bits(this float v)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
        }

        public static Tuple<byte[], int> UnscaledValues(this decimal val)
        {
            int[] ints = Decimal.GetBits(val);
            byte[] bytes = new byte[12];
            bytes[0] = ((byte)((ints[0] >> 0) & 0xFF));
            bytes[1] = ((byte)((ints[0] >> 8) & 0xFF));
            bytes[2] = ((byte)((ints[0] >> 16) & 0xFF));
            bytes[3] = ((byte)((ints[0] >> 24) & 0xFF));
            bytes[4] = ((byte)((ints[1] >> 0) & 0xFF));
            bytes[5] = ((byte)((ints[1] >> 8) & 0xFF));
            bytes[6] = ((byte)((ints[1] >> 16) & 0xFF));
            bytes[7] = ((byte)((ints[1] >> 24) & 0xFF));
            bytes[8] = ((byte)((ints[2] >> 0) & 0xFF));
            bytes[9] = ((byte)((ints[2] >> 8) & 0xFF));
            bytes[10] = ((byte)((ints[2] >> 16) & 0xFF));
            bytes[11] = ((byte)((ints[2] >> 24) & 0xFF));

            BigInteger signed = val < 0 ? -(new BigInteger(bytes)) : new BigInteger(bytes);
            byte[] unscaledbytes = signed.ToByteArray();
            Array.Reverse(unscaledbytes);            
            var scale = (ints[3] >> 16) & 0xFF;
            return new Tuple<byte[], int>(unscaledbytes, scale);
        }

        public static Decimal DecimalValueFrom(byte[] d, int s)
        {
            Array.Reverse(d);
            BigInteger signed = new BigInteger(d);
            BigInteger unscaled = signed < 0 ? -signed : signed;
            if (s < 0)
            {
                unscaled = unscaled * BigInteger.Pow(BigInteger.One, -s + 1);
                s = 0;
            }
            byte[] bytes = unscaled.ToByteArray();

            if (bytes.Length > 12 || s > 28)
                throw new OverflowException("The BigDecimal is too big for a 96 bit CLR decimal");

            byte[] buf = new byte[12];
            Array.Copy(bytes, 0, buf, 0, bytes.Length);

            int lo = buf[0] << 0 | buf[1] << 8 | buf[2] << 16 | buf[3] << 24;
            int mid = buf[4] << 0 | buf[5] << 8 | buf[6] << 16 | buf[7] << 24;
            int hi = buf[8] << 0 | buf[9] << 8 | buf[10] << 16 | buf[11] << 24;

            return new Decimal(lo, mid, hi, signed.Sign < 0, (byte)s);
        }
    }
}
