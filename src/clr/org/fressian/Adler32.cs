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

        #region
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
