using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;

namespace SALO_Core.CodeBlocks
{
	public class CB_Program : CodeBlock
	{
		protected override void Parse(string input)
		{
			
		}
		public CB_Program(string input, AST_Node ast_node) : base(input, null, ast_node)
		{
			
		}
	}
}
