//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Author:  Frank Failla
//

using System;
using System.Threading;

using org.fressian;

namespace org.fressian 
{
    public sealed class Adler32 : Checksum
    {
        private long _checksum;

        public Adler32()
        {
            this._checksum = 1;
        }

        public long Value
        {
            get { return this._checksum; }
        }

        public void Reset()
        {
            this._checksum = 1;            
        }

        public void Update(byte[] buffer, int offset, int count)
        {
            this._checksum = adler32(this._checksum, buffer, offset, count);            
        }

        #region adler32
        /*
        Adapted from the wikipedia article:
            http://en.wikipedia.org/wiki/Adler-32
        */
        private const int MOD_ADLER = 65521;

        public static long adler32(long adler, byte[] data, int index, int len) 
        {
            //long a = 1;
            //long b = 0;
            long a = adler & 0xffff;
            long b = (adler >> 16) & 0xffff;
 
            for (index = 0; index < len; ++index)
            {
                a = (a + data[index]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }
 
            return (b << 16) | a;
        }
        #endregion
    }
}
