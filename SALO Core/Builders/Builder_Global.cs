using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;
using SALO_Core.Tools;

namespace SALO_Core.Builders
{
	public class Builder_Global
	{
		public Builder_Locales LocaleStrings { get; }
		public Builder_Translation Translator { get; }
        public Builder_Preprocessor Preprocessor { get; }
		public Builder_AST AST { get; }
        public Builder_Libraries Libraries { get; }
		public Builder_Compile Compiler { get; }
		public bool Initialized { get; }
		public Builder_Global(string input, string inputPath, string settingsPath, Language lang,
			bool build_eng = true, bool build_ast = true, bool build_lang = true)
		{
            try
            {
                if(input == null)
                {
                    if (inputPath == null)
                        throw new NullReferenceException("Both provided input text and file are empty");
                }
                Encoding utf8 = Encoding.GetEncoding("UTF-8");
                //TODO - check if file exists, if arguments are correct, etc.
                input = File.ReadAllText(inputPath, utf8);
            }
            catch(Exception e)
            {
                throw new SALO_Exception("Unhandled exception caught while loading main code file", e);
            }

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
			if (build_eng)
			{
				try
				{
					Translator = new Builder_Translation(input, LocaleStrings);
#if DEBUG
                    Console.WriteLine("Translated:");
					Console.WriteLine(Translator.Translated);
#endif
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
                    Preprocessor = new Builder_Preprocessor(inputPath, Translator.Translated, LocaleStrings);
#if DEBUG
                    Console.WriteLine("Preprocessed:");
                    Console.WriteLine(Preprocessor.OutputText);
#endif
                }
                catch (SALO_Exception e)
                {
                    throw new SALO_Exception("Failed to create Preprocessor builder", e);
                }
                catch (Exception e)
                {
                    throw new SALO_Exception("Unhandled exception caught while creating Preprocessor builder", e);
                }

                if (build_ast)
				{
					try
					{
						AST = new Builder_AST(Preprocessor.OutputText);
#if DEBUG
                        string ast = "";
                        AST.Print(ref ast);
                        Console.WriteLine("AST:");
                        Console.WriteLine(ast);
#endif
                    }
					catch (SALO_Exception e)
					{
						//ExceptionHandler.ShowException(e, Translator.Translated);
						throw new SALO_Exception("Failed to create an AST", e);
					}
					catch (Exception e)
					{
						throw new SALO_Exception("Unhandled exception caught while creating an AST", e);
					}
					if (build_lang)
					{
						try
						{
                            Libraries = new Builder_Libraries(settingsPath);
							Compiler = new Builder_Compile(lang, AST, Libraries);
#if DEBUG
                            Console.WriteLine("Assembler:");
                            Console.WriteLine(Compiler.Result);
#endif
                        }
                        catch (SALO_Exception e)
						{
							throw new SALO_Exception("Failed to create an AST", e);
						}
						catch (Exception e)
						{
							throw new SALO_Exception("Unhandled exception caught while compiling into " + 
                                lang.ToString(), e);
						}
					}
				}
			}
			Initialized = true;
		}
		public Builder_Global(string input, string inputPath, Language lang) : 
            this(input, inputPath, "Resources\\LocaleStrings.json", lang)
		{

		}
	}
}
