//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using org.fressian.impl;
using org.fressian.handlers;

namespace org.fressian
{
    public class FressianReader : Reader, IDisposable
    {
        private readonly RawInput rawInput;
        private ArrayList priorityCache;
        private ArrayList structCache;
        public readonly IDictionary<object, ReadHandler> standardExtensionHandlers;
        private readonly org.fressian.handlers.ILookup<Object, ReadHandler> handlerLookup;
        private byte[] byteBuffer;

        public FressianReader(Stream istream)
            : this(istream, null, true)
        {

        }

        public FressianReader(Stream istream, org.fressian.handlers.ILookup<Object, ReadHandler> handlerLookup)
            : this(istream, handlerLookup, true)
        {

        }

        public FressianReader(Stream istream, org.fressian.handlers.ILookup<Object, ReadHandler> handlerLookup, bool validateAdler)
        {
            standardExtensionHandlers = Handlers.extendedReadHandlers;
            this.rawInput = new RawInput(istream, validateAdler);
            this.handlerLookup = handlerLookup;
            resetCaches();
        }

        public bool readBoolean()
        {
            int code = readNextCode();

            switch (code)
            {
                case Codes.TRUE:
                    return true;
                case Codes.FALSE:
                    return false;
                default:
                    {
                        Object result = read(code);
                        if (result is Boolean)
                        {
                            return (Boolean)result;
                        }
                        else
                        {
                            throw Fns.expected("boolean", code, result);
                        }
                    }
            }
        }

        public long readInt()
        {
            return internalReadInt();
        }

        private long internalReadInt()
        {
            long result;
            int code = readNextCode();
            switch (code)
            {

                //INT_PACKED_1_FIRST
                case 0xFF:
                    result = -1;
                    break;

                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    result = (long)code & 0xff;
                    break;

                //  INT_PACKED_2_FIRST
                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                case 0x44:
                case 0x45:
                case 0x46:
                case 0x47:
                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                case 0x4C:
                case 0x4D:
                case 0x4E:
                case 0x4F:
                case 0x50:
                case 0x51:
                case 0x52:
                case 0x53:
                case 0x54:
                case 0x55:
                case 0x56:
                case 0x57:
                case 0x58:
                case 0x59:
                case 0x5A:
                case 0x5B:
                case 0x5C:
                case 0x5D:
                case 0x5E:
                case 0x5F:
                    result = ((long)(code - Codes.INT_PACKED_2_ZERO) << 8) | rawInput.readRawInt8();
                    break;

                //  INT_PACKED_3_FIRST
                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x66:
                case 0x67:
                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6E:
                case 0x6F:
                    result = ((long)(code - Codes.INT_PACKED_3_ZERO) << 16) | rawInput.readRawInt16();
                    break;


                //  INT_PACKED_4_FIRST
                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                    result = ((long)(code - Codes.INT_PACKED_4_ZERO << 24)) | rawInput.readRawInt24();
                    break;


                //  INT_PACKED_5_FIRST
                case 0x74:
                case 0x75:
                case 0x76:
                case 0x77:
                    result = ((long)(code - Codes.INT_PACKED_5_ZERO) << 32) | rawInput.readRawInt32();
                    break;

                //  INT_PACKED_6_FIRST
                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                    result = (((long)code - Codes.INT_PACKED_6_ZERO) << 40) | rawInput.readRawInt40();
                    break;

                //  INT_PACKED_7_FIRST
                case 0x7C:
                case 0x7D:
                case 0x7E:
                case 0x7F:
                    result = (((long)code - Codes.INT_PACKED_7_ZERO) << 48) | rawInput.readRawInt48();
                    break;

                case Codes.INT:
                    result = rawInput.readRawInt64();
                    break;

                default:
                    {
                        Object o = read(code);
                        if (o is Int64)
                        {
                            return (Int64)o;
                        }
                        else
                        {
                            throw Fns.expected("int64", code, o);
                        }
                    }
            }
            return result;
        }

        public double readDouble()
        {
            int code = readNextCode();
            double d = internalReadDouble(code);
            return d;
        }

