using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_BadFormatException : AST_Exception
	{
		public AST_BadFormatException(int charIndex) : base(charIndex)
		{
		}
		public AST_BadFormatException(string message, int charIndex)
				: base(message, charIndex)
		{
		}
		public AST_BadFormatException(string message, Exception inner, int charIndex)
				: base(message, inner, charIndex)
		{
		}
	}
}
