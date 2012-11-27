using System;

namespace org.fressian
{
	public interface Checksum
	{
        long GetValue();		
		void Reset();
		void Update(byte[] buffer, int offset, int count);
	}
}