        public float readFloat()
        {
            int code = readNextCode();
            float result;
            switch (code)
            {
                case Codes.FLOAT:
                    result = rawInput.readRawFloat();
                    break;
                default:
                    {
                        Object o = read(code);
                        if (o is Single)
                        {
                            return (Single)o;
                        }
                        else
                        {
                            throw Fns.expected("float", code, o);
                        }
                    }
            }
            return result;
        }

        public Object readObject()
        {
            return read(readNextCode());
        }

        private Object read(int code)
        {
            Object result;
            switch (code)
            {

                //INT_PACKED_1_FIRST
                case 0xFF:
                    result = -1L;
                    break;

                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    result = (long)code & 0xff;
                    break;

                //  INT_PACKED_2_FIRST
                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                case 0x44:
                case 0x45:
                case 0x46:
                case 0x47:
                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                case 0x4C:
                case 0x4D:
                case 0x4E:
                case 0x4F:
                case 0x50:
                case 0x51:
                case 0x52:
                case 0x53:
                case 0x54:
                case 0x55:
                case 0x56:
                case 0x57:
                case 0x58:
                case 0x59:
                case 0x5A:
                case 0x5B:
                case 0x5C:
                case 0x5D:
                case 0x5E:
                case 0x5F:
                    result = ((long)(code - Codes.INT_PACKED_2_ZERO) << 8) | rawInput.readRawInt8();
                    break;

                //  INT_PACKED_3_FIRST
                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x66:
                case 0x67:
                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6E:
                case 0x6F:
                    result = ((long)(code - Codes.INT_PACKED_3_ZERO) << 16) | rawInput.readRawInt16();
                    break;

                //  INT_PACKED_4_FIRST
                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                    result = ((long)(code - Codes.INT_PACKED_4_ZERO << 24)) | rawInput.readRawInt24();
                    break;

                //  INT_PACKED_5_FIRST
                case 0x74:
                case 0x75:
                case 0x76:
                case 0x77:
                    result = ((long)(code - Codes.INT_PACKED_5_ZERO) << 32) | rawInput.readRawInt32();
                    break;

                //  INT_PACKED_6_FIRST
                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                    result = (((long)code - Codes.INT_PACKED_6_ZERO) << 40) | rawInput.readRawInt40();
                    break;

                //  INT_PACKED_7_FIRST
                case 0x7C:
                case 0x7D:
                case 0x7E:
                case 0x7F:
                    result = (((long)code - Codes.INT_PACKED_7_ZERO) << 48) | rawInput.readRawInt48();
                    break;

                case Codes.PUT_PRIORITY_CACHE:
                    result = readAndCacheObject(getPriorityCache());
                    break;

                case Codes.GET_PRIORITY_CACHE:
                    result = lookupCache(getPriorityCache(), readInt32());
                    break;

                case Codes.PRIORITY_CACHE_PACKED_START + 0:
                case Codes.PRIORITY_CACHE_PACKED_START + 1:
                case Codes.PRIORITY_CACHE_PACKED_START + 2:
                case Codes.PRIORITY_CACHE_PACKED_START + 3:
                case Codes.PRIORITY_CACHE_PACKED_START + 4:
                case Codes.PRIORITY_CACHE_PACKED_START + 5:
                case Codes.PRIORITY_CACHE_PACKED_START + 6:
                case Codes.PRIORITY_CACHE_PACKED_START + 7:
                case Codes.PRIORITY_CACHE_PACKED_START + 8:
                case Codes.PRIORITY_CACHE_PACKED_START + 9:
                case Codes.PRIORITY_CACHE_PACKED_START + 10:
                case Codes.PRIORITY_CACHE_PACKED_START + 11:
                case Codes.PRIORITY_CACHE_PACKED_START + 12:
                case Codes.PRIORITY_CACHE_PACKED_START + 13:
                case Codes.PRIORITY_CACHE_PACKED_START + 14:
                case Codes.PRIORITY_CACHE_PACKED_START + 15:
                case Codes.PRIORITY_CACHE_PACKED_START + 16:
                case Codes.PRIORITY_CACHE_PACKED_START + 17:
                case Codes.PRIORITY_CACHE_PACKED_START + 18:
                case Codes.PRIORITY_CACHE_PACKED_START + 19:
                case Codes.PRIORITY_CACHE_PACKED_START + 20:
                case Codes.PRIORITY_CACHE_PACKED_START + 21:
                case Codes.PRIORITY_CACHE_PACKED_START + 22:
                case Codes.PRIORITY_CACHE_PACKED_START + 23:
                case Codes.PRIORITY_CACHE_PACKED_START + 24:
                case Codes.PRIORITY_CACHE_PACKED_START + 25:
                case Codes.PRIORITY_CACHE_PACKED_START + 26:
                case Codes.PRIORITY_CACHE_PACKED_START + 27:
                case Codes.PRIORITY_CACHE_PACKED_START + 28:
                case Codes.PRIORITY_CACHE_PACKED_START + 29:
                case Codes.PRIORITY_CACHE_PACKED_START + 30:
                case Codes.PRIORITY_CACHE_PACKED_START + 31:
                    result = lookupCache(getPriorityCache(), code - Codes.PRIORITY_CACHE_PACKED_START);
                    break;

                case Codes.STRUCT_CACHE_PACKED_START + 0:
                case Codes.STRUCT_CACHE_PACKED_START + 1:
                case Codes.STRUCT_CACHE_PACKED_START + 2:
                case Codes.STRUCT_CACHE_PACKED_START + 3:
                case Codes.STRUCT_CACHE_PACKED_START + 4:
                case Codes.STRUCT_CACHE_PACKED_START + 5:
                case Codes.STRUCT_CACHE_PACKED_START + 6:
                case Codes.STRUCT_CACHE_PACKED_START + 7:
                case Codes.STRUCT_CACHE_PACKED_START + 8:
                case Codes.STRUCT_CACHE_PACKED_START + 9:
                case Codes.STRUCT_CACHE_PACKED_START + 10:
                case Codes.STRUCT_CACHE_PACKED_START + 11:
                case Codes.STRUCT_CACHE_PACKED_START + 12:
                case Codes.STRUCT_CACHE_PACKED_START + 13:
                case Codes.STRUCT_CACHE_PACKED_START + 14:
                case Codes.STRUCT_CACHE_PACKED_START + 15:
                    {
                        StructType st = (StructType)lookupCache(getStructCache(), code - Codes.STRUCT_CACHE_PACKED_START);
                        result = handleStruct(st.tag, st.fields);
                        break;
                    }

                case Codes.MAP:
                    result = handleStruct("map", 1);
                    break;

                case Codes.SET:
                    result = handleStruct("set", 1);
                    break;

                case Codes.UUID:
                    result = handleStruct("uuid", 2);
                    break;

                case Codes.REGEX:
                    result = handleStruct("regex", 1);
                    break;

                case Codes.URI:
                    result = handleStruct("uri", 1);
                    break;

                case Codes.BIGINT:
                    result = handleStruct("bigint", 1);
                    break;

                case Codes.BIGDEC:
                    result = handleStruct("bigdec", 2);
                    break;

                case Codes.INST:
                    result = handleStruct("inst", 1);
                    break;

                case Codes.SYM:
                    result = handleStruct("sym", 2);
                    break;

                case Codes.KEY:
                    result = handleStruct("key", 2);
                    break;

                case Codes.INT_ARRAY:
                    result = handleStruct("int[]", 2);
                    break;

                case Codes.LONG_ARRAY:
                    result = handleStruct("long[]", 2);
                    break;

                case Codes.FLOAT_ARRAY:
                    result = handleStruct("float[]", 2);
                    break;

                case Codes.BOOLEAN_ARRAY:
                    result = handleStruct("boolean[]", 2);
                    break;

                case Codes.DOUBLE_ARRAY:
                    result = handleStruct("double[]", 2);
                    break;

                case Codes.OBJECT_ARRAY:
                    result = handleStruct("Object[]", 2);
                    break;

                case Codes.BYTES_PACKED_LENGTH_START + 0:
                case Codes.BYTES_PACKED_LENGTH_START + 1:
                case Codes.BYTES_PACKED_LENGTH_START + 2:
                case Codes.BYTES_PACKED_LENGTH_START + 3:
                case Codes.BYTES_PACKED_LENGTH_START + 4:
                case Codes.BYTES_PACKED_LENGTH_START + 5:
                case Codes.BYTES_PACKED_LENGTH_START + 6:
                case Codes.BYTES_PACKED_LENGTH_START + 7:
                    result = internalReadBytes(code - Codes.BYTES_PACKED_LENGTH_START);
                    break;

                case Codes.BYTES:
                    result = internalReadBytes(readCount());
                    break;

                case Codes.BYTES_CHUNK:
                    result = internalReadChunkedBytes();
                    break;

                case Codes.STRING_PACKED_LENGTH_START + 0:
                case Codes.STRING_PACKED_LENGTH_START + 1:
                case Codes.STRING_PACKED_LENGTH_START + 2:
                case Codes.STRING_PACKED_LENGTH_START + 3:
                case Codes.STRING_PACKED_LENGTH_START + 4:
                case Codes.STRING_PACKED_LENGTH_START + 5:
                case Codes.STRING_PACKED_LENGTH_START + 6:
                case Codes.STRING_PACKED_LENGTH_START + 7:
                    result = internalReadString(code - Codes.STRING_PACKED_LENGTH_START).ToString();
                    break;

                case Codes.STRING:
                    result = internalReadString(readCount()).ToString();
                    break;

                case Codes.STRING_CHUNK:
                    result = internalReadChunkedString(readCount());
                    break;

                case Codes.LIST_PACKED_LENGTH_START + 0:
                case Codes.LIST_PACKED_LENGTH_START + 1:
                case Codes.LIST_PACKED_LENGTH_START + 2:
                case Codes.LIST_PACKED_LENGTH_START + 3:
                case Codes.LIST_PACKED_LENGTH_START + 4:
                case Codes.LIST_PACKED_LENGTH_START + 5:
                case Codes.LIST_PACKED_LENGTH_START + 6:
                case Codes.LIST_PACKED_LENGTH_START + 7:
                    result = internalReadList(code - Codes.LIST_PACKED_LENGTH_START);
                    break;

                case Codes.LIST:
                    result = internalReadList(readCount());
                    break;

                case Codes.BEGIN_CLOSED_LIST:
                    result = ((Func<Object[], IList>)getHandler("list"))(readClosedList());
                    break;

                case Codes.BEGIN_OPEN_LIST:
                    result = ((Func<Object[], IList>)getHandler("list"))(readOpenList());
                    break;

                case Codes.TRUE:
                    result = true;
                    break;

                case Codes.FALSE:
                    result = false;
                    break;

                case Codes.DOUBLE:
                case Codes.DOUBLE_0:
                case Codes.DOUBLE_1:
                    result = ((Func<double, Object>)getHandler("double"))(internalReadDouble(code));
                    break;

                case Codes.FLOAT:
                    result = ((Func<float, Object>)getHandler("float"))(rawInput.readRawFloat());
                    break;

                case Codes.INT:
                    result = rawInput.readRawInt64();
                    break;

                case Codes.NULL:
                    result = null;
                    break;

                case Codes.FOOTER:
                    {
                        int calculatedLength = rawInput.getBytesRead() - 1;
                        int magicFromStream = (int)((code << 24) + (int)rawInput.readRawInt24());
                        validateFooter(calculatedLength, magicFromStream);
                        return readObject();
                    }
                case Codes.STRUCTTYPE:
                    {
                        Object tag = readObject();
                        int fields = readInt32();
                        getStructCache().Add(new StructType(tag, fields));
                        result = handleStruct(tag, fields);
                        break;
                    }
                case Codes.STRUCT:
                    {
                        StructType st = (StructType)lookupCache(getStructCache(), readInt32());
                        result = handleStruct(st.tag, st.fields);
                        break;
                    }

                case Codes.RESET_CACHES:
                    {
                        resetCaches();
                        result = readObject();
                        break;
                    }


                default:
                    throw Fns.expected("any", code);
            }
            return result;
        }

