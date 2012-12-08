using System;
using System.IO;

namespace org.fressian
{
    public class CheckedOutputStream : System.IO.Stream
    {
        protected Stream _stream;
        protected Checksum _checksum;

        public override bool CanRead 
        { 
            get { return false; } 
        }
        
        public override bool CanSeek 
        { 
            get { return false; } 
        }
        
        public override bool CanWrite 
        { 
            get { return true; } 
        }

        public override long Position
        {
            get { return this._stream.Position; }
            set { throw new InvalidOperationException("Setting the position on a CheckedOutputStream is not permitted."); }
        }

        public override long Length
        {
            get { return this._stream.Length; }
        }

        public CheckedOutputStream(Stream stream, Checksum checksum)
        {
            this._stream = stream;
            this._checksum = checksum;
        }
       
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Only write operations are supported on a CheckedIOututStream");
        }
        
        public override int ReadByte()
        {
            throw new InvalidOperationException("Only write operations are supported on a CheckedIOututStream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this._stream.Write(buffer, offset, count);
            this._checksum.Update(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this._stream.WriteByte(value);
            this._checksum.Update(new byte[] { value }, 0, 1);
        }

        public override void Flush()
        {
            this._stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Seek is not supported on a CheckedOutputStream");
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("SetLength is not supported on a CheckedOutputStream");
        }

        public Checksum GetChecksum()
        {
            return this._checksum;
        }        
    }
}