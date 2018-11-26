using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SALO_Core;
using SALO_Core.Tools;
using SALO_Core.Builders;
using SALO_Core.Exceptions;

namespace SALO_Compiler
{
	class Program
	{
		static string ChangeEncoding(string text, string encSrc, string encDst)
		{

			//Encoding utf8 = Encoding.GetEncoding("UTF-8");
			//Encoding win1251 = Encoding.GetEncoding("Windows-1251");
			Encoding eSrc = Encoding.GetEncoding(encSrc);
			Encoding eDst = Encoding.GetEncoding(encDst);

			byte[] bytesSrc = eDst.GetBytes(text);
			byte[] bytesDst = Encoding.Convert(eSrc, eDst, bytesSrc);

			return eDst.GetString(bytesDst);
		}
		static void Main(string[] args)
		{
			Encoding utf8 = Encoding.GetEncoding("UTF-8");
			string utf8src = File.ReadAllText(args[0], utf8);

			Console.OutputEncoding = utf8;

			Console.WriteLine(utf8src);

			//TODO - parse string
			try
			{
				Builder_Global builder_Global = new Builder_Global(utf8src);
				string ast = "";
				builder_Global.AST.Print(ref ast);
				Console.WriteLine("AST:");
				Console.WriteLine(ast);
			}
			catch(SALO_Exception e)
			{
				ExceptionHandler.ShowException(e);
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Unhandled exception: " + e.Message);
			}

			Console.ReadLine();
		}
	}
}
