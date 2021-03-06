﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public class AST_Define : AST_Directive
	{
		public string identifier { get; protected set; }
		public string token { get; protected set; }
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided string is empty", charIndex);
			int identLength = input.IndexOf(' ');
			if (identLength < 1)
			{
				identifier = input.TrimStart(' ');
				token = null;
			}
			else
			{
				identifier = input.Substring(0, identLength);
				token = input.Substring(identLength + 1);

				//TODO - check if token is valid
			}
			this.directive_Type = AST_Directive_Type.define;
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
			output += "Define " + identifier + " " + (token ?? "") + "\r\n";
			if(childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Define(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
