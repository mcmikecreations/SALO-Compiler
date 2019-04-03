using SALO_Core.AST.Data;
using SALO_Core.Exceptions;
using SALO_Core.Exceptions.ASS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST.Logic
{
    public class AST_While : AST_Logic
    {
        public AST_Expression inside { get; protected set; }
        public override void Parse(string input, int charIndex)
        {
            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
                throw new AST_EmptyInputException("Provided string is empty", charIndex);
            if (!input.StartsWith("while") || !input.EndsWith("ends while"))
                throw new AST_EmptyInputException("Provided string is not a while code piece", charIndex);

            int insideIndexStart = "while".Length;
            while (AST_Program.separator_ast.IndexOf(input[insideIndexStart]) != -1)
            {
                //We are skipping white-spaces
                ++insideIndexStart;
            }
            if (input[insideIndexStart] != '(')
            {
                throw new AST_BadFormatException("Failed to find conditional expression inside while", charIndex + insideIndexStart);
            }

            Stack<string> brackets = new Stack<string>();
            int insideIndexEnd = insideIndexStart + 1;
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
                        throw new AST_BadFormatException("Failed to parse conditional expression inside while",
                            new AST_BadFormatException("Encountered unexpected tokens in bracket stack",
                            charIndex + insideIndexEnd), charIndex + insideIndexEnd);
                    }
                }
                else insideIndexEnd++;
                if (input.Length <= insideIndexEnd)
                {
                    throw new AST_BadFormatException("Reached end of input while parsing while brackets",
                        charIndex + insideIndexEnd);
                }
            }
            //insideIndexEnd points at closing bracket + 1
            string conditionalExpression = input.Substring(insideIndexStart + 1, insideIndexEnd - insideIndexStart - 2);
            inside = new AST_Expression(this, conditionalExpression + " ;", charIndex + insideIndexStart + 1);
            int localInput = insideIndexEnd;
            while (AST_Program.separator_ast.IndexOf(input[localInput]) != -1)
            {
                localInput++;
            }
            if (input.IndexOf("does", localInput) != localInput)
            {
                //We failed to find if body
                throw new AST_BadFormatException("Conditional statement body was not found", charIndex + localInput);
            }
            Stack<string> codeSegments = new Stack<string>();
            codeSegments.Push("does");
            int codeSegmentStart = localInput;
            localInput++;
            while (codeSegments.Count > 0)
            {
                if (input.IndexOf("does", localInput) == localInput)
                {
                    codeSegments.Push("does");
                }
                else if (input.IndexOf("ends", localInput) == localInput)
                {
                    if (codeSegments.Peek() != "does")
                    {
                        throw new AST_BadFormatException("Failed to parse conditional expression inside while",
                            new AST_BadFormatException("Encountered unexpected tokens in bracket stack",
                            charIndex + localInput), charIndex + localInput);
                    }
                    codeSegments.Pop();
                }
                localInput++;
                if (input.Length <= localInput && codeSegments.Count > 0)
                {
                    throw new AST_BadFormatException("Reached end of input while parsing while body",
                        charIndex + localInput);
                }
            }
            //localInput points at letter n from "ends"
            string codeBody = input.Substring(codeSegmentStart + "does".Length, localInput
                - 1 - "does".Length - codeSegmentStart);

            string[] exps = AST_Function.Split(codeBody, charIndex + codeSegmentStart + "does".Length);
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
                    if (nat.StartsWith("native does") && nat.EndsWith("native ends"))
                    {
                        expressions.AddLast(new AST_Native(this, nat, charIndex + exLength));
                    }
                    else if (nat.StartsWith("if") && nat.EndsWith("ends if"))
                    {
                        expressions.AddLast(new AST_If(this, nat, charIndex + exLength));
                    }
                    else if (nat.StartsWith("while") && nat.EndsWith("ends while"))
                    {
                        expressions.AddLast(new AST_While(this, nat, charIndex + exLength));
                    }
                    else if (nat.StartsWith("for") && nat.EndsWith("ends for"))
                    {
                        expressions.AddLast(new AST_For(this, nat, charIndex + exLength));
                    }
                    else
                    {
                        //Try to find local variable
                        bool isVariable = false;
                        string[] pieces = nat.Split(AST_Program.separator_ast.ToCharArray(),
                            StringSplitOptions.RemoveEmptyEntries);
                        if (pieces.Length == 2)
                        {
                            //We may have a variable declaration
                            isVariable = true;
                            try
                            {
                                //Do a simple parameter check beforehand not to waste time just in case
                                var localType = CodeBlocks.ParameterType.GetParameterType(pieces[0]);
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
                            expressions.AddLast(new AST_Expression(this, ex + ";", charIndex + exLength));
                        }
                    }
                }
                exLength += ex.Length + 1;
            }
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
            output += "While:\r\n";

            if (inside != null)
            {
                output += indent + "Condition:\r\n";
                inside.Print(indent, childNodes == null, ref output);
                output += indent + "\r\n";
            }
            if (expressions != null)
            {
                for (LinkedListNode<AST_Expression> ch = expressions.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }

            if (childNodes != null)
            {
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
        }

        public AST_While(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
