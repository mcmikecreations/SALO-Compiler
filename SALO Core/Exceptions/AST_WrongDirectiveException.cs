using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_WrongDirectiveException : AST_Exception
	{
		public AST_WrongDirectiveException()
		{
		}
		public AST_WrongDirectiveException(string message)
				: base(message)
		{
		}
		public AST_WrongDirectiveException(string message, Exception inner)
				: base(message, inner)
		{
		}
	}
}