        private Object handleStruct(Object tag, int fields)
        {
            ReadHandler h = Fns.lookup<object, ReadHandler>(handlerLookup, tag);
            if (h == null)
            {   
                //h = standardExtensionHandlers[tag.ToString()]; //FF ?? 
                if (standardExtensionHandlers.ContainsKey(tag))
                    h = standardExtensionHandlers[tag];
            }
            if (h == null)
                return new TaggedObject(tag, readObjects(fields));
            else
                return h.read(this, tag, fields);
        }

        private int readCount()
        {
            return readInt32();
        }

        private int internalReadInt32()
        {
            return Fns.intCast(internalReadInt());
        }

        private int readInt32()
        {
            return Fns.intCast(readInt());
        }

        private StringBuilder internalReadString(int length)
        {
            return internalReadStringBuffer(new StringBuilder(length), length);
        }

        private StringBuilder internalReadStringBuffer(StringBuilder buf, int length)
        {
            if ((byteBuffer == null) || (byteBuffer.Length < length))
                byteBuffer = new byte[length];
            rawInput.readFully(byteBuffer, 0, length);
            Fns.readUTF8Chars(buf, byteBuffer, 0, length);
            return buf;
        }

        private String internalReadChunkedString(int length)
        {
            StringBuilder buf = internalReadString(length);
            bool done = false;
            while (!done)
            {
                int code = readNextCode();
                switch (code)
                {
                    case Codes.STRING_PACKED_LENGTH_START + 0:
                    case Codes.STRING_PACKED_LENGTH_START + 1:
                    case Codes.STRING_PACKED_LENGTH_START + 2:
                    case Codes.STRING_PACKED_LENGTH_START + 3:
                    case Codes.STRING_PACKED_LENGTH_START + 4:
                    case Codes.STRING_PACKED_LENGTH_START + 5:
                    case Codes.STRING_PACKED_LENGTH_START + 6:
                    case Codes.STRING_PACKED_LENGTH_START + 7:
                        internalReadStringBuffer(buf, code - Codes.STRING_PACKED_LENGTH_START).ToString();
                        done = true;
                        break;

                    case Codes.STRING:
                        internalReadStringBuffer(buf, readCount());
                        done = true;
                        break;

                    case Codes.STRING_CHUNK:
                        internalReadStringBuffer(buf, readCount());
                        break;
                    default:
                        throw Fns.expected("chunked string", code);
                }
            }
            return buf.ToString();
        }

