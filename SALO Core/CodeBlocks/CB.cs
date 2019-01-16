using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;

namespace SALO_Core.CodeBlocks
{
	public abstract class CB
	{
		public string Result { get; protected set; }
		public abstract void Parse(AST_Program input);
		public abstract void Parse(AST_Comment input);
		public abstract void Parse(AST_Directive input);
		public abstract void Parse(AST_Include input);
		public abstract void Parse(AST_Define input);
		public abstract void Parse(AST_Function input);
		public abstract void Parse(AST_Expression input);
	}
}
