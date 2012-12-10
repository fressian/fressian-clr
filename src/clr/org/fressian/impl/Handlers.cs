//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Contributors:  Frank Failla
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Linq;

using org.fressian.handlers;

namespace org.fressian.impl
{
    public static class Handlers
    {
        public static readonly IDictionary<object, Int32> tagToCode;
        public static readonly IDictionary<object, ReadHandler> extendedReadHandlers;
        public static readonly org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> coreWriteHandlers;
        public static readonly org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> extendedWriteHandlers;

        public class GenericReadHandler<T> : ReadHandler
        {
            internal Func<Reader, Object, int, T> _fn;

            public GenericReadHandler(Func<Reader, Object, int, T> fn)
            {
                this._fn = fn;
            }

            public object read(Reader r, object tag, int componentCount)
            {
                return this._fn(r, tag, componentCount);
            }
        }

        public class GenericWriteHandler<T> : WriteHandler
        {
            internal Action<Writer, object> _fn;

            public GenericWriteHandler(Action<Writer, object> fn)
            {
                this._fn = fn;
            }

            public void write(Writer w, object instance)
            {
                this._fn(w, instance);
            }
        }

        public sealed class Null
        {
            public static readonly Null Value = new Null();
            private Null() { }
        }

        public static IDictionary<object, object> ParseNullKeyDictionary(IEnumerable o)
        {
            var ret = new Dictionary<object, object>();
            foreach(KeyValuePair<object, object> kvp in o)
            {
                if (kvp.Key == null)
                    ret[Handlers.Null.Value] = kvp.Value;
                else
                    ret[kvp.Key] = kvp.Value;
            }
            return ret;
        }

        static Handlers()
        {
            tagToCode = initTagToCode();
            extendedReadHandlers = initExtendedReadHandlers();
            coreWriteHandlers = initCoreWriteHandlers();
            extendedWriteHandlers = initExtendedWriteHandlers();        
        }

        private static IDictionary<object, Int32> initTagToCode()
        {
            IDictionary<object, int> builder = new Dictionary<object, int>();
            builder["map"] = Codes.MAP;
            builder["set"] = Codes.SET;
            builder["uuid"] = Codes.UUID;
            builder["regex"] = Codes.REGEX;
            builder["uri"] = Codes.URI;
            builder["bigint"] = Codes.BIGINT;
            builder["bigdec"] = Codes.BIGDEC;
            builder["inst"] = Codes.INST;
            builder["sym"] = Codes.SYM;
            builder["key"] = Codes.KEY;
            builder["int[]"] = Codes.INT_ARRAY;
            builder["float[]"] = Codes.FLOAT_ARRAY;
            builder["double[]"] = Codes.DOUBLE_ARRAY;
            builder["long[]"] = Codes.LONG_ARRAY;
            builder["boolean[]"] = Codes.BOOLEAN_ARRAY;
            builder["Object[]"] = Codes.OBJECT_ARRAY;
            return new ImmutableDictionary<object, int>(builder);
        }

        public static IDictionary<Type, IDictionary<String, WriteHandler>> installHandler(IDictionary<Type, IDictionary<String, WriteHandler>> map
                                                                                                    , Type cls
                                                                                                    , String tag
                                                                                                    , WriteHandler handler)
        {
            map[cls] = Fns.soloMap(tag, handler);
            return map;
        }

