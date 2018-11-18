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
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			childNodes = new LinkedList<AST_Node>();
			int i = 0;
			while(i < input.Length)
			{
				//Get to the first non-space character
				while (i < input.Length && " \r\n\t".Contains(input[i])) ++i;
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
				else
				{
					string val = "";
					while (!(" \r\n\t".Contains(input[i])))
					{
						val += input[i];
						++i;
						if (i >= input.Length) break;
					}
					childNodes.AddLast(new AST_Unknown(this, val));
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
			Console.WriteLine("Program");
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null);
				}
			}
		}
		public AST_Program(string input) : base(null, input)
		{

		}
	}
}
