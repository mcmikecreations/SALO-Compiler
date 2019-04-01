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
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided string is empty", charIndex);
			string[] var = input.Split(AST_Program.separator_ast.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (var.Length != 1)
			{
				throw new AST_BadFormatException("Return value was given in a wrong format: " + input,
							new FormatException("Return value format is <type>;"), charIndex);
			}
            //TODO - allow custom types, created by user
            try
            {
                dataType = CodeBlocks.ParameterType.GetParameterType(var[0]);
            }
            catch (Exception e)
            {
                throw new AST_BadFormatException("Format " + var[0] + " is not supported", e, charIndex);
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
		public AST_Type(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
