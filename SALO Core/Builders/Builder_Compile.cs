using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALO_Core.CodeBlocks;

namespace SALO_Core.Builders
{
	public enum Language
	{
		None,
		Assembler,
	}
	public class Builder_Compile
	{
		private CB cb;
		public Builder_Compile(Language lang, Builder_AST ast)
		{
			switch (lang)
			{
				case Language.None:
					{
						cb = null;
						break;
					}
				case Language.Assembler:
					{
						cb = new CB_Assembler(false);
						ast.Accept(cb);
						break;
					}
			}
		}
		public string Result
		{
			get
			{
				return cb?.Result;
			}
		}
	}
}
