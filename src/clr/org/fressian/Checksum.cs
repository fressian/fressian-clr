using System;

namespace org.fressian
{
	public interface Checksum
	{
        long Value { get; }		
		void Reset();
		void Update(byte[] buffer, int offset, int count);
	}
}