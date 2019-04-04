using System;
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
            string outText = input;
            //Remove comments
            outText = RemoveComments(outText);
            //Replace backslashed
            outText = ReplaceBackslash(outText);
            //Include files
            outText = IncludeCode(outText, mainFilePath);
            //Perform macros
            bool processed = true;
            while (processed)
                outText = ProcessFile(outText, out processed);

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
            while (singleLine != -1)
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
            input = input.Replace("structure ", "\"" + path + "\" structure ");
            input = input.Replace("global ", "\"" + path + "\" global ");
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
                fileData = builder_Translation.Translated;
                fileData = RemoveComments(fileData);
                fileData = ReplaceBackslash(fileData);
                fileData = IncludeCode(fileData, fileName);

                input = input.Remove(pos, includeEnd - pos + 1);
                input = input.Insert(pos, fileData);

                pos = input.IndexOf("#include");
            }
            return input;
        }

        private string ProcessFile(string input, out bool processed)
        {
            processed = false;
            int pos = input.IndexOf("#");
            int directivePosStart = pos, directivePosEnd = pos;
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
                    directivePosEnd = defineEnd;
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
                            if (defineReplacePos == -1 || (undefPosStart != -1 && defineReplacePos >= undefPosStart)) break;
                            else
                            {
                                if (!char.IsLetterOrDigit(input[defineReplacePos - 1]) &&
                                    !char.IsLetterOrDigit(input[defineReplacePos + name.Length]))
                                {
                                    //We have to replace
                                    input = input.Remove(defineReplacePos, name.Length);
                                    input = input.Insert(defineReplacePos, value);
                                    if (undefPosStart != -1)
                                        undefPosStart += value.Length - name.Length;
                                    undefPosEnd += value.Length - name.Length;

                                    pos = defineReplacePos + value.Length;
                                }
                                else
                                {
                                    pos = defineReplacePos + 1;
                                }
                            }
                        }
                        if (undefPosStart != -1)
                        {
                            input = input.Remove(undefPosStart, undefPosEnd - undefPosStart);
                        }
                        input = input.Remove(directivePosStart, directivePosEnd - directivePosStart + 1);
                        processed = true;
                    }
                    else
                    {
                        //We have a define with no value
                    }
                }
                else if (input.IndexOf("if ", pos + 1) == pos + 1)
                {
                    int ifStart = pos + 1 + "if ".Length;
                    int ifEnd = input.IndexOf("\n", ifStart);
                    if (ifEnd == -1) ifEnd = input.Length - 1;
                    if (input[ifEnd] == '\r') ifEnd--;

                    bool shouldInclude = true;
                    AST.AST_Expression ast_expression = new AST.AST_Expression(
                        null,
                        input.Substring(ifStart, ifEnd - ifStart + 1) + " ;",
                        ifStart);
                    CodeBlocks.Expressions.Exp_Statement exp_statement = new CodeBlocks.Expressions.Exp_Statement(
                        ast_expression.nodes);
                    if (exp_statement.head.left != null || exp_statement.head.right != null)
                    {
                        shouldInclude = false;
                    }
                    else if (exp_statement.head.exp_Type != CodeBlocks.Expressions.Exp_Type.Constant)
                    {
                        shouldInclude = false;
                    }
                    else if (int.Parse(exp_statement.head.exp_Data) == 0)
                    {
                        shouldInclude = false;
                    }

                    int ifCount = 1;
                    int ifPos = ifEnd;
                    int elsePos = -1, elseCount = 0;
                    while (ifCount != 0)
                    {
                        if (input.IndexOf("#if", ifPos) == ifPos)
                        {
                            ifCount++;
                            ifPos += "#if".Length;
                        }
                        else if (input.IndexOf("#endif", ifPos) == ifPos)
                        {
                            ifCount--;
                            if (ifCount == 0) break;
                            ifPos += "#endif".Length;
                        }
                        else if (input.IndexOf("#else", ifPos) == ifPos)
                        {
                            elseCount++;
                            if (elseCount > 1)
                                throw new AST_BadFormatException(
                                    "Can\'t have more than one preprocessor else inside preprocessor if", ifPos);
                            elsePos = ifPos;
                            ifPos += "#else".Length;
                        }
                        else
                        {
                            ifPos++;
                        }
                        if (ifPos >= input.Length)
                        {
                            if (ifCount == 0) break;
                            throw new AST_BadFormatException("No end found for preprocessor if", ifStart);
                        }
                    }
                    int endifIndex = input.IndexOf("\n", ifPos);
                    if (endifIndex == -1) endifIndex = input.Length - 1;
                    if (shouldInclude)
                    {
                        if(elsePos == -1)
                        {
                            int endifLength = endifIndex - ifPos + 1;
                            input = input.Remove(ifPos, endifLength);
                            input = input.Remove(pos, ifEnd - pos + 1);
                        }
                        else
                        {
                            int endifLength = endifIndex - elsePos + 1;
                            input = input.Remove(elsePos, endifLength);
                            input = input.Remove(pos, ifEnd - pos + 1);
                        }
                    }
                    else
                    {
                        if(elsePos == -1)
                        {
                            input = input.Remove(pos, ifPos - pos + 1);
                        }
                        else
                        {
                            int endifLength = endifIndex - ifPos + 1;
                            input = input.Remove(ifPos, endifLength);
                            input = input.Remove(pos, input.IndexOf("\n", elsePos) - pos + 1);
                        }
                    }
                    pos = ifPos;
                    processed = true;
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
