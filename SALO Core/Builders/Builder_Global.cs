using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.Builders
{
	public class Builder_Global
	{
		public Builder_Locales LocaleStrings { get; }
		public Builder_Translation Translator { get; }
		public Builder_AST AST { get; }
		public bool Initialized { get; }
		public Builder_Global(string input, string settingsPath)
		{
			try
			{
				LocaleStrings = Builder_Locales.FromFile(settingsPath);
			}
			catch (SALO_Exception e)
			{
				throw new SALO_Exception("Failed to create LocaleStrings builder", e);
			}
			catch (Exception e)
			{
				throw new SALO_Exception("Unhandled exception caught while creating LocaleStrings builder", e);
			}
			try
			{
				Translator = new Builder_Translation(input, LocaleStrings);
				Console.WriteLine();
				Console.WriteLine(Translator.Translated);
			}
			catch (SALO_Exception e)
			{
				throw new SALO_Exception("Failed to create Translation builder", e);
			}
			catch (Exception e)
			{
				throw new SALO_Exception("Unhandled exception caught while creating Translation builder", e);
			}
			try
			{
				AST = new Builder_AST(Translator.Translated);
			}
			catch (SALO_Exception e)
			{
				throw new SALO_Exception("Failed to create an AST", e);
			}
			catch (Exception e)
			{
				throw new SALO_Exception("Unhandled exception caught while creating an AST", e);
			}
			Initialized = true;
		}
		public Builder_Global(string input) : this(input, "Resources\\LocaleStrings.json")
		{

		}
	}
}
