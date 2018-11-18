using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;

namespace SALO_Core.CodeBlocks
{
	public abstract class CodeBlock
	{
		protected CodeBlock parent;
		protected AST_Node ast_node;
		protected LinkedList<CodeBlock> childBlocks;
		protected abstract void Parse(string input);
		public CodeBlock(string input, CodeBlock parent, AST_Node ast_node)
		{
			this.parent = parent;
			this.ast_node = ast_node;
			Parse(input);
		}
	}
}
