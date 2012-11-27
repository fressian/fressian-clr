﻿using System;

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

        public long GetValue()
        {
            return this._checksum;
        }

        public void Reset()
        {
            this._checksum = 1;
        }

        public void Update(byte[] buffer, int offset, int count)
        {
            this._checksum = adler32(this._checksum, buffer, offset, count);
        }

        private const int MOD_ADLER = 65521;

        internal long adler32(long adler, byte[] data, int index, int len) 
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
    }
}