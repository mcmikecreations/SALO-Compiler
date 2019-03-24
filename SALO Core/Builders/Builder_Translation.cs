using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Builders.Settings;
using SALO_Core.Exceptions;
using SALO_Core.Tools;

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
            //TODO - cut out comments
			foreach(NamesTranslated nt in locales.names_translated)
			{
				if(!string.IsNullOrWhiteSpace(nt.locale) && !string.IsNullOrWhiteSpace(nt.translated))
				{
					//int ind = translated.IndexOf(nt.locale);
					translated = translated.Replace(nt.locale, nt.translated);
				}
			}

            //Parse letter by letter, skip strings
            int quoteIndex = 0;
            string output = "";
            while(quoteIndex < translated.Length)
            {
                bool breakLoop = false;
                if(translated[quoteIndex] == '\"')
                {
                    if (quoteIndex == 0 || translated[quoteIndex - 1] != '\\')
                    {
                        //We have a quote start, find end
                        int quoteStart = quoteIndex;
                        ++quoteIndex;
                        while (!(translated[quoteIndex] == '\"' && translated[quoteIndex - 1] != '\\'))
                        {
                            quoteIndex++;
                            if (quoteIndex >= translated.Length)
                            {
                                //We reached the end
                                output += translated.Substring(quoteStart);
                                breakLoop = true;
                                break;
                            }
                        }
                        if (breakLoop) break;
                        //quoteIndex is pointing at the closing quote
                        output += translated.Substring(quoteStart, quoteIndex - quoteStart + 1);
                    }
                    else
                    {
                        output += '\"';
                    }
                }
                else
                {
                    string s = "";
                    s += translated[quoteIndex];
                    output += Translitor.Translit(s);
                }
                ++quoteIndex;
            }
            translated = output;
		}
	}
}
