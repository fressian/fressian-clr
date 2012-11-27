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
using System.Linq;

using org.fressian.impl;
using org.fressian.handlers;

namespace org.fressian
{
    public class FressianWriter : StreamingWriter, Writer, IDisposable
    {
        private Stream stream;
        private RawOutput rawOut;
        private InterleavedIndexHopMap priorityCache;
        private InterleavedIndexHopMap structCache;
        private byte[] stringBuffer;
        IWriteHandlerLookup writeHandlerLookup;

        public FressianWriter(Stream ostream)
            : this(ostream, Handlers.defaultWriteHandlers())
        {
        }

        /**
         *  Create a writer that combines userHandlers with the normal type handlers
         *  built into Fressian.
         */
        public FressianWriter(Stream stream, org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> userHandlers)
        {
            this.writeHandlerLookup = new WriteHandlerLookup(userHandlers);
            
            clearCaches();
            this.stream = stream;
            this.rawOut = new RawOutput(this.stream);
        }

        public FressianWriter(Stream stream, object userHandlers, bool dummy)
        {
            if (userHandlers != null)
            {
                var dLookup = new Dictionary<Type, IDictionary<String, WriteHandler>>();

                var handlers = ((System.Collections.Generic.IEnumerable<object>)userHandlers);
                foreach (System.Collections.IList h in handlers)
                {
                    var type = (Type)h[0];
                    if (!dLookup.ContainsKey(type))
                        dLookup[type] = new Dictionary<string, WriteHandler>();

                    foreach (System.Collections.IList e in (IEnumerable<object>)h[1])
                    {
                        var tag = (string)e[0];
                        var writeHandler = (org.fressian.handlers.WriteHandler)e[1];
                        dLookup[type][tag] = writeHandler;
                    }
                }

                this.writeHandlerLookup = new WriteHandlerLookup(new MapLookup<Type, IDictionary<String, WriteHandler>>(dLookup));
            }
            else
            {
                this.writeHandlerLookup =  new WriteHandlerLookup(Handlers.defaultWriteHandlers());
            }
            clearCaches();
            this.stream = stream;
            this.rawOut = new RawOutput(this.stream);
        }

        public Writer writeNull()
        {
            writeCode(Codes.NULL);
            return this;
        }

        public Writer writeBoolean(bool b)
        {
            if (b)
            {
                writeCode(Codes.TRUE);
            }
            else
            {
                writeCode(Codes.FALSE);
            }
            return this;
        }

        public Writer writeBoolean(Object o)
        {
            if (o == null)
            {
                writeNull();
                return this;
            }

            //writeBoolean(((Boolean) o).booleanValue());
            writeBoolean(Convert.ToBoolean(o));
            return this;
        }

        public Writer writeInt(long i)
        {
            internalWriteInt(i);
            return this;
        }

        public Writer writeInt(Object o)
        {
            if (o == null)
            {
                writeNull();
                return this;
            }
            // writeInt(((Number) o).longValue());
            writeInt(Convert.ToInt64(o));
            return this;
        }

        public Writer writeDouble(double d)
        {
            if (d == 0.0)
            {
                writeCode(Codes.DOUBLE_0);
            }
            else if (d == 1.0)
            {
                writeCode(Codes.DOUBLE_1);
            }
            else
            {
                writeCode(Codes.DOUBLE);
                rawOut.writeRawDouble(d);
            }
            return this;
        }

        public Writer writeDouble(Object o)
        {
            //writeDouble(((Number) o).doubleValue());
            writeDouble(Convert.ToDouble(o));
            return this;
        }

        public Writer writeFloat(float f)
        {
            writeCode(Codes.FLOAT);
            rawOut.writeRawFloat(f);
            return this;
        }

        public Writer writeFloat(Object o)
        {
            //writeFloat(((Number) o).floatValue());
            writeFloat(Convert.ToSingle(o));
            return this;
        }

        public Writer writeString(Object o)
        {
            if (o == null)
            {
                writeNull();
                return this;
            }
            string s = (string)o;
            int stringPos = 0;
            int bufPos = 0;
            int maxBufNeeded = Math.Min(s.Length * 3, 65536);
            if ((stringBuffer == null) || (stringBuffer.Length < maxBufNeeded))
            {
                stringBuffer = new byte[maxBufNeeded];
            }
            do
            {
                int[] temp = Fns.bufferStringChunkUTF8(s, stringPos, stringBuffer);
                stringPos = temp[0];
                bufPos = temp[1];
                if (bufPos < Ranges.STRING_PACKED_LENGTH_END)
                {
                    rawOut.writeRawByte((int)(Codes.STRING_PACKED_LENGTH_START + bufPos));
                }
                else if (stringPos == s.Length)
                {
                    writeCode(Codes.STRING);
                    writeCount(bufPos);
                }
                else
                {
                    writeCode(Codes.STRING_CHUNK);
                    writeCount(bufPos);
                }
                rawOut.writeRawBytes(stringBuffer, 0, bufPos);
            } while (stringPos < s.Length);

            return this;
        }

