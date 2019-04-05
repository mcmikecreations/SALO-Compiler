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
        public int layer;
        /// <summary>
        /// true if is prefix, false if is suffix, null otherwise
        /// </summary>
		public bool? isPrefix;
		public bool isLeftToRight;
        public bool isPaired;
        public bool toEnd;

        public bool init;
		public AST_Operator(string oper, int operandCount, int layer, bool? isPrefix, bool isLeftToRight, bool isPaired, bool toEnd)
		{
			this.oper = oper;
			this.operandCount = operandCount;
            this.layer = layer;
			this.isPrefix = isPrefix;
			this.isLeftToRight = isLeftToRight;
            this.isPaired = isPaired;
            this.toEnd = toEnd;
            this.init = true;
		}
	}
	public class AST_Expression : AST_Node
	{
		public static readonly string naming_ast = "_";
        public static readonly AST_Operator[] operators_ast =
        {
            new AST_Operator("return", 1, 0, true, true, false, true),
            new AST_Operator("return", 0, 0, null, true, false, false),
            //new AST_Operator("=", 2, 0, null, true, false, false),
			new AST_Operator("( )", 1, 2, null, true, true, false),
			new AST_Operator("[ ]", 1, 2, null, true, true, false),
			new AST_Operator("*", 1, 3, true, false, false, false),
			new AST_Operator("&", 1, 3, true, false, false, false),
            new AST_Operator(".", 2, 4, null, true, false, false),
            new AST_Operator("*", 2, 5, null, true, false, false),
            new AST_Operator("/", 2, 5, null, true, false, false),
            new AST_Operator("%", 2, 5, null, true, false, false),
            new AST_Operator("-", 2, 6, null, true, false, false),
            new AST_Operator("+", 2, 6, null, true, false, false),
            new AST_Operator(">", 2, 9, null, true, false, false),
            new AST_Operator(">=", 2, 9, null, true, false, false),
            new AST_Operator("<", 2, 9, null, true, false, false),
            new AST_Operator("<=", 2, 9, null, true, false, false),
            new AST_Operator("==", 2, 10, null, true, false, false),
            new AST_Operator("!=", 2, 10, null, true, false, false),
            new AST_Operator("&&", 2, 14, null, true, false, false),
            new AST_Operator("||", 2, 15, null, true, false, false),
            new AST_Operator("=", 2, 16, null, false, false, false),
            new AST_Operator(",", 2, 17, null, true, false, false),
			//new AST_Operator("++", 1, false, true),
			//new AST_Operator("--", 1, false, true),

			//TODO - classes
			//new AST_Operator(".", 2, false, true),
			//Not supported in Ukrainian keyboard
			//new TAST_Operator("->", 2, false, true),

			//new AST_Operator("++", 1, true, false),
			//new AST_Operator("--", 1, true, false),
			//new AST_Operator("+", 1, false, false),
			//new AST_Operator("-", 1, false, false),
			//new AST_Operator("!", 1, false, false),
			//new AST_Operator("~", 1, false, false),
		};
        public List<string> nodes { get; protected set; }
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty", charIndex);
			if (!input.EndsWith(";"))
				throw new AST_BadFormatException("Provided string is not ; terminated", charIndex + input.Length - 1);
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
                    int endInd = i + 1;
                    while(input[endInd] != '\"' || input[endInd - 1] == '\\')
                    {
                        //We search for a non-escaped quotation mark
                        ++endInd;
                        if(endInd >= input.Length)
                        {
                            endInd = -1;
                            break;
                        }
                    }
                    //input.IndexOf('\"', i + 1);
                    string val = "";
                    if (endInd == -1)
                    {
                        endInd = input.Length - 1;
                        val = input.Substring(i, endInd - i + 1) + "\"";
                    }
                    else
                    {
                        val = input.Substring(i, endInd - i + 1);
                    }
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
						if (input[i] == ';')
						{
							items.AddLast(input[i].ToString());
							++i;
						}
						else
                            throw new AST_BadFormatException(
                            "Error parsing input. Unknown character " + input[i], 
                            charIndex + i);
					}
				}
			}
            if (items.Count > 0)
            {
                nodes = items.ToList();
            }
            else nodes = null;
            childNodes = null;
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
			if (nodes != null)
			{
				foreach(string s in nodes)
                {
                    output += indent + "\t\t" + s + "\r\n";
                }
            }
            if (childNodes != null)
            {
                output += indent + "Children??" + "\r\n";
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
                throw new AST_Exception("Expression child nodes are not null, although it doesn't use them", -1);
            }
        }
		public AST_Expression(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
