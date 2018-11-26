using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public class AST_Program : AST_Node
	{
		public static readonly string separator_ast = "₴ \r\n\t";
		public static readonly string separator_ast_nospace = "₴\r\n\t";
		public static readonly string separator_line = "\r\n";
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			childNodes = new LinkedList<AST_Node>();
			int i = 0;
			while(i < input.Length)
			{
				//Get to the first non-space character
				while (i < input.Length && separator_ast.Contains(input[i])) ++i;
				if (i >= input.Length) break;
				if(input.IndexOf("#", i) == i)
				{
					//We have a directive
					string val = "";
					//Get directive value
					bool nextChar = true;
					while (nextChar)
					{
						if (input[i] == '\r' || input[i] == '\n') break;
						val += input[i];
						if (i + 2 < input.Length && (input[i + 2] == '\r' || input[i + 2] == '\n'))
						{
							if (input[i + 1] != '\\')
							{
								val += input[i + 1];
							}
							i += 2;
							//Line ending is \r\n
							if (i + 1 < input.Length && input[i + 1] == '\n') ++i;
							nextChar = false;
						}
						++i;
					}
					childNodes.AddLast(new AST_Directive(this, val));
				}
				else if(input.IndexOf("/", i) == i)
				{
					if(input.IndexOf("/", i + 1) == i + 1)
					{
						//Single-line comment
						string val = "";
						while (!(separator_line.Contains(input[i])))
						{
							val += input[i];
							++i;
							if (i >= input.Length) break;
						}
						childNodes.AddLast(new AST_Comment(this, val));
					}
					else if(input.IndexOf("*", i) == i + 1)
					{
						//Multiline comment
						int end = input.IndexOf("*/", i);
						if (end == -1) end = input.Length - 1;
						else ++end;
						string val = input.Substring(i, end - i + 1);
						childNodes.AddLast(new AST_Comment(this, val));
						i += val.Length;
					}
					else
					{
						//TODO - something else with '/' char
						++i;
					}
				}
				else
				{
					bool isForV = false;
					//It's a variable or a function declaration
					//TODO - add more stuff with access levels
					int j = -1;
					if (input.IndexOf("shared", i) == i)
					{
						j = i + "shared".Length + 1;
						isForV = true;
					}
					else if (input.IndexOf("private", i) == i)
					{
						j = i + "private".Length + 1;
						isForV = true;
					}
					if (isForV)
					{
						//TODO - do checks for other function types
						if (input.IndexOf("function", j) == j)
						{
							int k = j + "function".Length + 1;

							if (k >= input.Length)
								throw new AST_BadFormatException("Failed to parse function name",
											new ArgumentOutOfRangeException("input", "Reached the end of input"));
							if (!(char.IsLetter(input[k]) || AST_Expression.naming_ast.Contains(input[k])))
								throw new AST_BadFormatException("Function name not allowed",
											new FormatException("Function name should start with a letter or " + AST_Expression.naming_ast));
							string nm = "";
							while (char.IsLetterOrDigit(input[k]) || AST_Expression.naming_ast.Contains(input[k]))
							{
								nm += input[k];
								++k;
								if (k >= input.Length) break;
							}
							string funcend = "ends " + nm;
							int end = input.IndexOf(funcend, k);
							if (end == -1)
							{
								throw new AST_BadFormatException("Failed to find a corresponding end to function start",
											new FormatException("No corresponding ends for function " + nm));
							}
							end += funcend.Length;
							string function = input.Substring(i, end - i);
							childNodes.AddLast(new AST_Function(this, function));
							i = end;
							continue;
						}
						else throw new AST_BadFormatException("Failed to parse input");
						//TODO - do checks for variables
					}

					int expInd = input.IndexOf('₴', i);
					if (expInd != -1)
					{
						string val = input.Substring(i, expInd - i + 1);
						childNodes.AddLast(new AST_Expression(this, val));
						i += val.Length;
					}
					else
					{
						string val = "";
						while (!(separator_line.Contains(input[i])))
						{
							val += input[i];
							++i;
							if (i >= input.Length) break;
						}
						childNodes.AddLast(new AST_Unknown(this, val));
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
			output += "Program\r\n";
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Program(string input) : base(null, input)
		{

		}
	}
}