        private void writeIterator<T>(int length, IEnumerator<T> it)
        {
            if (length < Ranges.LIST_PACKED_LENGTH_END)
            {
                rawOut.writeRawByte((int)(Codes.LIST_PACKED_LENGTH_START + length));
            }
            else
            {
                writeCode(Codes.LIST);
                writeCount(length);
            }
            while (it.MoveNext())
            {
                writeObject(it.Current);
            }
        }

        public Writer writeList(Object o)
        {
            if (o == null)
            {
                writeNull();
                return this;
            }

            if (o.GetType().IsArray)
            {
                return writeList((o as IEnumerable<object>).ToList());
            }
            IList<object> c = (IList<object>)o;
            writeIterator(c.Count, c.GetEnumerator());
            return this;
        }

        public Writer writeBytes(byte[] b)
        {
            if (b == null)
            {
                writeNull();
                return this;
            }

            return writeBytes(b, 0, b.Length);
        }

        public Writer writeBytes(byte[] b, int offset, int length)
        { 
            if (length < Ranges.BYTES_PACKED_LENGTH_END)
            {
                rawOut.writeRawByte((int)(Codes.BYTES_PACKED_LENGTH_START + length));
                rawOut.writeRawBytes(b, offset, length);
            }
            else
            {
                while (length > Ranges.BYTE_CHUNK_SIZE)
                {
                    writeCode(Codes.BYTES_CHUNK);
                    writeCount(Ranges.BYTE_CHUNK_SIZE);
                    rawOut.writeRawBytes(b, offset, Ranges.BYTE_CHUNK_SIZE);
                    offset += Ranges.BYTE_CHUNK_SIZE;
                    length -= Ranges.BYTE_CHUNK_SIZE;
                }
                writeCode(Codes.BYTES);
                writeCount(length);
                rawOut.writeRawBytes(b, offset, length);
            }
            return this;
        }

        public void writeFooterFor(MemoryStream bb)
        {
            if (rawOut.getBytesWritten() != 0)
                throw new InvalidOperationException("writeFooterFor can only be called at a footer boundary.");
            byte[] bytes = bb.ToArray();

            //FF
            //ByteBuffer source = bb.duplicate();
            //byte[] bytes;
            //if (source.hasArray()) {
            //    bytes = source.array();
            //} else {
            //    bytes = new byte[source.remaining()];
            //    source.get(bytes);
            //}

            rawOut.getChecksum().Update(bytes, 0, bytes.Length);
            internalWriteFooter(bytes.Length);
        }

        public Writer writeFooter()
        {
            internalWriteFooter(rawOut.getBytesWritten());
            clearCaches();
            return this;
        }

        private void internalWriteFooter(int length)
        {
            rawOut.writeRawInt32(Codes.FOOTER_MAGIC);
            rawOut.writeRawInt32(length);
            rawOut.writeRawInt32((int)rawOut.getChecksum().GetValue());
            rawOut.reset();
        }

        private void clearCaches()
        {
            if ((priorityCache != null) && !priorityCache.isEmpty())
                priorityCache.clear();
            if ((structCache != null) && !structCache.isEmpty())
                structCache.clear();
        }

        public Writer resetCaches()
        {
            writeCode(Codes.RESET_CACHES);
            clearCaches();
            return this;
        }

        public InterleavedIndexHopMap getPriorityCache()
        {
            if (priorityCache == null)
            {
                priorityCache = new InterleavedIndexHopMap(16);
            }
            return priorityCache;
        }

        public InterleavedIndexHopMap getStructCache()
        {
            if (structCache == null)
            {
                structCache = new InterleavedIndexHopMap(16);
            }
            return structCache;
        }

        public Writer writeTag(Object tag, int componentCount)
        {
            int shortcutCode = Handlers.tagToCode[tag];
            if (shortcutCode != null)
            {
                writeCode(shortcutCode);
            }
            else
            {
                int index = getStructCache().oldIndex(tag);
                if (index == -1)
                {
                    writeCode(Codes.STRUCTTYPE);
                    writeObject(tag);
                    writeInt(componentCount);
                }
                else if (index < Ranges.STRUCT_CACHE_PACKED_END)
                {
                    writeCode((int)Codes.STRUCT_CACHE_PACKED_START + index);
                }
                else
                {
                    writeCode(Codes.STRUCT);
                    writeInt(index);
                }
            }
            return this;
        }

