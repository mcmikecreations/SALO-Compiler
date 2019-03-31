using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public enum AST_Directive_Type
	{
		none,
		define,
		include,
        @if,
	}
	public class AST_Directive : AST_Node
	{
		protected AST_Directive_Type directive_Type;
		public AST_Directive_Type Directive_Type { get { return directive_Type; } }
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided string is empty", charIndex);
			if (input.StartsWith("#define "))
			{
				directive_Type = AST_Directive_Type.define;
			}
			else if (input.StartsWith("#merge "))
			{
				directive_Type = AST_Directive_Type.include;
			}
            else
			{
				throw new AST_WrongDirectiveException(input + " is not a valid directive", charIndex);
			}
			//TODO - change static string defines with a constant
			switch (directive_Type)
			{
				case AST_Directive_Type.define:
					{
						string subinput = input.Remove(0, "#define ".Length);
						if (string.IsNullOrWhiteSpace(subinput))
							throw new AST_EmptyInputException("Provided string is empty", charIndex + "#define ".Length);
						childNodes = new LinkedList<AST_Node>();
						childNodes.AddLast(new AST_Define(this, subinput, charIndex + "#define ".Length));
						break;
					}
				case AST_Directive_Type.include:
					{
						string subinput = input.Remove(0, "#merge ".Length);
						if (string.IsNullOrWhiteSpace(subinput))
							throw new AST_EmptyInputException("Provided string is empty", charIndex + "#merge ".Length);
						childNodes = new LinkedList<AST_Node>();
						childNodes.AddLast(new AST_Include(this, subinput, charIndex + "#merge ".Length));
						break;
					}
				default:
					{
						throw new NotImplementedException();
					}
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
			output += "Directive\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Directive(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
