using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST.Data
{
	public class AST_Type : AST_Data
	{
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			string[] var = input.Split(AST_Program.separator_ast.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (var.Length != 1)
			{
				throw new AST_BadFormatException("Return value was given in a wrong format: " + input,
							new FormatException("Return value format is <type>₴"));
			}
			//TODO - allow custom types, created by user
			switch (var[0])
			{
				case "void":
					dataType = DataType.Void;
					break;
				case "int32":
					dataType = DataType.Int32;
					break;
				case "int8":
					dataType = DataType.Int8;
					break;
				case "float32":
					dataType = DataType.Float32;
					break;
				case "bool":
					dataType = DataType.Bool;
					break;
				default:
					throw new AST_BadFormatException("Format " + var[0] + " is not supported");
			}
			data = var[0];
		}
		public override void Print(string indent, bool last, ref string output)
		{
			output += indent;
			if (last)
			{
				output += "\\-";
				indent += "  ";
			}
			else
			{
				output += "|-";
				indent += "| ";
			}
			output += "Type: " + dataType.ToString() + "\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Type(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}
