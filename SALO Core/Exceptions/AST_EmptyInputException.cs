using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_EmptyInputException : AST_BadFormatException
	{
		public AST_EmptyInputException()
		{
		}
		public AST_EmptyInputException(string message)
				: base(message)
		{
		}
		public AST_EmptyInputException(string message, Exception inner)
				: base(message, inner)
		{
		}
	}
}
