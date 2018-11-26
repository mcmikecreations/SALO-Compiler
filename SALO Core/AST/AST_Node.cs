using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST
{
	public abstract class AST_Node
	{
		protected AST_Node parent;
		protected LinkedList<AST_Node> childNodes;
		public abstract void Parse(string input);
		public abstract void Print(string indent, bool last, ref string output);
		public AST_Node(AST_Node parent, string input)
		{
			this.parent = parent;
			Parse(input);
		}
	}
}
