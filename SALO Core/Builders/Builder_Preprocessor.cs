﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.Builders
{
    public class Builder_Preprocessor
    {
        public string InputText { get; protected set; }
        public string OutputText { get; protected set; }
        private string mainFilePath;
        private Encoding utf8;
        private Builder_Locales locales;

        public static bool IsFullPath(string path)
        {
            return !String.IsNullOrWhiteSpace(path)
                && path.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                && Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public Builder_Preprocessor(string mainFilePath, string input, Builder_Locales locales)
        {
            utf8 = Encoding.GetEncoding("UTF-8");
            this.locales = locales;
            if (!IsFullPath(mainFilePath))
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string absolute_path = Path.Combine(dir, mainFilePath);
                mainFilePath = Path.GetFullPath((new Uri(absolute_path)).LocalPath);
            }
            this.mainFilePath = mainFilePath;

            if (!File.Exists(mainFilePath))
                throw new SALO_Exception("Main code file was not found",
                    new FileNotFoundException(mainFilePath));
            //Get input text
            InputText = input;
            //Include files
            string outText = IncludeCode(InputText, mainFilePath);
            //Remove comments
            outText = RemoveComments(outText);
            //Replace backslashed
            outText = ReplaceBackslash(outText);
            //Perform macros
            bool processed = true;
            while (processed) outText = ProcessFile(outText, out processed);

            OutputText = outText;
        }

        private string RemoveComments(string input)
        {
            //Remove multi-line comments
            int multiLine = input.IndexOf("/*");
            while (multiLine != -1)
            {
                int multiLineEnd = input.IndexOf("*/", multiLine);
                if (multiLineEnd == -1) multiLineEnd = input.Length - 1;
                input = input.Remove(multiLine, multiLineEnd - multiLine + 2);

                multiLine = input.IndexOf("/*");
            }

            //Remove single-line comments
            int singleLine = input.IndexOf("//");
            while(singleLine != -1)
            {
                int singleLineEnd = input.IndexOf("\n", singleLine);
                if (singleLineEnd == -1) singleLineEnd = input.Length - 1;
                input = input.Remove(singleLine, singleLineEnd - singleLine + 1);

                singleLine = input.IndexOf("//");
            }

            return input;
        }

        private string ReplaceBackslash(string input)
        {
            return input.Replace("\\\r\n", "").Replace("\\\n", "");
        }

        private string IncludeCode(string input, string path)
        {
            input = input.Replace("function ", "\"" + path + "\" function ");
            //Parse includes
            int pos = input.IndexOf("#include");
            while (pos != -1)
            {
                int includeStart = pos + "#include".Length;
                while (input[includeStart] == ' ') includeStart++;
                if (input[includeStart] != '\"')
                    throw new SALO_Exception("Include preprocessor directive is empty at " + pos);
                int includeEnd = includeStart + 1;
                while (input[includeEnd] != '\"' || input[includeEnd - 1] == '\\') includeEnd++;
                //We found include file name boundaries
                string fileName = input.Substring(includeStart + 1, includeEnd - includeStart - 1);

                if (!IsFullPath(fileName))
                {
                    string dir = Path.GetDirectoryName(path);
                    string absolute_path = Path.Combine(dir, fileName);
                    fileName = Path.GetFullPath((new Uri(absolute_path)).LocalPath);
                }
                string fileData = File.ReadAllText(fileName, utf8);
                Builder_Translation builder_Translation = new Builder_Translation(fileData, locales);
                fileData = IncludeCode(builder_Translation.Translated, fileName);

                input = input.Remove(pos, includeEnd - pos + 1);
                input = input.Insert(pos, fileData);

                pos = input.IndexOf("#include");
            }
            return input;
        }

        private string ProcessFile(string input, out bool processed)
        {
            processed = false;
            int pos = input.IndexOf("#define");
            int definePosStart = pos, definePosEnd = pos;
            if (pos != -1)
            {
                //We have a preprocessor directive
                if (input.IndexOf("define ", pos + 1) == pos + 1)
                {
                    //We have a define directive
                    int defineStart = pos + 1 + "define ".Length;
                    int defineEnd = defineStart;
                    //Start points at first word start
                    string name = "";
                    string value = "";

                    if (input[defineStart] == '\r' || input[defineStart] == '\n' ||
                        input[defineStart + 1] == '\n')
                    {
                        throw new SALO_Exception("#define directive at " + defineStart + " is empty");
                    }

                    bool nameFilled = false;
                    while (input[defineEnd] != '\n')
                    {
                        if (!nameFilled && input[defineEnd] == ' ' && !string.IsNullOrWhiteSpace(name))
                        {
                            nameFilled = true;
                            while (input[defineEnd] == ' ') defineEnd++;
                            if (input[defineEnd] == '\n')
                            {
                                break;
                            }
                            else if (input[defineEnd] == '\r' && input[defineEnd + 1] == '\n')
                            {
                                defineEnd++;
                                break;
                            }
                        }
                        if (input[defineEnd] == '\\')
                        {
                            while (input[defineEnd] != '\n') defineEnd++;
                            defineEnd++;
                            continue;
                        }
                        if (nameFilled)
                        {
                            value += input[defineEnd];
                        }
                        else
                        {
                            name += input[defineEnd];
                        }
                        ++defineEnd;
                    }
                    name = name.Trim();
                    value = value.Trim();
                    //if (!string.IsNullOrWhiteSpace(value))
                    //{
                    //    defineReplace = (new Tuple<string, string>(name, value));
                    //}
                    //else
                    //{
                    //    defineValue = (name);
                    //}
                    definePosEnd = defineEnd;
                    pos = defineEnd;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        int undefPos = pos;
                        int undefPosStart = pos, undefPosEnd = pos;
                        bool undefFound = false;
                        while (!undefFound && undefPos < input.Length)
                        {
                            undefPos = input.IndexOf("#undef", undefPos);
                            undefPosStart = undefPos;
                            if (undefPos != -1)
                            {
                                undefPos += "#undef".Length;
                                while (input[undefPos] == ' ') undefPos++;
                                if (input.IndexOf(name, undefPos) == undefPos &&
                                    char.IsWhiteSpace(input[undefPos + name.Length]))
                                {
                                    undefPosEnd = undefPos + name.Length;
                                    undefFound = true;
                                    break;
                                }
                            }
                            else
                            {
                                undefFound = false;
                                undefPos = input.Length;
                                break;
                            }
                        }
                        while (pos < undefPos)
                        {
                            int defineReplacePos = input.IndexOf(name, pos);
                            if (defineReplacePos == -1 || defineReplacePos >= undefPosStart) break;
                            else
                            {
                                if (!char.IsLetterOrDigit(input[defineReplacePos-1]) &&
                                    !char.IsLetterOrDigit(input[defineReplacePos + name.Length]))
                                {
                                    //We have to replace
                                    input = input.Remove(defineReplacePos, name.Length);
                                    input = input.Insert(defineReplacePos, value);
                                    undefPosStart += value.Length - name.Length;
                                    undefPosEnd += value.Length - name.Length;
                                    
                                    pos = defineReplacePos + value.Length;
                                    processed = true;
                                }
                                else
                                {
                                    pos = defineReplacePos + 1;
                                }
                            }
                        }
                        input = input.Remove(undefPosStart, undefPosEnd - undefPosStart);
                        input = input.Remove(definePosStart, definePosEnd - definePosStart + 1);
                    }
                    else
                    {
                        //We have a define with no value
                    }
                }
                else
                {
                    pos++;
                    //Other directive
                }
            }
            return input;
        }
    }
}