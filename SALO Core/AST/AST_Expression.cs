using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public enum AST_Expression_Type
	{
		None,
		Call,
		Assign,
	}
	public struct AST_Operator
	{
		public string oper;
		public int operandCount;
		public bool isPrefix;
		public bool isLeftToRight;
		public AST_Operator(string oper, int operandCount, bool isPrefix, bool isLeftToRight)
		{
			this.oper = oper;
			this.operandCount = operandCount;
			this.isPrefix = isPrefix;
			this.isLeftToRight = isLeftToRight;
		}
	}
	public class AST_Expression : AST_Node
	{
		public static readonly string naming_ast = "_";
		public static readonly AST_Operator[] operators_ast =
		{
			new AST_Operator("++", 1, false, true),
			new AST_Operator("--", 1, false, true),
			new AST_Operator("( )", 2, false, true),
			//TODO - classes
			//new AST_Operator(".", 2, false, true),
			//Not supported in Ukrainian keyboard
			//new TAST_Operator("->", 2, false, true),
			new AST_Operator("++", 1, true, false),
			new AST_Operator("--", 1, true, false),
			new AST_Operator("+", 1, false, false),
			new AST_Operator("-", 1, false, false),
			new AST_Operator("!", 1, false, false),
			new AST_Operator("~", 1, false, false),
		};
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			if (!input.EndsWith("₴")) throw new AST_BadFormatException("Provided string is not ₴ terminated");
			LinkedList<string> items = new LinkedList<string>();
			int i = 0;
			while (i < input.Length)
			{
				while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
				if (i >= input.Length) break;
				//We are parsing a number
				if (char.IsDigit(input[i]))
				{
					string val = "";
					//TODO - Create a smarter input for hex integers, floats etc.
					while (char.IsDigit(input[i]))
					{
						val += input[i];
						++i;
						if (i >= input.Length) break;
					}
					items.AddLast(val);
				}
				else if (input[i] == '\"')
				{
					if (i + 1 >= input.Length)
					{
						items.AddLast("\"");
						break;
					}
					//We have a string literal
					int endInd = input.IndexOf('\"', i + 1);
					if (endInd == -1) endInd = input.Length - 1;
					string val = input.Substring(i, endInd - i + 1);
					items.AddLast(val);
					i += val.Length;
				}
				//We are parsing a string
				else if (char.IsLetter(input[i]) || naming_ast.Contains(input[i]))
				{
					string val = "";
					while (char.IsLetterOrDigit(input[i]) || naming_ast.Contains(input[i]))
					{
						val += input[i];
						++i;
						if (i >= input.Length) break;
					}
					items.AddLast(val);
				}
				else
				{
					bool isOper = false;
					foreach(AST_Operator oper in operators_ast)
					{
						if(oper.oper.IndexOf(' ') != -1)
						{
							//We have a compound operator (E.g. '( )')
							string[] operParts = oper.oper.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
							for(int j = 0; j < operParts.Length; ++j)
							{
								if(input.IndexOf(operParts[j], i) == i)
								{
									items.AddLast(operParts[j]);
									i += operParts[j].Length;
									isOper = true;
									break;
								}
							}
						}
						//We have an operator
						else if(input.IndexOf(oper.oper, i) == i)
						{
							items.AddLast(oper.oper);
							i += oper.oper.Length;
							isOper = true;
							break;
						}
					}
					if (!isOper)
					{
						if (input[i] == '₴')
						{
							items.AddLast(input[i].ToString());
							++i;
						}
						else throw new AST_BadFormatException("Error parsing input: unknown character " + input[i]);
					}
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
			output += "Expression:\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Expression(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}
