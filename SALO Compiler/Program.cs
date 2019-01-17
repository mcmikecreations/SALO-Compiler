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
		static readonly string help = "SALO Compiler 0.1\r\n" +
			"To use this command-line tool, input input file name, followed by one or more of these parameters:\r\n" +
			"\t\"-eng\" for writing translated input file into a separate .txt file\r\n" +
			"\t\"-ast\" for writing abstract syntax tree into a separate .txt file\r\n" +
			"\t\"-nb\" for not creating an output file (for debug purposes)\r\n";
		static void Main(string[] args)
		{
			if(args == null || args.Length < 1)
			{
				Console.Write(help);
				return;
			}

			Encoding utf8 = Encoding.GetEncoding("UTF-8");
			//TODO - check if file exists, if arguments are correct, etc.
			string utf8src = File.ReadAllText(args[0], utf8);

			Console.OutputEncoding = utf8;

			Console.WriteLine(utf8src);

			//TODO - parse string
			Builder_Global builder_Global = null;
			try
			{
				builder_Global = new Builder_Global(utf8src, Language.Assembler);

				string fileName = Path.GetFileNameWithoutExtension(args[0]);
				if ((args.Length > 1 && args[1] == "-eng") || 
					(args.Length > 2 && args[2] == "-eng") || 
					(args.Length > 3 && args[3] == "-eng"))
				{
					File.WriteAllText(fileName + "_translated.txt", builder_Global.Translator.Translated);
				}
				if ((args.Length > 1 && args[1] == "-ast") || 
					(args.Length > 2 && args[2] == "-ast") || 
					(args.Length > 3 && args[3] == "-ast"))
				{
					string ast = "";
					builder_Global.AST.Print(ref ast);
					File.WriteAllText(fileName + "_ast.txt", ast);
				}
				if((args.Length < 2 || args[1] != "-nb") && 
					(args.Length < 3 || args[2] != "-nb") && 
					(args.Length < 4 || args[3] != "-nb"))
				{
					File.WriteAllText(fileName + ".asm", builder_Global.Compiler.Result);
				}
			}
			catch (SALO_Exception e)
			{
				ExceptionHandler.ShowException(e);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Unhandled exception: " + e.Message);
			}
#if DEBUG
			Console.ReadLine();
#endif
		}
	}
}
