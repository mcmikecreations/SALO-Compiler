using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public class AST_Include : AST_Directive
	{
		public string file { get; protected set; }
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided string is empty", charIndex);

			//TODO - perform include or whatever
			if (input[0] == '\"' && input[input.Length - 1] == '\"')
			{
				input = input.Substring(1, input.Length - 2);
			}
			else if(input[0] == '<' && input[input.Length - 1] == '>')
			{
				input = input.Substring(1, input.Length - 2);
			}
			file = input;
			//TODO - check if path is valid

			this.directive_Type = AST_Directive_Type.include;
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
			output += "Merge " + file + "\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Include(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
