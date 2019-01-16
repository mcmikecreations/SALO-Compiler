using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions.ASS
{
	class ASS_Exception : SALO_Exception
	{
		public int CharIndex { get; private set; }
		public ASS_Exception(int charIndex)
		{
			this.CharIndex = charIndex;
		}
		public ASS_Exception(string message, int charIndex)
				: base(message)
		{
			this.CharIndex = charIndex;
		}
		public ASS_Exception(string message, Exception inner, int charIndex)
				: base(message, inner)
		{
			this.CharIndex = charIndex;
		}
	}
}