        private static org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> initCoreWriteHandlers()
        {
            IDictionary<Type, IDictionary<String, WriteHandler>> handlers = new Dictionary<Type, IDictionary<String, WriteHandler>>();
            var intHandler = new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeInt(o)));
            installHandler(handlers, typeof(short), "int", intHandler);
            installHandler(handlers, typeof(int), "int", intHandler);
            installHandler(handlers, typeof(long), "int", intHandler);
            installHandler(handlers, typeof(bool), "bool", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeBoolean(o))));
            installHandler(handlers, (new byte[0]).GetType(), "bytes", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeBytes((byte[])o))));
            installHandler(handlers, typeof(double), "double", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeDouble(o))));
            installHandler(handlers, typeof(float), "float", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeFloat(o))));
            installHandler(handlers, typeof(string), "string", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeString(o))));
            //FF - c# dictionaries cannot key on null
            //installHandler(handlers, null, "null", new Action<Writer, object>((w, o) => w.writeNull()));
            installHandler(handlers, typeof(Null), "null", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeNull())));
            installHandler(handlers, (new int[] { }).GetType(), "int[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                int[] ints = (int[])o;
                w.writeTag("int[]", 2);
                w.writeInt(ints.Length);
                for (int n = 0; n < ints.Length; n++)
                {
                    w.writeInt(ints[n]);
                }
            })));
            installHandler(handlers, (new long[] { }).GetType(), "long[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                long[] longs = (long[])o;
                w.writeTag("long[]", 2);
                w.writeInt(longs.Length);
                for (int n = 0; n < longs.Length; n++)
                {
                    w.writeInt(longs[n]);
                }
            })));
            installHandler(handlers, (new float[] { }).GetType(), "float[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                float[] floats = (float[])o;
                w.writeTag("float[]", 2);
                w.writeInt(floats.Length);
                for (int n = 0; n < floats.Length; n++)
                {
                    w.writeFloat(floats[n]);
                }
            })));
            installHandler(handlers, (new bool[] { }).GetType(), "boolean[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                bool[] bools = (bool[])o;
                w.writeTag("boolean[]", 2);
                w.writeInt(bools.Length);
                for (int n = 0; n < bools.Length; n++)
                {
                    w.writeBoolean(bools[n]);
                }
            })));
            installHandler(handlers, (new double[] { }).GetType(), "double[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                double[] doubles = (double[])o;
                w.writeTag("double[]", 2);
                w.writeInt(doubles.Length);
                for (int n = 0; n < doubles.Length; n++)
                {
                    w.writeDouble(doubles[n]);
                }
            })));
            installHandler(handlers, (new Object[] { }).GetType(), "Object[]", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                Object[] objects = (Object[])o;
                w.writeTag("Object[]", 2);
                w.writeInt(objects.Length);
                for (int n = 0; n < objects.Length; n++)
                {
                    w.writeObject(objects[n]);
                }
            })));
            installHandler(handlers, typeof(TaggedObject), "any", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                TaggedObject t = (TaggedObject)o;
                Object[] value = t.Value;
                w.writeTag(t.Tag, value.Length);
                for (int n = 0; n < value.Length; n++)
                {
                    //FF - c# dictionaries cannot have null keys, this is part of the workaround
                    var x = value[n];                    
                    w.writeObject(x==null ? Handlers.Null.Value : x);
                }
            })));

            return new InheritanceLookup<IDictionary<String, WriteHandler>>(new MapLookup<Type, IDictionary<String,WriteHandler>>(handlers));
        }

        private static IDictionary<object, ReadHandler> initExtendedReadHandlers()
        {
            IDictionary<object, ReadHandler> handlers = new Dictionary<object, ReadHandler>();

            handlers["set"] = new GenericReadHandler<ISet<object>>(new Func<Reader, Object, int, ISet<object>>((r, t, c) =>
            {
                ISet<object> s = new HashSet<object>();
                s.UnionWith((IList<object>)r.readObject());
                return s;
            }));

            handlers["map"] = new GenericReadHandler<IDictionary<object, object>>(new Func<Reader, Object, int, IDictionary<object, object>>((r, t, c) =>
            {
                IDictionary<object, object> m = new Dictionary<object, object>();
                //FF - java impl casted to RandomAccess for which there is no c# equivilent 
                //List kvs = (List) (RandomAccess) r.readObject();
                IList<object> kvs = (IList<object>)r.readObject();
                for (int i = 0; i < kvs.Count; i += 2)
                {
                    if (kvs[i] == null)
                        m[Handlers.Null.Value] = kvs[i + 1];
                    else
                        m[kvs[i]] = kvs[i + 1];

                    //m[kvs[i]] = kvs[i + 1];
                }
                return m;
            }));

            handlers["int[]"] = new GenericReadHandler<int[]>(new Func<Reader, Object, int, int[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                int[] result = new int[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = Fns.intCast(r.readInt());
                }
                return result;
            }));

            handlers["long[]"] = new GenericReadHandler<long[]>(new Func<Reader, Object, int, long[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                long[] result = new long[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = r.readInt();
                }
                return result;
            }));

            handlers["float[]"] = new GenericReadHandler<float[]>(new Func<Reader, Object, int, float[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                float[] result = new float[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = r.readFloat();
                }
                return result;
            }));

            handlers["boolean[]"] = new GenericReadHandler<bool[]>(new Func<Reader, Object, int, bool[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                bool[] result = new bool[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = r.readBoolean();
                }
                return result;
            }));

            handlers["double[]"] = new GenericReadHandler<double[]>(new Func<Reader, Object, int, double[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                double[] result = new double[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = r.readDouble();
                }
                return result;
            }));

            handlers["Object[]"] = new GenericReadHandler<Object[]>(new Func<Reader, Object, int, Object[]>((r, t, c) =>
            {
                int size = Fns.intCast(r.readInt());
                Object[] result = new Object[size];
                for (int n = 0; n < size; n++)
                {
                    result[n] = r.readObject();
                }
                return result;
            }));

            handlers["uuid"] = new GenericReadHandler<object>(new Func<Reader, Object, int, object>((r, t, c) =>
            {
                byte[] buf = (byte[])r.readObject();
                if (buf.Length != 16) throw new System.IO.IOException("Invalid uuid buffer size: " + buf.Length);
                //FF ?? Guid is a struct, but now is boxed to an object
                return Fns.byteArrayToUUID(buf);
            }));

            handlers["regex"] = new GenericReadHandler<Regex>(new Func<Reader, Object, int, Regex>((r, t, c) =>
            {
                return new System.Text.RegularExpressions.Regex((String)r.readObject());
            }));

            handlers["uri"] = new GenericReadHandler<Uri>(new Func<Reader, Object, int, Uri>((r, t, c) =>
            {
                try
                {
                    return new Uri((String)r.readObject());
                }
                catch (UriFormatException e)
                {
                    throw new ApplicationException("Invalid Uri string!", e);
                }
            }));

            handlers["bigint"] = new GenericReadHandler<BigInteger>(new Func<Reader, Object, int, BigInteger>((r, t, c) =>
            {
                return new System.Numerics.BigInteger((byte[])r.readObject());
            }));

            handlers["bigdec"] = new GenericReadHandler<decimal>(new Func<Reader, Object, int, decimal>((r, t, c) =>
            {
                var d = (byte[])r.readObject();
                var s = (int)r.readInt();
                return Fns.DecimalValueFrom(d, s);
            }));

            handlers["inst"] = new GenericReadHandler<DateTime>(new Func<Reader, Object, int, DateTime>((r, t, c) =>
            {
                return toDateTime(r.readInt());
            }));
            
            return new ImmutableDictionary<object, ReadHandler>(handlers);
        }

        public static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static long getTime(DateTime d)
        {
            var tms = d.ToUniversalTime().Subtract(EPOCH).TotalMilliseconds;
            return (long)tms;
        }

        internal static DateTime toDateTime(long t)
        {
            return EPOCH.AddMilliseconds(t);
        }

        private static org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> initExtendedWriteHandlers()
        {
            IDictionary<Type, IDictionary<String, WriteHandler>> handlers = new Dictionary<Type, IDictionary<String, WriteHandler>>();
            var intHandler = new Action<Writer, object>((w, o) => w.writeInt(o));

            installHandler(handlers, typeof(IList<object>), "list", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) => w.writeList(o))));
            installHandler(handlers, typeof(DateTime), "inst", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                w.writeTag("inst", 1);
                w.writeInt(getTime(((DateTime)o)));

            })));
            installHandler(handlers, typeof(ISet<object>), "set", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                w.writeTag("set", 1);
                w.writeList(((ISet<object>)o).ToList());

            })));
            installHandler(handlers, typeof(IDictionary<object, object>), "map", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                w.writeTag("map", 1);
                IDictionary<Object, Object> m = (IDictionary<object, object>)o;
                IList<object> l = new List<object>();
                foreach (var e in m)
                {
                    l.Add(e.Key);
                    l.Add(e.Value);
                }
                w.writeList(l);
            })));

            installHandler(handlers, typeof(BigInteger), "bigint", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                BigInteger b = (BigInteger)o;
                w.writeTag("bigint", 1);
                w.writeBytes(b.ToByteArray());
            })));

            installHandler(handlers, typeof(decimal), "bigdec", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                decimal d = (decimal)o;
                var unscaledVals = Fns.UnscaledValues(d);
                w.writeTag("bigdec", 2);
                w.writeBytes(unscaledVals.Item1);
                w.writeInt(unscaledVals.Item2);
            })));

            installHandler(handlers, typeof(Regex), "regex", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                Regex re = (Regex)o;
                w.writeTag("regex", 1);
                w.writeString(re.ToString());
            })));

            installHandler(handlers, typeof(Uri), "uri", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                Uri uri = (Uri)o;
                w.writeTag("uri", 1);
                w.writeString(uri.ToString());
            })));

            installHandler(handlers, typeof(Guid), "uuid", new GenericWriteHandler<object>(new Action<Writer, object>((w, o) =>
            {
                Guid uuid = (Guid)o;
                w.writeTag("uuid", 1);
                w.writeBytes(Fns.UUIDtoByteArray(uuid));
            })));

            return new InheritanceLookup<IDictionary<String, WriteHandler>>(new MapLookup<Type, IDictionary<String, WriteHandler>>(handlers));
        }

        public static org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> defaultWriteHandlers()
        {
            return new CachingLookup<Type, IDictionary<String, WriteHandler>>
                (new ChainedLookup<Type, IDictionary<String, WriteHandler>>(coreWriteHandlers, extendedWriteHandlers));
        }

        public static org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> customWriteHandlers(org.fressian.handlers.ILookup<Type, IDictionary<String, WriteHandler>> userHandlers)
        {
            if (userHandlers != null)
            {
                return new CachingLookup<Type, IDictionary<String, WriteHandler>>
                    (new ChainedLookup<Type, IDictionary<String, WriteHandler>>(coreWriteHandlers, userHandlers, extendedWriteHandlers));
            }
            else
            {
                return defaultWriteHandlers();
            }
        }
    }
}