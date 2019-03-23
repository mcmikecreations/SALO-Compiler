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
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided variable string is empty", charIndex);
            //TODO - take spaces into account while parsing
			string[] vars = input.Split(AST_Program.separator_ast.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (vars.Length != 2)
			{
				if (!(vars.Length == 1 && vars[0] == "void"))
					throw new AST_BadFormatException("Input parameter was given in a wrong format: " + input,
								new FormatException("Parameter format is <type> <name>₴"), charIndex);
			}
            //TODO - allow custom types, created by user
            try
            {
                dataType = CodeBlocks.ParameterType.GetParameterType(vars[0]);
            }
            catch (Exception e)
            {
                throw new AST_BadFormatException("Format " + vars[0] + " is not supported", e, charIndex);
            }
			if(vars.Length == 2)
			{
				if (!(char.IsLetter(vars[1][0]) || AST_Expression.naming_ast.Contains(vars[1][0])/* ||
                    vars[1][0] == '\"'*/))
					throw new AST_BadFormatException("Variable name not allowed",
								new FormatException("Variable name should start with a letter or " + AST_Expression.naming_ast),
								charIndex + vars[0].Length + 1);
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
		public AST_Variable(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
        //Use only when creating global variables in low-level languages
        public AST_Variable(AST_Node parent, string type, string data) : base(parent, "int32 a", -1)
        {
            this.parent = parent;
            this.dataType = CodeBlocks.ParameterType.GetParameterType(type);
            this.data = data;
        }
	}
}
