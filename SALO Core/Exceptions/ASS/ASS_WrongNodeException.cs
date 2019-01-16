using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions.ASS
{
	class ASS_WrongNodeException : ASS_Exception
	{
		public ASS_WrongNodeException(int charIndex) : base(charIndex)
		{
			
		}
		public ASS_WrongNodeException(string message, int charIndex)
				: base(message, charIndex)
		{
			
		}
		public ASS_WrongNodeException(string message, Exception inner, int charIndex)
				: base(message, inner, charIndex)
		{
			
		}
	}
}
