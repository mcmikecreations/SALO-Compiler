using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.Exceptions;

namespace SALO_Core.CodeBlocks.Expressions
{
    public enum Exp_Type
    {
        None,
        Variable,
        Constant,
        Operator,
        Function,
        Bracket,
    }
    public class Exp_Node
    {
        public Exp_Node left { get; protected set; }
        public Exp_Node right { get; protected set; }
        public List<string> input { get; protected set; }
        public string exp_Data { get; protected set; } = null;
        public Exp_Type exp_Type { get; protected set; } = Exp_Type.None;
        public Exp_Node(List<string> input, int charInd)
        {
            int charIndex = charInd, leftIndex = 0, rightIndex = 0;
            //TODO - add support for different operations
            if (input == null || input.Count == 0)
                throw new AST_EmptyInputException("Can't parse expression. Input list is empty.", charIndex);
            this.input = input;
            List<string> l = new List<string>();
            List<string> r = new List<string>();
            List<string> temp = new List<string>();
            int i = 0;

            //TODO - put this code in a separate place
            string bracketTypesStart = "", bracketTypesEnd = "";
            foreach (var oper in AST_Expression.operators_ast)
            {
                if (oper.oper.IndexOf(' ') != -1)
                {
                    string[] operParts = oper.oper.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (operParts.Length == 2)
                    {
                        bracketTypesStart += operParts[0];
                        bracketTypesEnd += operParts[1];
                    }
                }
            }

            //Get left child node
            if (bracketTypesStart.IndexOf(input[i]) != -1)
            {
                //Input expression starts with an opening bracket, find corresponding end
                int bracket_count = 1;
                ++charIndex;
                ++i;
                leftIndex = charIndex;
                while (i < input.Count)
                {
                    if (bracketTypesStart.IndexOf(input[i]) != -1) bracket_count++;
                    if (bracketTypesEnd.IndexOf(input[i]) != -1) bracket_count--;
                    charIndex += input[i].Length;
                    if (bracket_count == 0) break;
                    temp.Add(input[i]);
                    ++i;
                }
                ++i;
                if (bracket_count == 0)
                {
                    l = temp;
                    temp = new List<string>();
                    if (i >= input.Count)
                    {
                        //TODO - decide between "(" and "( )"
                        exp_Data = input[0];
                        exp_Type = Exp_Type.Bracket;
                        rightIndex = charInd;
                        r = null;
                    }
                    else
                    {
                        if (isOperation(input[i]))
                        {
                            exp_Data = input[i];
                            exp_Type = Exp_Type.Operator;
                            charIndex += input[i].Length;
                            rightIndex = charIndex;
                            for (i = i + 1; i < input.Count; ++i)
                            {
                                temp.Add(input[i]);
                                charIndex += input[i].Length;
                            }
                            r = temp;
                        }
                        else throw new AST_BadFormatException("Didn\'t find an operation after brackets", charIndex);
                    }
                }
                else
                {
                    throw new AST_BadFormatException(
                        "Didn\'t find a corresponding closing bracket for " + input[0],
                        charIndex);
                }
            }
            else
            {
                //It is not a compound expression. Check for function calls
                bool isVar = isVariable(input[i]), isCon = isConstant(input[i]);
                if ((isVar || isCon) && i == input.Count - 1)
                {
                    //It is a leaf of the tree
                    leftIndex = charInd;
                    rightIndex = charInd;
                    l = null;
                    r = null;
                    exp_Data = input[i];
                    exp_Type = isVar ? Exp_Type.Variable : Exp_Type.Constant;
                }
                else if (isVar && bracketTypesStart.IndexOf(input[i + 1]) != -1)
                {
                    //It is a function call
                    charIndex += input[i].Length + input[i + 1].Length;
                    int funcIndex = i;
                    i += 2;
                    //Function call starts with an opening bracket, find corresponding end
                    int bracket_count = 1;
                    leftIndex = charIndex;
                    while (i < input.Count)
                    {
                        if (bracketTypesStart.IndexOf(input[i]) != -1) bracket_count++;
                        if (bracketTypesEnd.IndexOf(input[i]) != -1) bracket_count--;
                        charIndex += input[i].Length;
                        if (bracket_count == 0) break;
                        temp.Add(input[i]);
                        ++i;
                    }
                    ++i;
                    if(bracket_count == 0)
                    {
                        if(i >= input.Count)
                        {
                            //Input is a function call
                            l = temp;
                            r = null;
                            rightIndex = charInd;
                            exp_Data = input[funcIndex];
                            exp_Type = Exp_Type.Function;
                        }
                        else if (isOperation(input[i]))
                        {
                            //Function call is only a child node of input
                            temp.Insert(0, input[funcIndex + 1]);
                            temp.Insert(0, input[funcIndex]);
                            temp.Add(input[i - 1]);
                            l = temp;
                            exp_Data = input[i];
                            exp_Type = Exp_Type.Operator;
                            //Get right child node
                            temp = new List<string>();
                            charIndex += input[i].Length;
                            rightIndex = charIndex;
                            for (i = i + 1; i < input.Count; ++i)
                            {
                                temp.Add(input[i]);
                                charIndex += input[i].Length;
                            }
                            r = temp;
                        }
                        else throw new AST_BadFormatException("Unknown expression sequence", charIndex);
                    }
                    else
                    {
                        throw new AST_BadFormatException(
                            "Didn\'t find a corresponding closing bracket for " + input[funcIndex + 1],
                            charIndex);
                    }
                }
                else throw new AST_BadFormatException("Unknown expression sequence", charIndex);
            }

            if(exp_Type == Exp_Type.None || exp_Data == null)
            {
                throw new AST_Exception("Failed to parse expression node", charInd);
            }
            if (l != null)
            {
                left = new Exp_Node(l, leftIndex);
            }
            if(r != null)
            {
                right = new Exp_Node(r, rightIndex);
            }
        }
        private bool isConstant(string input)
        {
            //TODO - write a correct checker for constant
            int val_i = 0;
            float val_f = 0;
            if (input[0] == '\"' && input[input.Length - 1] == '\"') return true;
            if (float.TryParse(input, out val_f) || int.TryParse(input, out val_i)) return true;
            return false;
        }
        private bool isOperation(string input)
        {
            foreach (var op in AST_Expression.operators_ast)
            {
                if (op.oper == input)
                {
                    //Excludes compound operators
                    return true;
                }
            }
            return false;
        }
        private bool isVariable(string input)
        {
            foreach (var op in AST_Expression.operators_ast)
            {
                if (op.oper.IndexOf(' ') != -1)
                {
                    //We have a compound operator (E.g. '( )')
                    string[] operParts = op.oper.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < operParts.Length; ++j)
                    {
                        if (operParts[j] == input)
                        {
                            return false;
                        }
                    }
                }
            }
            return (!isConstant(input) && !isOperation(input));
        }
        public void Print(string indent, bool last, ref string output)
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
            output += exp_Type.ToString() + " " + exp_Data + "\r\n";
            if (left != null)
            {
                left.Print(indent, right == null, ref output);
            }
            if(right != null)
            {
                right.Print(indent, true, ref output);
            }
        }
    }
}
