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
    public class Codes
    {
        public const int PRIORITY_CACHE_PACKED_START = 0x80;
        public const int PRIORITY_CACHE_PACKED_END = 0xA0;
        public const int STRUCT_CACHE_PACKED_START = 0xA0;
        public const int STRUCT_CACHE_PACKED_END = 0xB0;
        public const int LONG_ARRAY = 0xB0;
        public const int DOUBLE_ARRAY = 0xB1;
        public const int BOOLEAN_ARRAY = 0xB2;
        public const int INT_ARRAY = 0xB3;
        public const int FLOAT_ARRAY = 0xB4;
        public const int OBJECT_ARRAY = 0xB5;
        public const int MAP = 0xC0;
        public const int SET = 0xC1;
        public const int UUID = 0xC3;
        public const int REGEX = 0xC4;
        public const int URI = 0xC5;
        public const int BIGINT = 0xC6;
        public const int BIGDEC = 0xC7;
        public const int INST = 0xC8;
        public const int SYM = 0xC9;
        public const int KEY = 0xCA;
        public const int GET_PRIORITY_CACHE = 0xCC;
        public const int PUT_PRIORITY_CACHE = 0xCD;
        public const int PRECACHE = 0xCE;
        public const int FOOTER = 0xCF;
        public const int FOOTER_MAGIC = unchecked((int)0xCFCFCFCF); //FF ?? come back to this cast
        public const int BYTES_PACKED_LENGTH_START = 0xD0;
        public const int BYTES_PACKED_LENGTH_END = 0xD8;
        public const int BYTES_CHUNK = 0xD8;
        public const int BYTES = 0xD9;
        public const int STRING_PACKED_LENGTH_START = 0xDA;
        public const int STRING_PACKED_LENGTH_END = 0xE2;
        public const int STRING_CHUNK = 0xE2;
        public const int STRING = 0xE3;
        public const int LIST_PACKED_LENGTH_START = 0xE4;
        public const int LIST_PACKED_LENGTH_END = 0xEC;
        public const int LIST = 0xEC;
        public const int BEGIN_CLOSED_LIST = 0xED;
        public const int BEGIN_OPEN_LIST = 0xEE;
        public const int STRUCTTYPE = 0xEF;
        public const int STRUCT = 0xF0;
        public const int META = 0xF1;
        public const int ANY = 0xF4;
        public const int TRUE = 0xF5;
        public const int FALSE = 0xF6;
        public const int NULL = 0xF7;
        public const int INT = 0xF8;
        public const int FLOAT = 0xF9;
        public const int DOUBLE = 0xFA;
        public const int DOUBLE_0 = 0xFB;
        public const int DOUBLE_1 = 0xFC;
        public const int END_COLLECTION = 0xFD;
        public const int RESET_CACHES = 0xFE;
        public const int INT_PACKED_1_START = 0xFF;
        public const int INT_PACKED_1_END = 0x40;
        public const int INT_PACKED_2_START = 0x40;
        public const int INT_PACKED_2_ZERO = 0x50;
        public const int INT_PACKED_2_END = 0x60;
        public const int INT_PACKED_3_START = 0x60;
        public const int INT_PACKED_3_ZERO = 0x68;
        public const int INT_PACKED_3_END = 0x70;
        public const int INT_PACKED_4_START = 0x70;
        public const int INT_PACKED_4_ZERO = 0x72;
        public const int INT_PACKED_4_END = 0x74;
        public const int INT_PACKED_5_START = 0x74;
        public const int INT_PACKED_5_ZERO = 0x76;
        public const int INT_PACKED_5_END = 0x78;
        public const int INT_PACKED_6_START = 0x78;
        public const int INT_PACKED_6_ZERO = 0x7A;
        public const int INT_PACKED_6_END = 0x7C;
        public const int INT_PACKED_7_START = 0x7C;
        public const int INT_PACKED_7_ZERO = 0x7E;
        public const int INT_PACKED_7_END = 0x80;
    }
}