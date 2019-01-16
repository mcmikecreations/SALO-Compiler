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
		private static int outputWidth = 20;
		public static void ShowException(SALO_Exception e, string input = null)
		{
			Exception ex = e;
			while (ex != null)
			{
				if (ex is AST_Exception ast_ex)
				{
					if (!string.IsNullOrEmpty(input))
					{
						int index = ast_ex.CharIndex;
						string inputPart = input.Substring(0, index + 1);
						int lineNumber = inputPart.Count(c => c == '\n') + 1;
						int lineStart = inputPart.LastIndexOf('\n') + 1;
						int lineEnd = input.IndexOf('\n', index + 1, 40);
						if(lineEnd == -1)
						{
							if (lineStart + outputWidth < input.Length) lineEnd = lineStart + outputWidth;
							else lineEnd = input.Length - 1;
						}
						string line = 
							input.Substring(lineStart, index - lineStart) + 
							"[Error!]" + 
							input.Substring(index, lineEnd - index);
						Console.Error.WriteLine("Error at line " + lineNumber.ToString() + ":");
						Console.Error.WriteLine(line);
					}
					Console.Error.WriteLine("AST Error caught: " + ast_ex.Message);
				}
				else if (ex is SALO_Exception)
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
