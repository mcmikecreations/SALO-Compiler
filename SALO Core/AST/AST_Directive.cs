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
	}
	public class AST_Directive : AST_Node
	{
		protected AST_Directive_Type directive_Type;
		public AST_Directive_Type Directive_Type { get { return directive_Type; } }
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
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
				throw new AST_WrongDirectiveException(input + " is not a valid define directive");
			}
			switch (directive_Type)
			{
				case AST_Directive_Type.define:
					{
						string subinput = input.Remove(0, "#define ".Length);
						if (string.IsNullOrWhiteSpace(subinput))
							throw new AST_EmptyInputException("Provided string is empty");
						childNodes = new LinkedList<AST_Node>();
						childNodes.AddLast(new AST_Define(this, subinput));
						break;
					}
				case AST_Directive_Type.include:
					{
						string subinput = input.Remove(0, "#merge ".Length);
						if (string.IsNullOrWhiteSpace(subinput))
							throw new AST_EmptyInputException("Provided string is empty");
						childNodes = new LinkedList<AST_Node>();
						childNodes.AddLast(new AST_Include(this, subinput));
						break;
					}
				default:
					{
						throw new NotImplementedException();
					}
			}
		}
		public override void Print(string indent, bool last)
		{
			Console.Write(indent);
			if (last)
			{
				Console.Write("\\-");
				indent += "  ";
			}
			else
			{
				Console.Write("|-");
				indent += "| ";
			}
			Console.WriteLine("Directive");
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null);
				}
			}
		}
		public AST_Directive(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}
