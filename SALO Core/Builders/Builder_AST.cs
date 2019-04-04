using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.CodeBlocks;

namespace SALO_Core.Builders
{
	public class Builder_AST
	{
        public static List<AST_Structure> structures = new List<AST_Structure>();
		protected AST_Node head;
		public void Print(ref string output)
		{
			head.Print("", true, ref output);
		}
		public void Accept(CB cb)
		{
			head.Accept(cb);
		}
		public Builder_AST(string translatedInput)
		{
			head = new AST_Program(translatedInput, 0);
		}
	}
}