        public Writer writeExt(Object tag, params object[] fields)
        { 
            writeTag(tag, fields.Length);
            for (int n = 0; n < fields.Length; n++)
            {
                writeObject(fields[n]);
            }
            return this;
        }

        public void writeCount(int count)
        { 
            writeInt(count);
        }

        // returns (bits not needed to represent this number) + 1
        private int bitSwitch(long l)
        {
            if (l < 0) l = ~l;
            return Fns.numberOfLeadingZeros(l);
        }

        private void internalWriteInt(long i)
        { 
            switch (bitSwitch(i))
            {
                //FF??
                case 0:
                
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                    writeCode(Codes.INT);
                    rawOut.writeRawInt64(i);
                    break;

                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_7_ZERO + (i >> 48)));
                    rawOut.writeRawInt48(i);
                    break;

                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_6_ZERO + (i >> 40)));
                    rawOut.writeRawInt40(i);
                    break;

                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_5_ZERO + (i >> 32)));
                    rawOut.writeRawInt32((int)i);
                    break;

                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_4_ZERO + (i >> 24)));
                    rawOut.writeRawInt24((int)i);
                    break;

                case 45:
                case 46:
                case 47:
                case 48:
                case 49:
                case 50:
                case 51:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_3_ZERO + (i >> 16)));
                    rawOut.writeRawInt16((int)i);
                    break;

                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                    rawOut.writeRawByte((int)(Codes.INT_PACKED_2_ZERO + (i >> 8)));
                    rawOut.writeRawByte((int)i);
                    break;

                case 58:
                case 59:
                case 60:
                case 61:
                case 62:
                case 63:
                case 64:
                    if (i < -1)
                    {
                        rawOut.writeRawByte((int)(Codes.INT_PACKED_2_ZERO + (i >> 8)));
                    }
                    rawOut.writeRawByte((int)i);
                    break;

                default:
                    throw new Exception("more than 64 bits in a long!");
            }
        }

        private bool shouldSkipCache(Object o)
        {
            if ((o == null) || (o is Boolean))
                return true;
            else if ((o is Int32) || (o is Int16) || (o is Int64))
            {
                switch (bitSwitch(Convert.ToInt64(o)))
                {
                    // current: 1 or 2 byte representations skip cache
                    // consider: cache two byte reps after checking priority cache
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 63:
                    case 64:
                        return true;
                    default:
                        return false;

                }
            }
            else if (o is String)
                return ((String)o).Length == 0;
            else if (o is Double)
            {
                double d = Convert.ToDouble(o);
                return (d == 0.0) || (d == 1.0);
            }
            return false;
        }

        private void doWrite(String tag, Object o, WriteHandler w, bool cache)
        {
            if (cache)
            {
                if (shouldSkipCache(o))
                    doWrite(tag, o, w, false);
                else
                {
                    int index = getPriorityCache().oldIndex(o);
                    if (index == -1)
                    {
                        writeCode(Codes.PUT_PRIORITY_CACHE);
                        doWrite(tag, o, w, false);
                    }
                    else
                    {
                        if (index < Ranges.PRIORITY_CACHE_PACKED_END)
                        {
                            writeCode((int)Codes.PRIORITY_CACHE_PACKED_START + index);
                        }
                        else
                        {
                            writeCode(Codes.GET_PRIORITY_CACHE);
                            writeInt(index);
                        }
                    }
                }
            }
            else
            {
                w.write(this, o);
            }
        }

        public Writer writeAs(String tag, Object o, bool cache)
        {
            if (o is CachedObject)
            {
                o = CachedObject.unwrap(o);
                cache = true;
            }
            WriteHandler w = writeHandlerLookup.requireWriteHandler(tag, o);
            doWrite(tag, o, w, cache);
            return this;
        }

        public Writer writeAs(String tag, Object o)
        {
            return writeAs(tag, o, false);
        }

        public Writer writeObject(Object o, bool cache)
        {
            return writeAs(null, o, cache);
        }

        public Writer writeObject(Object o)
        {
            return writeAs(null, o);
        }

        public void writeCode(int code)
        {
            rawOut.writeRawByte(code);
        }

        public Writer beginClosedList()
        {
            writeCode(Codes.BEGIN_CLOSED_LIST);
            return this;
        }

        public Writer endList()
        {
            writeCode(Codes.END_COLLECTION);
            return this;
        }

        public Writer beginOpenList()
        {
            if (0 != rawOut.getBytesWritten())
                throw new InvalidOperationException("openList must be called from the top level, outside any footer context.");
            writeCode(Codes.BEGIN_OPEN_LIST);
            rawOut.reset();
            return this;
        }

        //public void close()
        //{
        //    rawOut.close();
        //}

        public void Dispose()
        {
            this.rawOut.Dispose();
        }
    }
}