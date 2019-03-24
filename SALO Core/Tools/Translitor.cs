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
                bool IsUpper = char.ToLower(word[i]) != word[i];
				switch (char.ToLower(word[i]))
				{
					case 'а':
						{
                            if (IsUpper)
                            {
                                output += "A";
                            }
                            else
                            {
                                output += "a";
                            }
							break;
						}
					case 'б':
                        {
                            if (IsUpper)
                            {
                                output += "B";
                            }
                            else
                            {
                                output += "b";
                            }
                            break;
						}
					case 'в':
                        {
                            if (IsUpper)
                            {
                                output += "V";
                            }
                            else
                            {
                                output += "v";
                            }
                            break;
						}
					case 'г':
                        {
                            if (IsUpper)
                            {
                                output += "H";
                            }
                            else
                            {
                                output += "h";
                            }
                            break;
						}
					case 'ґ':
                        {
                            if (IsUpper)
                            {
                                output += "G";
                            }
                            else
                            {
                                output += "g";
                            }
                            break;
						}
					case 'д':
                        {
                            if (IsUpper)
                            {
                                output += "D";
                            }
                            else
                            {
                                output += "d";
                            }
                            break;
						}
					case 'е':
                        {
                            if (IsUpper)
                            {
                                output += "E";
                            }
                            else
                            {
                                output += "e";
                            }
                            break;
						}
					case 'є':
						{
							if (i == 0)
                            {
                                if (IsUpper)
                                {
                                    output += "Ye";
                                }
                                else
                                {
                                    output += "ye";
                                }
                            }
							else
                            {
                                if (IsUpper)
                                {
                                    output += "Ie";
                                }
                                else
                                {
                                    output += "ie";
                                }
                            }
							break;
						}
					case 'ж':
                        {
                            if (IsUpper)
                            {
                                output += "Zh";
                            }
                            else
                            {
                                output += "zh";
                            }
                            break;
						}
					case 'з':
                        {
                            if (IsUpper)
                            {
                                output += "Z";
                            }
                            else
                            {
                                output += "z";
                            }
                            if (i + 1 < word.Length && word[i + 1] == 'г')
							{
								output += "gh";
								++i;
							}
							break;
						}
					case 'и':
                        {
                            if (IsUpper)
                            {
                                output += "Y";
                            }
                            else
                            {
                                output += "y";
                            }
                            break;
						}
					case 'і':
                        {
                            if (IsUpper)
                            {
                                output += "I";
                            }
                            else
                            {
                                output += "i";
                            }
                            break;
						}
					case 'ї':
						{
							if (i == 0)
                            {
                                if (IsUpper)
                                {
                                    output += "Yi";
                                }
                                else
                                {
                                    output += "yi";
                                }
                            }
							else
                            {
                                if (IsUpper)
                                {
                                    output += "I";
                                }
                                else
                                {
                                    output += "i";
                                }
                            }
							break;
						}
					case 'й':
						{
							if (i == 0)
                            {
                                if (IsUpper)
                                {
                                    output += "Y";
                                }
                                else
                                {
                                    output += "y";
                                }
                            }
							else
                            {
                                if (IsUpper)
                                {
                                    output += "I";
                                }
                                else
                                {
                                    output += "i";
                                }
                            }
							break;
						}
					case 'к':
                        {
                            if (IsUpper)
                            {
                                output += "K";
                            }
                            else
                            {
                                output += "k";
                            }
                            break;
						}
					case 'л':
                        {
                            if (IsUpper)
                            {
                                output += "L";
                            }
                            else
                            {
                                output += "l";
                            }
                            break;
						}
					case 'м':
                        {
                            if (IsUpper)
                            {
                                output += "M";
                            }
                            else
                            {
                                output += "m";
                            }
                            break;
						}
					case 'н':
                        {
                            if (IsUpper)
                            {
                                output += "N";
                            }
                            else
                            {
                                output += "n";
                            }
                            break;
						}
					case 'о':
                        {
                            if (IsUpper)
                            {
                                output += "O";
                            }
                            else
                            {
                                output += "o";
                            }
                            break;
						}
					case 'п':
                        {
                            if (IsUpper)
                            {
                                output += "P";
                            }
                            else
                            {
                                output += "p";
                            }
                            break;
						}
					case 'р':
                        {
                            if (IsUpper)
                            {
                                output += "R";
                            }
                            else
                            {
                                output += "r";
                            }
                            break;
						}
					case 'с':
                        {
                            if (IsUpper)
                            {
                                output += "S";
                            }
                            else
                            {
                                output += "s";
                            }
                            break;
						}
					case 'т':
                        {
                            if (IsUpper)
                            {
                                output += "T";
                            }
                            else
                            {
                                output += "t";
                            }
                            break;
						}
					case 'у':
                        {
                            if (IsUpper)
                            {
                                output += "U";
                            }
                            else
                            {
                                output += "u";
                            }
                            break;
						}
					case 'ф':
                        {
                            if (IsUpper)
                            {
                                output += "F";
                            }
                            else
                            {
                                output += "f";
                            }
                            break;
						}
					case 'х':
                        {
                            if (IsUpper)
                            {
                                output += "Kh";
                            }
                            else
                            {
                                output += "kh";
                            }
                            break;
						}
					case 'ц':
                        {
                            if (IsUpper)
                            {
                                output += "Ts";
                            }
                            else
                            {
                                output += "ts";
                            }
                            break;
						}
					case 'ч':
                        {
                            if (IsUpper)
                            {
                                output += "Ch";
                            }
                            else
                            {
                                output += "ch";
                            }
                            break;
						}
					case 'ш':
                        {
                            if (IsUpper)
                            {
                                output += "Sh";
                            }
                            else
                            {
                                output += "sh";
                            }
                            break;
						}
					case 'щ':
                        {
                            if (IsUpper)
                            {
                                output += "Shch";
                            }
                            else
                            {
                                output += "shch";
                            }
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
                                if (IsUpper)
                                {
                                    output += "Yu";
                                }
                                else
                                {
                                    output += "yu";
                                }
                            }
							else
                            {
                                if (IsUpper)
                                {
                                    output += "Iu";
                                }
                                else
                                {
                                    output += "iu";
                                }
                            }
							break;
						}
					case 'я':
						{
							if (i == 0)
                            {
                                if (IsUpper)
                                {
                                    output += "Ya";
                                }
                                else
                                {
                                    output += "ya";
                                }
                            }
							else
                            {
                                if (IsUpper)
                                {
                                    output += "Ia";
                                }
                                else
                                {
                                    output += "ia";
                                }
                            }
							break;
						}
					case '`':
						{
							break;
						}
					default:
						{
							output += word[i];
							break;
						}
				}
				++i;
			}
			return output;
		}
	}
}
