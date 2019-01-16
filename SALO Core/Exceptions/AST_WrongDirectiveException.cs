using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_WrongDirectiveException : AST_BadFormatException
	{
		public AST_WrongDirectiveException(int charIndex) : base(charIndex)
		{
		}
		public AST_WrongDirectiveException(string message, int charIndex)
				: base(message, charIndex)
		{
		}
		public AST_WrongDirectiveException(string message, Exception inner, int charIndex)
				: base(message, inner, charIndex)
		{
		}
	}
}
