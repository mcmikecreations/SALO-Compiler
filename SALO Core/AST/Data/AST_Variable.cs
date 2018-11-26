using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST.Data
{
	public class AST_Variable : AST_Data
	{
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			string[] vars = input.Split(AST_Program.separator_ast.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (vars.Length != 2)
			{
				if (!(input == "void"))
					throw new AST_BadFormatException("Input parameter was given in a wrong format: " + input,
								new FormatException("Parameter format is <type> <name>₴"));
			}
			//TODO - allow custom types, created by user
			switch (vars[0])
			{
				case "void":
					dataType = DataType.Void;
					data = "void";
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
					throw new AST_BadFormatException("Format " + vars[0] + " is not supported");
			}
			if(vars.Length == 2)
			{
				if (!(char.IsLetter(vars[1][0]) || AST_Expression.naming_ast.Contains(vars[1][0])))
					throw new AST_BadFormatException("Variable name not allowed",
								new FormatException("Variable name should start with a letter or " + AST_Expression.naming_ast));
				data = vars[1];
			}
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
			output += "Variable: " + dataType.ToString() + " " + data + "\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Variable(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}
