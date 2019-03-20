using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALO_Core.AST;
using SALO_Core.AST.Data;
using SALO_Core.CodeBlocks.Expressions;
using SALO_Core.Exceptions;
using SALO_Core.Exceptions.ASS;

namespace SALO_Core.CodeBlocks
{
    public class CB_Assembler : CB
    {
        public CB_Assembler(bool convertTo32)
        {
            this.convertTo32 = convertTo32;
        }

        public override string GetResult()
        {
            return Result;
        }

        public override void Parse(AST_Program input)
        {
            string header, footer;
            header = "format PE GUI 4.0\nentry main\n";
            footer = "";

            Result = header;

            foreach (AST_Node node in input.childNodes)
            {
                node.Accept(this);
            }

            Result += footer;
        }

        public override void Parse(AST_Comment input)
        {
            foreach (string s in input.text)
            {
                Result += "; " + s + "\n";
            }
        }

        public override void Parse(AST_Directive input)
        {
            if (input.childNodes == null || input.childNodes.Count < 1)
                throw new AST_BadFormatException("Input directive node has too few children", input.charIndex);
            if (input.childNodes.Count > 1)
                throw new AST_BadFormatException("Input directive node has too many children", input.charIndex);
            input.childNodes.First.Value.Accept(this);
        }

        public override void Parse(AST_Include input)
        {
            Result += "include \'" + input.file + "\'\n";
        }

        public override void Parse(AST_Define input)
        {
            if (input.token == null)
                throw new NotImplementedException();
            Result += input.identifier + " equ " + input.token + "\n";
        }
        private struct FunctionData
        {
            public string name;
            //Variable, isUsed, address
            public List<Tuple<AST_Variable, bool, string>> parameters;
            public List<Tuple<AST_Variable, bool, string>> locals;
            public AccessLevel accessLevel;
            public AST_Type returnValue;
            public bool axUsed, bxUsed, cxUsed, dxUsed;
            public string ending;
            public Stack<string> left, right, ret;
        }
        private FunctionData functionData;
        private bool convertTo32;
        public override void Parse(AST_Function input)
        {
            functionData.accessLevel = input.accessLevel;
            functionData.name = input.name;
            functionData.ending = "";
            //TODO - implement locals
            functionData.locals = new List<Tuple<AST_Variable, bool, string>>();
            functionData.left = new Stack<string>();
            functionData.right = new Stack<string>();
            functionData.ret = new Stack<string>();
            if (input.parameters == null) functionData.parameters = null;
            else
            {
                functionData.parameters = new List<Tuple<AST_Variable, bool, string>>();
                int addr = 4;
                for (int i = input.parameters.Count - 1; i >= 0; --i)
                {
                    var p = input.parameters.ElementAt(i);
                    string address = "";
                    string dataType = p.DataType.GetName();
                    switch (dataType)
                    {
                        case "bool":
                            addr += 1;
                            address = "byte ptr ebp+" + addr.ToString() + "";
                            break;
                        case "float32":
                            throw new NotImplementedException("Floats are not supported");
                        case "int32":
                            addr += 4;
                            address = "ptr ebp+" + addr.ToString() + "";
                            break;
                        case "int8":
                            addr += 1;
                            address = "byte ptr ebp+" + addr.ToString() + "";
                            break;
                        case "int16":
                            addr += 2;
                            address = "word ptr ebp+" + addr.ToString() + "";
                            break;
                    }
                    functionData.parameters.Add(new Tuple<AST_Variable, bool, string>(p, false, address));
                }
            }
            functionData.returnValue = input.retValue;
            //if (functionData.returnValue.DataType != DataType.Void) functionData.axUsed = true;
            functionData.axUsed = false;
            functionData.bxUsed = false;
            functionData.cxUsed = false;
            functionData.dxUsed = false;
            Result += input.name + ":" + AST_Program.separator_line;
            //Working with input parameters, if void then skip
            if (input.parameters.Count > 0 &&
                !(input.parameters.Count == 1 && input.parameters.First.Value.DataType.GetName() == "void"))
            {
                Result += "\tpush \tebp" + AST_Program.separator_line;
                Result += "\tmov  \tebp,\tesp" + AST_Program.separator_line;
                functionData.ending += "\tmov  \tesp,\tebp" + AST_Program.separator_line;
                functionData.ending += "\tpop  \tebp" + AST_Program.separator_line;
            }

            functionData.ending += "\tret" + AST_Program.separator_line;

            foreach (var exp in input.expressions)
            {
                exp.Accept(this);
            }

            //if (input.parameters.Count == 1 && input.parameters.First.Value.DataType == DataType.Void)
            //{
            //    Result += functionData.ending;
            //}
        }

