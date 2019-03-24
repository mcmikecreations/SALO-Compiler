using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;
using SALO_Core.AST.Data;
using SALO_Core.CodeBlocks;
using SALO_Core.AST.Logic;

namespace SALO_Core.AST
{
    public enum AccessLevel
    {
        None,
        Shared,
        Private,
    }
    public enum FunctionType
    {
        stdcall,
        cdecl,
    }
    public class AST_Function : AST_Node
    {
        public AccessLevel accessLevel { get; protected set; }
        public FunctionType functionType { get; protected set; }
        public string name { get; protected set; }
        public string path { get; protected set; }
        public LinkedList<AST_Variable> parameters { get; protected set; }
        //TODO - parse locals
        public LinkedList<AST_Variable> locals { get; protected set; }
        public AST_Type retValue { get; protected set; }
        public LinkedList<AST_Expression> expressions { get; protected set; }
        public override void Parse(string input, int charIndex)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new AST_EmptyInputException("Provided string is empty", charIndex);
            int i = 0;
            //Read function access level
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function access level",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("shared", i) == i)
            {
                accessLevel = AccessLevel.Shared;
                i += "shared".Length;
            }
            else if (input.IndexOf("private", i) == i)
            {
                accessLevel = AccessLevel.Private;
                i += "private".Length;
            }
            else
            {
                accessLevel = AccessLevel.Private;
            }
            //Read path
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function path",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input[i] != '\"')
                throw new AST_BadFormatException("Failed to parse function path",
                            new ArgumentException("Function path not found"), charIndex + i);
            int pathStart = i;
            ++i;
            while (input[i] != '\"' || input[i - 1] == '\\') ++i;
            path = input.Substring(pathStart + 1, i - pathStart - 1);
            ++i;
            //Read function type
            //TODO - add more function types
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function type",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("function", i) == i)
            {
                functionType = FunctionType.cdecl;
                i += "function".Length;
            }
            else
            {
                throw new AST_BadFormatException("Unknown function format", charIndex + i);
            }
            //Read name
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function name",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (!(char.IsLetter(input[i]) || AST_Expression.naming_ast.Contains(input[i])))
                throw new AST_BadFormatException("Function name not allowed",
                            new FormatException("Function name should start with a letter or " + AST_Expression.naming_ast),
                            charIndex + i);
            string nm = "";
            while (char.IsLetterOrDigit(input[i]) || AST_Expression.naming_ast.Contains(input[i]))
            {
                nm += input[i];
                ++i;
                if (i >= input.Length) break;
            }
            name = nm;
            //Read parameters
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function parameters, return value or code",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("takes", i) == i)
            {
                int end = input.IndexOf("ends", i);
                if (end == -1)
                {
                    throw new AST_BadFormatException("Failed to find a corresponding end to parameters start",
                                new FormatException("No corresponding ends for takes"), charIndex + i);
                }
                i += "takes".Length;
                string inputparameters = input.Substring(i, end - i);
                string[] vars = inputparameters.Split(AST_Program.separator_ast_nospace.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int varStart = i;
                if (vars.Length > 0)
                {
                    parameters = new LinkedList<AST_Variable>();
                }
                foreach (string s in vars)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        parameters.AddLast(new AST_Variable(this, s, charIndex + varStart));
                        varStart += s.Length;
                    }
                    else
                    {
                        varStart += 1;
                    }
                }
                i = end + "ends".Length;
            }
            //Read output
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function return value or code",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("gives", i) == i)
            {
                int end = input.IndexOf("ends", i);
                if (end == -1)
                {
                    throw new AST_BadFormatException("Failed to find a corresponding end to return value start",
                                new FormatException("No corresponding ends for gives"), charIndex + input.Length - 1);
                }
                i += "gives".Length;
                string inputreturn = input.Substring(i, end - i);
                string[] outputVars = inputreturn.Split(AST_Program.separator_ast.ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries);
                if (outputVars.Length != 1)
                    throw new AST_BadFormatException("Too many or too few return values. Try using a structure instead",
                                new FormatException("Wrong return values count. Should be 1"), charIndex + i);
                retValue = new AST_Type(this, outputVars[0], charIndex + i + inputreturn.IndexOf(outputVars[0]));

                i = end + "ends".Length;
            }
            //Read expressions
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse function code",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            //TODO - function declarations
            //TODO - nested ends and does
            if (input.IndexOf("does", i) == i)
            {
                Stack<string> segments = new Stack<string>();
                segments.Push("does");

                int endsIndex = i + "does".Length;
                while (segments.Count > 0)
                {
                    if (input.IndexOf("ends", endsIndex) == endsIndex)
                    {
                        segments.Pop();
                        if (segments.Count > 0) endsIndex += "ends".Length;
                    }
                    else if (input.IndexOf("does", endsIndex) == endsIndex)
                    {
                        segments.Push("does");
                        endsIndex += "does".Length;
                    }
                    else
                    {
                        endsIndex++;
                    }
                    if (endsIndex >= input.Length)
                        throw new AST_BadFormatException("Failed to parse function code",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"),
                            charIndex + input.Length - 1);
                }

                //int end = input.IndexOf("ends", i);
                int end = endsIndex;
                if (end == -1)
                {
                    throw new AST_BadFormatException("Failed to find a corresponding end to code start",
                                new FormatException("No corresponding ends for does"), charIndex + i);
                }
                if (input.IndexOf(name, end + "ends".Length + 1) == end + "ends".Length + 1)
                {
                    throw new AST_BadFormatException("Failed to find a corresponding end to code start",
                                new FormatException("No corresponding ends for does"), charIndex + end);
                }
                i += "does".Length;
                string inputcode = input.Substring(i, end - i);
                string[] exps = Split(inputcode);//inputcode.Split(new char[] { '₴' }/*, StringSplitOptions.RemoveEmptyEntries*/);
                if (exps.Length > 0)
                {
                    expressions = new LinkedList<AST_Expression>();
                }
                int exLength = 0;
                foreach (string ex in exps)
                {
                    bool hasActualExpression = false;
                    for (int p = 0; p < ex.Length; ++p)
                    {
                        if (!AST_Program.separator_ast.Contains(ex[p]))
                        {
                            hasActualExpression = true;
                            break;
                        }
                    }
                    if (hasActualExpression)
                    {
                        string nat = ex.Trim();
                        if (nat.StartsWith("native does") && nat.EndsWith("ends native"))
                        {
                            expressions.AddLast(new AST_Native(this, nat, charIndex + exLength));
                        }
                        else if (nat.StartsWith("if") && nat.EndsWith("ends if"))
                        {
                            expressions.AddLast(new AST_If(this, nat, charIndex + exLength));
                        }
                        else
                        {
                            //Try to find local variable
                            bool isVariable = false;
                            string[] pieces = nat.Split(AST_Program.separator_ast.ToCharArray(), 
                                StringSplitOptions.RemoveEmptyEntries);
                            if(pieces.Length == 2)
                            {
                                //We may have a variable declaration
                                isVariable = true;
                                try
                                {
                                    //Do a simple parameter check beforehand not to waste time just in case
                                    var localType = ParameterType.GetParameterType(pieces[0]);
                                    AST_LocalVariable localVariable = new AST_LocalVariable(this, nat, charIndex + exLength);
                                    expressions.AddLast(localVariable);
                                }
                                catch (Exception)
                                {
                                    //It is not
                                    isVariable = false;
                                }
                            }
                            if (!isVariable)
                            {
                                //Parse as an expression
                                expressions.AddLast(new AST_Expression(this, ex + "₴", charIndex + exLength));
                            }
                        }
                    }
                    exLength += ex.Length + 1;
                }
                i = end + "ends".Length;
            }
            else
            {
                throw new AST_BadFormatException("Failed to find function code", charIndex + i);
            }
        }

        public string[] Split(string input)
        {
            List<string> nodes = new List<string>();
            int i = 0;
            while(i < input.Length)
            {
                while (i < input.Length && AST_Program.separator_ast_nosemicolon.IndexOf(input[i]) != -1) i++;
                if (i >= input.Length) break;
                string expression = "";
                if(input.IndexOf("native", i) == i)
                {
                    int startIndex = i;
                    startIndex += "native".Length;
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[startIndex]) != -1) startIndex++;
                    if(input.IndexOf("does", startIndex) != startIndex)
                    {
                        throw new AST_BadFormatException("Bad formatting of native code start", charIndex + startIndex);
                    }
                    int endIndex = input.IndexOf("ends", startIndex);
                    endIndex += "ends".Length;
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[endIndex]) != -1) endIndex++;
                    if(input.IndexOf("native", endIndex) != endIndex)
                    {
                        throw new AST_BadFormatException("Bad formatting of native code end", charIndex + endIndex);
                    }
                    endIndex += "native".Length;
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[endIndex]) != -1) endIndex++;
                    if(input.IndexOf("₴", endIndex) != endIndex)
                    {
                        throw new AST_BadFormatException("Bad formatting of native code end", charIndex + endIndex);
                    }

                    expression = input.Substring(i, endIndex - i);
                    i = endIndex + 1;
                }
                else if (input.IndexOf("if", i) == i)
                {
                    int startIndex = i;
                    startIndex += "if".Length;
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[startIndex]) != -1) startIndex++;
                    if (input.IndexOf("(", startIndex) != startIndex)
                    {
                        throw new AST_BadFormatException("Bad formatting of if statement condition brackets", charIndex + startIndex);
                    }

                    Stack<string> brackets = new Stack<string>();
                    int insideIndexEnd = startIndex + 1;
                    brackets.Push("(");
                    while (brackets.Count > 0)
                    {
                        if (input[insideIndexEnd] == '(')
                        {
                            brackets.Push("(");
                            insideIndexEnd++;
                        }
                        else if (input[insideIndexEnd] == ')')
                        {
                            if (brackets.Peek() == "(")
                            {
                                brackets.Pop();
                                insideIndexEnd++;
                            }
                            else
                            {
                                throw new AST_BadFormatException("Failed to parse conditional expression inside if",
                                    new AST_BadFormatException("Encountered unexpected tokens in bracket stack",
                                    charIndex + insideIndexEnd), charIndex + insideIndexEnd);
                            }
                        }
                        else insideIndexEnd++;
                        if (input.Length <= insideIndexEnd)
                        {
                            throw new AST_BadFormatException("Reached end of input while parsing if brackets",
                                charIndex + insideIndexEnd);
                        }
                    }
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[insideIndexEnd]) != -1) insideIndexEnd++;

                    if (input.IndexOf("does", insideIndexEnd) != insideIndexEnd)
                    {
                        //We failed to find if body
                        throw new AST_BadFormatException("Conditional statement body was not found", charIndex + insideIndexEnd);
                    }

                    Stack<string> codeSegments = new Stack<string>();
                    codeSegments.Push("does");
                    int codeSegmentStart = insideIndexEnd;
                    insideIndexEnd++;
                    while (codeSegments.Count > 0)
                    {
                        if (input.IndexOf("does", insideIndexEnd) == insideIndexEnd)
                        {
                            codeSegments.Push("does");
                        }
                        else if (input.IndexOf("ends", insideIndexEnd) == insideIndexEnd)
                        {
                            if (codeSegments.Peek() != "does")
                            {
                                throw new AST_BadFormatException("Failed to parse conditional expression inside if",
                                    new AST_BadFormatException("Encountered unexpected tokens in bracket stack",
                                    charIndex + insideIndexEnd), charIndex + insideIndexEnd);
                            }
                            codeSegments.Pop();
                        }
                        insideIndexEnd++;
                        if (input.Length <= insideIndexEnd && codeSegments.Count > 0)
                        {
                            throw new AST_BadFormatException("Reached end of input while parsing if body",
                                charIndex + insideIndexEnd);
                        }
                    }
                    //insideIndexEnd points at letter n from "ends"
                    insideIndexEnd--;
                    insideIndexEnd += "ends".Length;
                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[insideIndexEnd]) != -1) insideIndexEnd++;
                    if(input.IndexOf("if", insideIndexEnd) != insideIndexEnd)
                    {
                        throw new AST_BadFormatException("\"if\" not found at the end of an if block", charIndex + insideIndexEnd);
                    }
                    insideIndexEnd += "if".Length;

                    while (AST_Program.separator_ast_nosemicolon.IndexOf(input[insideIndexEnd]) != -1) insideIndexEnd++;
                    if (input.IndexOf("₴", insideIndexEnd) != insideIndexEnd)
                    {
                        throw new AST_BadFormatException("Semicolon not found at the end of an if block", charIndex + insideIndexEnd);
                    }

                    expression = input.Substring(i, insideIndexEnd - i);
                    i = insideIndexEnd + 1;
                }
                else
                {
                    int semicolonIndex = input.IndexOf("₴", i);
                    if (semicolonIndex == -1)
                        throw new AST_BadFormatException("Semicolon not found", charIndex + i);
                    expression = input.Substring(i, semicolonIndex - i);
                    i = semicolonIndex + 1;
                }
                nodes.Add(expression);
            }
            return nodes.ToArray();
        }

        public override void Print(string indent, bool last, ref string output)
        {
            output += indent;
            if (last)
            {
                output += "\\-";
                indent += "  ";
            }
            else
            {
                output += "|-";
                indent += "| ";
            }
            output += "Function: " + accessLevel.ToString() + " " + functionType.ToString() + " " + name + "\r\n";
            if (parameters != null)
            {
                output += indent + "Input:" + "\r\n";
                for (LinkedListNode<AST_Variable> ch = parameters.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
            if (retValue != null)
            {
                output += indent + "Output:" + "\r\n";
                retValue.Print(indent, true, ref output);
            }
            if (expressions != null)
            {
                output += indent + "Expressions:" + "\r\n";
                for (LinkedListNode<AST_Expression> ch = expressions.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
            if (childNodes != null)
            {
                output += indent + "Children??" + "\r\n";
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
                throw new AST_Exception("Function child nodes are not null, although it doesn't use them", -1);
            }
        }
        public AST_Function(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
