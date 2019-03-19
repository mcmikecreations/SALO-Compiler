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
        private string result = "";
		public Builder_Compile(Language lang, Builder_AST ast, Builder_Libraries libs)
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
						cb = new CB_Assembler_New(false, libs.libraries);
						ast.Accept(cb);
						break;
					}
                default: throw new NotImplementedException(lang + " is not supported");
			}
            result = cb?.GetResult();
		}
		public string Result
		{
			get
			{
                return result;
			}
		}
	}
}
