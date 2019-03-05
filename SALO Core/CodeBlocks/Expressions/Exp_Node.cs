using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.Exceptions;

namespace SALO_Core.CodeBlocks.Expressions
{
    public class Exp_Node
    {
        public Exp_Node left { get; protected set; }
        public Exp_Node right { get; protected set; }
        public List<string> input { get; protected set; }
        public string exp_Data { get; protected set; } = null;
        public Exp_Type exp_Type { get; protected set; } = Exp_Type.None;
        public Exp_Node()
        {

        }
        public Exp_Node(List<string> input, int charInd)
        {
            int charIndex = charInd, leftIndex = 0, rightIndex = 0;
            //TODO - add support for different operations
            if (input == null || input.Count == 0)
                throw new AST_EmptyInputException("Can't parse expression. Input list is empty.", charIndex);
            this.input = input;
            List<string> l;
            List<string> r;
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
            var operPrefix = AST_Expression.operators_ast.Where((a) => a.isPrefix == true).ToList();
            var operSuffix = AST_Expression.operators_ast.Where((a) => a.isPrefix == false).ToList();
            var operInfix = AST_Expression.operators_ast.Where((a) => a.isPrefix == null).ToList();

            //Get left child node
            //Check sequence: brackets, TODO - equals sign, prefix operations, infix operations, suffix operations
            if (bracketTypesStart.IndexOf(input[i]) != -1)
            {
                //Input expression starts with an opening bracket, find corresponding end
                int bracket_count = 1;
                ++charIndex;
                ++i;
                leftIndex = charIndex;
                //TODO - rewrite this code with a stack instead of an increment/decrement system
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
                        //We reached the end of the expression
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
                            if (i + 1 >= input.Count)
                            {
                                //Check for a suffix and infix operation
                                if (operSuffix.FindIndex(a => a.oper == input[i]) != -1)
                                {
                                    //It is a suffix operation
                                    r = null;
                                }
                                else
                                {
                                    throw new AST_BadFormatException(
                                        input[i] + " is not a suffix operaiton", 
                                        charIndex);
                                }
                            }
                            else
                            {
                                if (operInfix.FindIndex(a => a.oper == input[i]) != -1)
                                {
                                    //It is an infix operation
                                    for (i = i + 1; i < input.Count; ++i)
                                    {
                                        temp.Add(input[i]);
                                        charIndex += input[i].Length;
                                    }
                                    r = temp;
                                }
                                else
                                {
                                    throw new AST_BadFormatException(
                                        input[i] + " is not an infix operaiton", 
                                        charIndex);
                                }
                            }
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
                else if (operInfix.FindIndex(a => a.oper == input[i + 1]) != -1 && i + 2 < input.Count)
                {
                    //it is an expression
                    leftIndex = charIndex;
                    l = new List<string> { input[i] };
                    exp_Type = Exp_Type.Operator;
                    exp_Data = input[i + 1];
                    charIndex += input[i].Length + input[i + 1].Length;
                    rightIndex = charIndex;
                    for (i = i + 2; i < input.Count; ++i)
                    {
                        temp.Add(input[i]);
                        charIndex += input[i].Length;
                    }
                    r = temp;
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
                    //TODO - rewrite this code with a stack instead of an increment/decrement system
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
                        if (i >= input.Count)
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
                            bool isInfix = false, isSuffix = false;
                            if (operInfix.FindIndex(a => a.oper == input[i]) != -1)
                            {
                                isInfix = true;
                            }
                            if (operSuffix.FindIndex(a => a.oper == input[i]) != -1)
                            {
                                isSuffix = true;
                            }
                            exp_Data = input[i];
                            exp_Type = Exp_Type.Operator;
                            if (isInfix && i + 1 < input.Count)
                            {
                                //It is an infix operation, get right child node
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
                            else if (isSuffix && i + 1 >= input.Count)
                            {
                                //It is a suffix operation
                                r = null;
                            }
                            else throw new AST_BadFormatException(input[i] + " is not a suffix operaiton", charIndex);
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
                else if (operPrefix.FindIndex(a => a.oper == input[i]) != -1)
                {
                    //It is a prefix operation
                    charIndex += input[i].Length;
                    int operIndex = i;
                    ++i;
                    if (bracketTypesStart.IndexOf(input[i]) != -1)
                    {
                        //right child is a compound expression, we should find the right expression
                        charIndex += input[i].Length;
                        ++i;
                        int bracket_count = 1;
                        rightIndex = charIndex;
                        //TODO - rewrite this code with a stack instead of an increment/decrement system
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
                            if (i >= input.Count)
                            {
                                //Everything is good
                                l = null;
                                r = temp;
                                leftIndex = charInd;
                                exp_Data = input[operIndex];
                                exp_Type = Exp_Type.Operator;
                            }
                            else
                            {
                                throw new AST_BadFormatException("This operation is not supported", charIndex);
                            }
                        }
                        else
                        {
                            throw new AST_BadFormatException(
                                "Didn\'t find a corresponding closing bracket for " + input[operIndex + 1],
                                charIndex);
                        }
                    }
                    else
                    {
                        rightIndex = charIndex;
                        for (; i < input.Count; ++i)
                        {
                            temp.Add(input[i]);
                            charIndex += input[i].Length;
                        }
                        l = null;
                        r = temp;
                        exp_Data = input[operIndex];
                        exp_Type = Exp_Type.Operator;
                    }
                }
                else throw new AST_BadFormatException("Unknown expression sequence", charIndex);
            }

            if (exp_Type == Exp_Type.None || exp_Data == null)
            {
                throw new AST_Exception("Failed to parse expression node", charInd);
            }
            if (l != null)
            {
                left = new Exp_Node(l, leftIndex);
            }
            if (r != null)
            {
                right = new Exp_Node(r, rightIndex);
            }
        }
        public static bool isConstant(string input)
        {
            //TODO - write a correct checker for constant
            int val_i = 0;
            float val_f = 0;
            if (input[0] == '\"' && input[input.Length - 1] == '\"') return true;
            if (float.TryParse(input, out val_f) || int.TryParse(input, out val_i)) return true;
            return false;
        }
        public static bool isOperation(string input)
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
        public static bool isVariable(string input)
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
            if (right != null)
            {
                right.Print(indent, true, ref output);
            }
        }
        public void Accept(CB cb)
        {
            cb.Parse(this);
        }
    }
}
