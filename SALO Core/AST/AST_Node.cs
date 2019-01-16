using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.CodeBlocks;

namespace SALO_Core.AST
{
	public abstract class AST_Node
	{
		protected AST_Node parent;
		public LinkedList<AST_Node> childNodes { get; protected set; }
		public int charIndex { get; protected set; }
		public abstract void Parse(string input, int charIndex);
		public abstract void Print(string indent, bool last, ref string output);
		public void Accept(CB cb)
		{
			cb.Parse((dynamic)this);
		}
		public AST_Node(AST_Node parent, string input, int charIndex)
		{
			this.parent = parent;
			this.charIndex = charIndex;
			Parse(input, charIndex);
		}
	}
}
