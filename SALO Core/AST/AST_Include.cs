﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public class AST_Include : AST_Directive
	{
		protected string file;
		public override void Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty");
			file = input;

			//TODO - check if path is valid

			this.directive_Type = AST_Directive_Type.include;
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
			Console.WriteLine("Merge " + file);
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null);
				}
			}
		}
		public AST_Include(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}