        private byte[] internalReadBytes(int length)
        {
            byte[] result = new byte[length];
            rawInput.readFully(result, 0, length);
            return result;
        }

        private byte[] internalReadChunkedBytes()
        {
            IList<byte[]> chunks = new List<byte[]>();
            int code = Codes.BYTES_CHUNK;
            while (code == Codes.BYTES_CHUNK)
            {
                chunks.Add(internalReadBytes(readCount()));
                code = readNextCode();
            }
            if (code != Codes.BYTES)
            {
                throw Fns.expected("conclusion of chunked bytes", code);
            }
            chunks.Add(internalReadBytes(readCount()));
            int length = 0;
            for (int n = 0; n < chunks.Count; n++)
            {
                length = length + chunks[n].Length;
            }
            byte[] result = new byte[length];
            int pos = 0;
            for (int n = 0; n < chunks.Count; n++)
            {
                Array.Copy(chunks[n], 0, result, pos, chunks[n].Length);
                pos += chunks[n].Length;
            }
            return result;
        }

        private Object getHandler(String tag)
        {
            Object o = coreHandlers[tag];
            if (o == null)
            {
                throw new ApplicationException("No read handler for type " + tag);
            }
            return o;
        }

        private double internalReadDouble(int code)
        {
            switch (code)
            {
                case Codes.DOUBLE:
                    return rawInput.readRawDouble();
                case Codes.DOUBLE_0:
                    return 0.0D;
                case Codes.DOUBLE_1:
                    return 1.0D;
                default:
                    {
                        Object o = read(code);
                        if (o is Double)
                        {
                            return (Double)o;
                        }
                        else
                        {
                            throw Fns.expected("double", code, o);
                        }
                    }
            }
        }

