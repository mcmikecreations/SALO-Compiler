using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;

namespace SALO_Core.Builders
{
	public class Builder_AST
	{
		protected AST_Node head;
		public void Print(string indent)
		{
			head.Print(indent, true);
		}
		public Builder_AST(string translatedInput)
		{
			head = new AST_Program(translatedInput);
		}
	}
}