        public override void Parse(AST_Expression input)
        {
            Exp_Statement exp = new Exp_Statement(input.nodes);
#if DEBUG
            string output = "";
            exp.Print("", false, ref output);
            Console.WriteLine(output);
#endif
            //exp.Accept(this);
        }

        public override void Parse(Exp_Statement exp)
        {
            exp.head.Accept(this);
        }

        public override void Parse(Exp_Node node)
        {
            if (node.exp_Type == Exp_Type.Operator)
            {
                if (node.left == null && node.right != null)
                {
                    //It is a prefix
                    if (node.exp_Data == "return")
                    {
                        if (functionData.returnValue.DataType.GetName() == "void")
                            throw new AST_BadFormatException(
                                "A void function " + functionData.name + " can\'t have return statements", -1);
                        if (node.right.exp_Type == Exp_Type.Constant)
                        {
                            //A simple return value
                            Result += "\tmov  \teax,\t" + node.right.exp_Data + AST_Program.separator_line;
                        }
                        else if (node.right.exp_Type == Exp_Type.Operator)
                        {
                            node.right.Accept(this);
                            string right = functionData.ret.Pop();
                            if (SetDimentions(right, 32) != "eax")
                            {
                                Result += "\tmov  \teax,\t" + right + AST_Program.separator_line;
                                ClearReg(SetDimentions(right, 32), true);
                            }
                        }
                        else throw new NotImplementedException("This node is not supported in a return statement");
                        Result += functionData.ending;
                    }
                    else throw new NotImplementedException("Not supported suffix operator " + node.exp_Data);
                }
                else if (node.left != null && node.right == null)
                {
                    throw new NotImplementedException("Not supported prefix operator " + node.exp_Data);
                }
                else if (node.left != null && node.right != null)
                {
                    bool popAx = false;
                    string left = "";
                    string right = "";
                    //TODO - use stack for variable declaration
                    Stack<Exp_Node> stack = new Stack<Exp_Node>();

                    //Process left node
                    if (node.left.exp_Type == Exp_Type.Function)
                    {
                        if (functionData.axUsed)
                        {
                            Result += "\tpush \teax" + AST_Program.separator_line;
                            popAx = true;
                            functionData.axUsed = false;
                        }

                        node.left.Accept(this);
                        if (functionData.ret.Pop() != "eax")
                            throw new AST_BadFormatException(
                                "Return stack corrupted from function " + node.left.exp_Data, -1);

                        if (functionData.axUsed)
                        {
                            //We have a return value
                            //TODO - we shouldn't always have a return value
                            //TODO - we don't have to always move return value to something else
                            right = ToReg("eax");
                            if (popAx)
                            {
                                Result += "\tpop  \teax" + AST_Program.separator_line;
                            }
                        }
                    }
                    else if (node.left.exp_Type == Exp_Type.Operator)
                    {
                        node.left.Accept(this);
                        left = functionData.ret.Pop();
                    }
                    else if (node.left.exp_Type == Exp_Type.Constant)
                    {
                        left = ToReg(node.left.exp_Data);
                        //left = node.left.exp_Data;
                    }
                    else if (node.left.exp_Type == Exp_Type.Variable)
                    {
                        int indParam = functionData.parameters.FindIndex(a => a.Item1.Data == node.left.exp_Data);
                        int indLocal = functionData.locals.FindIndex(a => a.Item1.Data == node.left.exp_Data);
                        if (indLocal != -1)
                        {
                            //This variable exists as a local
                            left = functionData.locals[indParam].Item3;
                        }
                        else if (indParam != -1)
                        {
                            //This variable exists as a parameter
                            left = functionData.parameters[indParam].Item3;
                        }
                        else throw new AST_BadFormatException("Unknown variable " + node.left.exp_Data, -1);
                    }
                    else
                    {
                        throw new AST_BadFormatException("Not supported node " + node.left.exp_Type.ToString(), -1);
                    }

                    //Process right node
                    if (node.right.exp_Type == Exp_Type.Function)
                    {
                        if (functionData.axUsed)
                        {
                            Result += "\tpush \teax" + AST_Program.separator_line;
                            popAx = true;
                            functionData.axUsed = false;
                        }

                        node.right.Accept(this);
                        if (functionData.ret.Pop() != "eax")
                            throw new AST_BadFormatException(
                                "Return stack corrupted from function " + node.right.exp_Data, -1);

                        if (functionData.axUsed)
                        {
                            //We have a return value
                            //TODO - we shouldn't always have a return value
                            //TODO - we don't have to always move return value to something else
                            right = ToReg("eax");
                            if (popAx)
                            {
                                Result += "\tpop  \teax" + AST_Program.separator_line;
                            }
                        }
                    }
                    else if (node.right.exp_Type == Exp_Type.Operator)
                    {
                        //TODO - actually handle recurent operations
                        node.right.Accept(this);
                        right = functionData.ret.Pop();
                    }
                    else if (node.right.exp_Type == Exp_Type.Constant)
                    {
                        right = node.right.exp_Data;
                    }
                    else if (node.right.exp_Type == Exp_Type.Variable)
                    {
                        int indParam = functionData.parameters.FindIndex(a => a.Item1.Data == node.right.exp_Data);
                        int indLocal = functionData.locals.FindIndex(a => a.Item1.Data == node.right.exp_Data);
                        if (indLocal != -1)
                        {
                            //This variable exists as a local
                            right = functionData.locals[indParam].Item3;
                        }
                        else if (indParam != -1)
                        {
                            //This variable exists as a parameter
                            right = functionData.parameters[indParam].Item3;
                        }
                        else throw new AST_BadFormatException("Unknown variable " + node.right.exp_Data, -1);
                    }
                    else
                    {
                        throw new AST_BadFormatException(
                            "Not supported node of type " + node.right.exp_Type.ToString(), -1);
                    }

                    if (node.exp_Data == "+")
                    {
                        if (!IsReg(left))
                        {
                            left = ToReg(left);
                        }
                        if (convertTo32)
                        {
                            if (!IsReg(right) && GetDimentions(right) != 32)
                            {
                                right = ToReg(right);
                            }
                        }
                        if (GetDimentions(left) != GetDimentions(right))
                        {
                            left = SetDimentions(left, GetDimentions(right));
                        }
                        Result += "\tadd  \t" + left + ",\t" + right + AST_Program.separator_line;
                        if (IsReg(right)) ClearReg(SetDimentions(right, 32), true);
                        if(!convertTo32 && IsReg(left) && GetDimentions(left) != 32)
                        {
                            Result += "\tmovsx\t" + SetDimentions(left, 32) + ",\t" + left + AST_Program.separator_line;
                        }
                        functionData.ret.Push(left);
                    }
                    else if (node.exp_Data == "-")
                    {
                        if (!IsReg(left))
                        {
                            left = ToReg(left);
                        }
                        if (convertTo32)
                        {
                            if (!IsReg(right) && GetDimentions(right) != 32)
                            {
                                right = ToReg(right);
                            }
                        }
                        if (GetDimentions(left) != GetDimentions(right))
                        {
                            left = SetDimentions(left, GetDimentions(right));
                        }
                        Result += "\tsub  \t" + left + ",\t" + right + AST_Program.separator_line;
                        if (IsReg(right)) ClearReg(SetDimentions(right, 32), true);
                        if (!convertTo32 && IsReg(left) && GetDimentions(left) != 32)
                        {
                            Result += "\tmovsx\t" + SetDimentions(left, 32) + ",\t" + left + AST_Program.separator_line;
                        }
                        functionData.ret.Push(left);
                    }
                    else throw new NotImplementedException("Operation " + node.exp_Data + " is not supported");
                }
                //TODO - check if it is an empty return statement or something
                else throw new AST_BadFormatException("Operation " + node.exp_Data + " is not supported", -1);
            }
            else if (node.exp_Type == Exp_Type.Bracket)
            {
                node.left.Accept(this);
                throw new AST_BadFormatException("Bracket nodes are not supported and should be simplified", -1);
            }
            else throw new AST_BadFormatException("Not supported node of type " + node.exp_Type.ToString(), -1);
        }

