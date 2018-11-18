using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Builders.Settings;
using SALO_Core.Exceptions;

namespace SALO_Core.Builders
{
	public class Builder_Translation
	{
		protected string original;
		protected string translated;
		protected Builder_Locales localeStrings;
		public string Translated { get { return translated; } }
		public Builder_Translation(string input, Builder_Locales locales)
		{
			localeStrings = locales;
			original = input;
			translated = input;

			foreach(NamesTranslated nt in locales.names_translated)
			{
				if(!string.IsNullOrWhiteSpace(nt.locale) && !string.IsNullOrWhiteSpace(nt.translated))
				{
					int ind = translated.IndexOf(nt.locale);
					translated = translated.Replace(nt.locale, nt.translated);
				}
			}
		}
	}
}
