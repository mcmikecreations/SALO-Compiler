using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class SALO_Exception : Exception
	{
		public SALO_Exception()
		{
		}
		public SALO_Exception(string message)
				: base(message)
		{
		}
		public SALO_Exception(string message, Exception inner)
				: base(message, inner)
		{
		}
	}
}
