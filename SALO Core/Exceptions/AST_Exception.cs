using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_Exception : SALO_Exception
	{
		public AST_Exception()
		{
		}
		public AST_Exception(string message)
				: base(message)
		{
		}
		public AST_Exception(string message, Exception inner)
				: base(message, inner)
		{
		}
	}
}
