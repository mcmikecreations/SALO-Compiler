using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_BadFormatException : AST_Exception
	{
		public AST_BadFormatException()
		{
		}
		public AST_BadFormatException(string message)
				: base(message)
		{
		}
		public AST_BadFormatException(string message, Exception inner)
				: base(message, inner)
		{
		}
	}
}