        private void FreeReg(string input)
        {
            if (input == "eax")
            {
                functionData.axUsed = false;
            }
            else if (input == "ebx")
            {
                functionData.bxUsed = false;
            }
            else if (input == "ecx")
            {
                functionData.cxUsed = false;
            }
            else if (input == "edx")
            {
                functionData.dxUsed = false;
            }
            else
            {

            }
        }
        private bool IsReg(string input)
        {
            if (input == "eax" || input == "ebx" || input == "ecx" || input == "edx" ||
                input == "ax" || input == "bx" || input == "cx" || input == "dx" ||
                input == "ah" || input == "bh" || input == "ch" || input == "dh" ||
                input == "al" || input == "bl" || input == "cl" || input == "dl" ||
                input == "ebp" || input == "bp" || input == "esp" || input == "sp"
                )
            {
                return true;
            }
            else
            {

            }
            return false;
        }
        private string ToReg(string input)
        {
            int newDims = GetDimentions(input);
            string reg = "";
            if (!functionData.axUsed)
            {
                reg = "eax";
                functionData.axUsed = true;
            }
            else if (!functionData.bxUsed)
            {
                reg = "ebx";
                functionData.bxUsed = true;
            }
            else if (!functionData.cxUsed)
            {
                reg = "ecx";
                functionData.cxUsed = true;
            }
            else if (!functionData.dxUsed)
            {
                reg = "edx";
                functionData.dxUsed = true;
            }
            else throw new AST_Exception("Too many variables used. Aborting", -1);
            if (newDims != 32 && convertTo32)
            {
                Result += "\tmovsx\t" + reg + ",\t" + input + AST_Program.separator_line;
            }
            else
            {
                reg = SetDimentions(reg, newDims);
                Result += "\tmov  \t" + reg + ",\t" + input + AST_Program.separator_line;
            }
            return reg;
        }
        private void ClearReg(string input, bool free)
        {
            if (!IsReg(input)) throw new AST_BadFormatException(input + " is not a supported register", -1);
            if (free)
            {
                FreeReg(input);
            }
            Result += "\tmov  \t" + input + ",\t0" + AST_Program.separator_line;
        }
        private int GetDimentions(string input)
        {
            input = input.ToLower();
            if (input == "eax" || input == "ebx" || input == "ecx" || input == "edx" ||
                input == "ebp" || input == "esp")
            {
                return 32;
            }
            else if (input == "ax" || input == "bx" || input == "cx" || input == "dx" ||
                input == "bp" || input == "sp")
            {
                return 16;
            }
            else if (input == "ah" || input == "bh" || input == "ch" || input == "dh")
            {
                return 8;
            }
            else if (input == "al" || input == "bl" || input == "cl" || input == "dl")
            {
                return 8;
            }
            else if (input.StartsWith("word ptr"))
            {
                return 16;
            }
            else if (input.StartsWith("byte ptr"))
            {
                return 8;
            }
            else if (input[0] == '[')
            {
                input = input.Trim(new char[] { '[', ']' });
                string[] rg = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (IsReg(rg[0]))
                {
                    return GetDimentions(rg[0]);
                }
                else throw new AST_BadFormatException("Can\'t parse input - " + input, -1);
            }
            else return 32;
        }
        private string SetDimentions(string input, int newDims)
        {
            if (GetDimentions(input) == newDims) return input;
            //TODO - check for register parts usage, not for the register as a whole
            //if (newDims == 8) throw new AST_BadFormatException("There is no 8-bit stack pointer register", -1);

            List<List<Tuple<string, int>>> registers = new List<List<Tuple<string, int>>>
            {
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("eax", 32),
                    new Tuple<string, int>("ax", 16),
                    new Tuple<string, int>("al", 8),
                    },
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("ebx", 32),
                    new Tuple<string, int>("bx", 16),
                    new Tuple<string, int>("bl", 8),
                    },
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("ecx", 32),
                    new Tuple<string, int>("cx", 16),
                    new Tuple<string, int>("cl", 8),
                    },
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("edx", 32),
                    new Tuple<string, int>("dx", 16),
                    new Tuple<string, int>("dl", 8),
                    },
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("ebp", 32),
                    new Tuple<string, int>("bp", 16),
                    },
                new List<Tuple<string, int>>{
                    new Tuple<string, int>("esp", 32),
                    new Tuple<string, int>("sp", 16),
                    },
            };
            foreach (var l in registers)
            {
                int inputIndex = l.FindIndex(a => a.Item1 == input);
                if (inputIndex != -1)
                {
                    //We found our input register
                    int outputIndex = l.FindIndex(a => a.Item2 == newDims);
                    if (outputIndex == -1)
                        throw new AST_BadFormatException(
                            newDims.ToString() + "-bit version of " + input + " is not supported", -1);
                    //We found our output register
                    return l[outputIndex].Item1;
                }
            }
            throw new NotImplementedException(
                "Conversion of " + input + " to " + newDims.ToString() + " is not supported");
        }
    }
}