        private Object[] readObjects(int length)
        {
            Object[] objects = new Object[length];
            for (int n = 0; n < length; n++)
            {
                objects[n] = readObject();
            }
            return objects;
        }

        private Object[] readClosedList()
        {
            ArrayList objects = new ArrayList();
            while (true)
            {
                int code = readNextCode();
                if (code == Codes.END_COLLECTION)
                {
                    return objects.ToArray();
                }
                objects.Add(read(code));
            }
        }

        private Object[] readOpenList()
        {
            ArrayList objects = new ArrayList();
            int code;
            while (true)
            {
                try
                {
                    code = readNextCode();
                }
                catch (EndOfStreamException e)
                {
                    code = Codes.END_COLLECTION;
                }
                if (code == Codes.END_COLLECTION)
                {
                    return objects.ToArray();
                }
                objects.Add(read(code));
            }
        }

        //FF - need immutable keyval pair ??
        //static class MapEntry : Map.Entry {
        //    public readonly Object key;
        //    public readonly Object value;

        //    public MapEntry(Object key, Object value) {
        //        this.key = key;
        //        this.value = value;
        //    }

        //    public Object getKey() {
        //        return key;
        //    }

        //    public Object getValue() {
        //        return value;
        //    }

        //    public Object setValue(Object o) {
        //        throw new NotSupportedException();
        //    }
        //}

