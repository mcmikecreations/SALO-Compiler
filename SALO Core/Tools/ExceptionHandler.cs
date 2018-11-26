using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.Tools
{
	public static class ExceptionHandler
	{
		public static void ShowException(SALO_Exception e)
		{
			Exception ex = e;
			while (ex != null)
			{
				if(ex is AST_Exception)
				{
					Console.Error.WriteLine("AST Error caught: " + ex.Message);
				}
				else if(ex is SALO_Exception)
				{
					Console.Error.WriteLine("SALO Error caught: " + ex.Message);
				}
				else
				{
					Console.Error.WriteLine("Error caught: " + ex.Message);
				}
				ex = ex.InnerException;
			}
		}
	}
}
