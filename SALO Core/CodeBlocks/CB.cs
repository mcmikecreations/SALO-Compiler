﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.CodeBlocks.Expressions;

namespace SALO_Core.CodeBlocks
{
	public abstract class CB
	{
		protected string Result { get; set; }
        public abstract string GetResult();
		public abstract void Parse(AST_Program input);
		public abstract void Parse(AST_Comment input);
		public abstract void Parse(AST_Directive input);
		public abstract void Parse(AST_Include input);
		public abstract void Parse(AST_Define input);
        public abstract void Parse(AST.Data.AST_GlobalVariable input);
        public abstract void Parse(AST_Structure input);

        public abstract void Parse(AST_Function input);
		public abstract void Parse(AST_Expression input);
        public abstract void Parse(Exp_Statement exp);
        public abstract void Parse(Exp_Node node);
	}
}
