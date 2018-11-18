using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Tools
{
	public static class Translitor
	{
		public static string Translit(string word)
		{
			if (string.IsNullOrWhiteSpace(word)) return word;
			int i = 0;
			string output = "";
			while (i < word.Length)
			{
				switch (word[i])
				{
					case 'а':
						{
							output += "a";
							break;
						}
					case 'б':
						{
							output += "b";
							break;
						}
					case 'в':
						{
							output += "v";
							break;
						}
					case 'г':
						{
							output += "h";
							break;
						}
					case 'ґ':
						{
							output += "g";
							break;
						}
					case 'д':
						{
							output += "d";
							break;
						}
					case 'е':
						{
							output += "e";
							break;
						}
					case 'є':
						{
							if (i == 0)
							{
								output += "ye";
							}
							else
							{
								output += "ie";
							}
							break;
						}
					case 'ж':
						{
							output += "zh";
							break;
						}
					case 'з':
						{
							output += "z";
							if (i + 1 < word.Length && word[i + 1] == 'г')
							{
								output += "gh";
								++i;
							}
							break;
						}
					case 'и':
						{
							output += "y";
							break;
						}
					case 'і':
						{
							output += "i";
							break;
						}
					case 'ї':
						{
							if (i == 0)
							{
								output += "yi";
							}
							else
							{
								output += "i";
							}
							break;
						}
					case 'й':
						{
							if (i == 0)
							{
								output += "y";
							}
							else
							{
								output += "i";
							}
							break;
						}
					case 'к':
						{
							output += "k";
							break;
						}
					case 'л':
						{
							output += "l";
							break;
						}
					case 'м':
						{
							output += "m";
							break;
						}
					case 'н':
						{
							output += "n";
							break;
						}
					case 'о':
						{
							output += "o";
							break;
						}
					case 'п':
						{
							output += "p";
							break;
						}
					case 'р':
						{
							output += "r";
							break;
						}
					case 'с':
						{
							output += "s";
							break;
						}
					case 'т':
						{
							output += "t";
							break;
						}
					case 'у':
						{
							output += "u";
							break;
						}
					case 'ф':
						{
							output += "f";
							break;
						}
					case 'х':
						{
							output += "kh";
							break;
						}
					case 'ц':
						{
							output += "ts";
							break;
						}
					case 'ч':
						{
							output += "ch";
							break;
						}
					case 'ш':
						{
							output += "sh";
							break;
						}
					case 'щ':
						{
							output += "shch";
							break;
						}
					case 'ь':
						{
							break;
						}
					case 'ю':
						{
							if (i == 0)
							{
								output += "yu";
							}
							else
							{
								output += "iu";
							}
							break;
						}
					case 'я':
						{
							if (i == 0)
							{
								output += "ya";
							}
							else
							{
								output += "ia";
							}
							break;
						}
					case '\'':
						{
							break;
						}
				}
				++i;
			}
			return output;
		}
	}
}