        // placeholder for objects still being read in
        static private Object UNDER_CONSTRUCTION = new Object();

        private Object lookupCache(ArrayList cache, int index)
        {
            if (index < cache.Count)
            {
                Object result = cache[index];
                if (result == UNDER_CONSTRUCTION)
                    throw new ApplicationException("Unable to resolve circular reference in cache");
                else
                    return result;
            }
            else
            {
                throw new ApplicationException("Requested object beyond end of cache at " + index);
            }
        }

        private IList internalReadList(int length)
        {
            var h = (Func<Object[], IList>)getHandler("list");
            return h(readObjects(length));
        }

        private void validateFooter(int calculatedLength, int magicFromStream)
        {
            if (magicFromStream != Codes.FOOTER_MAGIC)
            {
                throw new ApplicationException(String.Format("Invalid footer magic, expected %X got %X", Codes.FOOTER_MAGIC, magicFromStream));
            }
            int lengthFromStream = (int)rawInput.readRawInt32();
            if (lengthFromStream != calculatedLength)
            {
                throw new ApplicationException(String.Format("Invalid footer length, expected %X got %X", calculatedLength, lengthFromStream));
            }
            rawInput.validateChecksum();
            rawInput.reset();
            resetCaches();
        }

        private ArrayList getPriorityCache()
        {
            if (priorityCache == null) priorityCache = new ArrayList();
            return priorityCache;
        }

        private ArrayList getStructCache()
        {
            if (structCache == null) structCache = new ArrayList();
            return structCache;
        }

        private void resetCaches()
        {
            if (priorityCache != null) priorityCache.Clear();
            if (structCache != null) structCache.Clear();
        }

        public void validateFooter()
        {
            int calculatedLength = rawInput.getBytesRead();
            int magicFromStream = (int)rawInput.readRawInt32();
            validateFooter(calculatedLength, magicFromStream);
        }

        private int readNextCode()
        {
            return rawInput.readRawByte();
        }

        private Object readAndCacheObject(ArrayList cache)
        {
            int index = cache.Count;
            cache.Add(UNDER_CONSTRUCTION);
            Object o = readObject();
            cache[index] = o;
            return o;
        }

        public static readonly IDictionary<object, object> coreHandlers;

        static FressianReader()
        {
            IDictionary<object, object> handlers = new Dictionary<object, object>();
            handlers.Add("list", new Func<Object[], IList>(x => x.ToList()));
            handlers.Add("bytes", new Func<byte[], Object>(x => x));
            handlers.Add("double", new Func<double, Object>(x => x));
            handlers.Add("float", new Func<float, Object>(x => x));
            coreHandlers = new ImmutableDictionary<object, object>(handlers);
        }

        //public void close()
        //{
        //    istream.close();
        //}

        public void Dispose()
        {
            this.rawInput.Dispose();
        }        
    }
}