﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.Exceptions;

namespace SALO_Core.CodeBlocks.Expressions
{
	public class Exp
	{
		protected List<string> items;
		public Exp_Node head { get; protected set; }
		public Exp(List<string> list)
		{
            items = list;
            if (list == null)
                throw new AST_EmptyInputException(
                    "Expression is empty", 
                    new NullReferenceException("Expression piece list is null"), 
                    0);
            head = new Exp_Node(list, 0);
		}
        public void Print(string indent, bool last, ref string output)
        {
            head.Print(indent, last, ref output);
        }
	}
}
