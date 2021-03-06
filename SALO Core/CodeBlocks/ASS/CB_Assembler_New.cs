﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALO_Core.AST;
using SALO_Core.AST.Data;
using SALO_Core.AST.Logic;
using SALO_Core.CodeBlocks.Expressions;
using SALO_Core.Exceptions;
using SALO_Core.Exceptions.ASS;

namespace SALO_Core.CodeBlocks
{
    public class CB_Assembler_New : CB
    {
        public bool UpdateIsUsedLocal()
        {
            bool res = false;
            foreach (var f in functions)
            {
                if (f.isUsed)
                {
                    bool shouldUse = false;
                    foreach(object o in f.usedFrom)
                    {
                        if (o is CB_Assembler_New.Function)
                        {
                            if (o == f) continue;
                            if (((Function)o).isUsed)
                            {
                                shouldUse = true;
                                break;
                            }
                        }
                        else if (o is string)
                        {
                            shouldUse = true;
                            break;
                        }
                        else if (o is CodeBlocks.Function)
                        {
                            if (((CodeBlocks.Function)o).used)
                            {
                                shouldUse = true;
                                break;
                            }
                        }
                    }
                    if (!shouldUse)
                    {
                        f.isUsed = false;
                        res = true;
                    }
                }
            }
            return res;
        }
        public bool UpdateIsUsedLib()
        {
            bool res = false;
            foreach (var l in defaultLibs)
            {
                if (l.used)
                {
                    foreach (var f in l.functions)
                    {
                        if (f.used)
                        {
                            bool shouldUse = false;
                            foreach (object o in f.usedFrom)
                            {
                                if (o is CB_Assembler_New.Function)
                                {
                                    if (o == f) continue;
                                    if (((Function)o).isUsed)
                                    {
                                        shouldUse = true;
                                        break;
                                    }
                                }
                                else if (o is string)
                                {
                                    shouldUse = true;
                                    break;
                                }
                                else if (o is CodeBlocks.Function)
                                {
                                    if (((CodeBlocks.Function)o).used)
                                    {
                                        shouldUse = true;
                                        break;
                                    }
                                }
                            }
                            if (!shouldUse)
                            {
                                f.used = false;
                                res = true;
                            }
                        }
                    }
                    bool remLib = true;
                    foreach (var f in l.functions)
                    {
                        if (f.used) remLib = false;
                    }
                    if (remLib)
                    {
                        l.used = false;
                        res = true;
                    }
                }
            }
            return res;
        }
        public static string ToDataTypeLabel(IParameterType parameterType)
        {
            if (parameterType is PT_Void || parameterType is PT_None)
            {
                throw new ASS_Exception("Error converting parameter type to an assembler label",
                    new ASS_Exception("Empty parameter types can\'t be converted to an assembler label", -1), -1);
            }
            else if (parameterType is PT_Int32 || parameterType is PT_Ptr || parameterType is PT_Float32)
            {
                return "dd";
            }
            else if (parameterType is PT_Int16)
            {
                return "dw";
            }
            else if (parameterType is PT_Int8 || parameterType is PT_Lpcstr)
            {
                return "db";
            }
            else if (parameterType is PT_Struct)
            {
                return ((PT_Struct)parameterType).name;
            }
            else
            {
                throw new ASS_Exception("Error converting parameter type to an assembler label",
                    new NotImplementedException(parameterType.GetName() +
                        " can\'t be converted to an assembler label"), -1);
            }
        }
        protected abstract class Section<T>
        {
            protected string attributes;
            protected List<T> values;
            public Section(string attributes)
            {
                this.attributes = attributes;
                this.values = new List<T>();
            }
            public void AddValue(T value)
            {
                if (value != null)
                {
                    if (values.IndexOf(value) == -1)
                        values.Add(value);
                }
            }
            public void RemoveValue(T value)
            {
                if (value != null && values.IndexOf(value) != -1)
                    values.Remove(value);
            }
            public abstract string ConvertToString();
        }
        protected class Include : Section<string>
        {
            public Include() : base(null)
            {

            }
            public override string ConvertToString()
            {
                return ConvertToString(true);
            }
            public string ConvertToString(bool unfoldPATH)
            {
                string result = "";
                foreach (string t in values)
                {
                    string str = Environment.ExpandEnvironmentVariables(t);
                    bool fileExists = File.Exists(str);
                    if (!fileExists)
                    {
                        throw new AST_BadFormatException(str + " as (" + t + ") doesn\'t exist", -1);
                    }
                    if (unfoldPATH)
                    {
                        result += "include \'" + str.Replace("\\", "/") + "\'" + AST_Program.separator_line;
                    }
                    else
                    {
                        result += "include \'" + t + "\'" + AST_Program.separator_line;
                    }
                }
                result += AST_Program.separator_line;
                return result;
            }
        }
        protected class Data : Section<Variable>
        {
            public List<Structure> structures;
            public Data(string attributes) : base(attributes)
            {
                structures = new List<Structure>();
            }
            public Data() : this("data readable writeable")
            {

            }
            private bool IsSpecialSymbol(char symbol)
            {
                //TODO - rewrite this code to actually work
                if (symbol == '\\' || symbol == '\r' || symbol == '\n' ||
                    symbol == '\'' || symbol == '\"' || symbol == '\t')
                {
                    return true;
                }
                return false;
            }
            private bool IsSpecialSymbol(char symbol, char prev)
            {
                if (prev != '\\') return false;
                if (symbol == '\\' || symbol == 'r' || symbol == 'n' ||
                    symbol == '\'' || symbol == '\"' || symbol == 't')
                {
                    return true;
                }
                return false;
            }
            private byte ToSpecialSymbol(char symbol, char prev)
            {
                if (prev != '\\')
                    throw new ASS_Exception(prev + " is not an escape character", -1);
                char retVal = ' ';
                switch (symbol)
                {
                    case '\\':
                        retVal = '\\';
                        break;
                    case 'r':
                        retVal = '\r';
                        break;
                    case 'n':
                        retVal = '\n';
                        break;
                    case 't':
                        retVal = '\t';
                        break;
                    case '\'':
                        retVal = '\'';
                        break;
                    case '\"':
                        retVal = '\"';
                        break;
                    default:
                        throw new ASS_Exception(symbol + " is not recognised as a special symbol", -1);
                }
                byte[] b = Encoding.GetEncoding(1251).GetBytes(new char[] { retVal });
                return b[0];
            }
            static string UTF8ToWin1251(string sourceStr)
            {
                Encoding utf8 = Encoding.UTF8;
                Encoding win1251 = Encoding.GetEncoding(1251);
                byte[] utf8Bytes = utf8.GetBytes(sourceStr);
                byte[] win1251Bytes = Encoding.Convert(utf8, win1251, utf8Bytes);
                return win1251.GetString(win1251Bytes);
            }
            public Variable DeclareString(string input)
            {
                if (input[0] != '\"' || input[input.Length - 1] != '\"')
                    throw new ASS_Exception("Can\'t use a broken string literal", -1);
                input = input.Substring(1, input.Length - 2);
                string parsedInput = "";
                input = UTF8ToWin1251(input);
                byte[] byteData = Encoding.GetEncoding(1251).GetBytes(input.ToCharArray());
                //TODO - make better conversion
                for (int i = 0; i < byteData.Length; ++i)
                {
                    if (i < input.Length - 1 && IsSpecialSymbol(input[i + 1], input[i]))
                    {
                        //We have a special character
                        if (parsedInput.Length > 0 && parsedInput[parsedInput.Length - 1] == ',')
                        {
                            parsedInput += ToSpecialSymbol(input[i + 1], input[i]).
                                ToString(CultureInfo.InvariantCulture) + ",";
                        }
                        else if (parsedInput.Length == 0)
                        {
                            parsedInput += ToSpecialSymbol(input[i + 1], input[i]).
                                ToString(CultureInfo.InvariantCulture) + ",";
                        }
                        else
                        {
                            //We have a letter
                            parsedInput += "\"," + ToSpecialSymbol(input[i + 1], input[i]).
                                ToString(CultureInfo.InvariantCulture) + ",";
                        }
                        ++i;
                    }
                    else if (IsSpecialSymbol(input[i]))
                    {
                        if (i == 0 || (i > 0 && IsSpecialSymbol(input[i - 1])) || 
                            (i > 1 && IsSpecialSymbol(input[i - 1], input[i - 2])))
                        {
                            parsedInput += byteData[i] + ",";
                        }
                        else
                        {
                            parsedInput += "\"," + byteData[i] + ",";
                        }
                    }
                    else
                    {
                        //We have a simple string character
                        if (i == 0 || (i > 0 && IsSpecialSymbol(input[i - 1])) ||
                            (i > 1 && IsSpecialSymbol(input[i - 1], input[i - 2])))
                        {
                            parsedInput += "\"" + input[i];
                        }
                        else
                        {
                            parsedInput += input[i];
                        }
                        if (i == byteData.Length - 1)
                        {
                            parsedInput += "\",";
                        }
                    }
                }
                parsedInput += "0";
                Variable v = new Variable(new AST_Variable(null, "lpcstr", parsedInput),
                    ParameterType.GetParameterType("lpcstr"),
                    new Address("lpcstr" + values.Count.ToString(CultureInfo.InvariantCulture), -3));
                Variable found = values.Find(a =>
                    a.dataType.Equals(v.dataType) &&
                    a.address.length == v.address.length &&
                    a.ast_variable.Data == v.ast_variable.Data);
                if (found != null) return found;
                values.Add(v);
                return v;
            }
            public Variable Find(Predicate<Variable> predicate) => values.Find(predicate);
            public override string ConvertToString()
            {
                string result = "";
                if ((values != null && values.Count > 0) || (structures != null && structures.Count > 0))
                {
                    result += "section \'.data\' " + attributes + AST_Program.separator_line;
                }
                foreach(var s in structures)
                {
                    result += s.ConvertToString();
                }
                foreach (var t in values)
                {
                    if (t.address.length != -3)
                        throw new ASS_Exception(t.address.address +
                            " is not considered a proper global variable", -1);
                    result += t.address.address + " " +
                        ToDataTypeLabel(t.dataType) + " " +
                        t.ast_variable.Data + AST_Program.separator_line;
                }
                result += AST_Program.separator_line;

                return result;
            }
        }
        protected class IData : Section<Library>
        {
            public IData(string attributes) : base(attributes)
            {

            }
            public IData() : this("data import readable")
            {

            }
            public override string ConvertToString()
            {
                string result = "";

                if (values.Count > 0)
                {
                    result += "section \'.idata\' " + attributes + AST_Program.separator_line;
                    result += "library ";
                }
                foreach (Library t in values)
                {
                    if (!t.used) continue;
                    result += t.name + ",\'" + t.path + "\'";
                    if (t != values[values.Count - 1])
                    {
                        result += ",\\";
                    }
                    result += AST_Program.separator_line;
                }
                result += AST_Program.separator_line;
                foreach (Library t in values)
                {
                    if (!t.used) continue;
                    if (t.functions.Count > 0)
                    {

                        CodeBlocks.Function lastUsed = null;
                        foreach (var function in t.functions)
                        {
                            if (function.used) lastUsed = function;
                        }
                        if(lastUsed != null)
                        {
                            result += "import " + t.name + ",\\" + AST_Program.separator_line;
                            foreach (var function in t.functions)
                            {
                                if (!function.used) continue;
                                result += function.name + ",\'" + function.path + "\'";
                                if (function != lastUsed)
                                {
                                    result += ",\\";
                                }
                                result += AST_Program.separator_line;
                            }
                        }
                    }
                    result += AST_Program.separator_line;
                }
                result += AST_Program.separator_line;
                return result;
            }
        }
        protected class Code : Section<string>
        {
            public Code(string attributes) : base(attributes)
            {

            }
            public Code() : this("code readable executable")
            {

            }
            public override string ConvertToString()
            {
                string result = "";

                if (values.Count > 0)
                {
                    result += "section \'.text\' " + attributes + AST_Program.separator_line;
                }
                foreach (string t in values)
                {
                    result += t;
                }
                result += AST_Program.separator_line;

                return result;
            }
        }
        protected class Format : Section<string>
        {
            public Format() : base(null)
            {

            }
            public override string ConvertToString()
            {
                string result = "";
                foreach (string t in values)
                {
                    result += t + AST_Program.separator_line;
                }
                result += AST_Program.separator_line;
                return result;
            }
        }

        protected Format sec1;
        protected Include sec2;
        protected Data sec3;
        protected Code sec4;
        protected IData sec5;
        protected bool addComments;
        public List<Library> defaultLibs;
        public List<Function> functions;

        public CB_Assembler_New(bool addComments, List<CodeBlocks.Library> libraries)
        {
            this.addComments = addComments;
            this.defaultLibs = libraries;
            this.functions = new List<Function>();
            sec1 = new Format();
            sec2 = new Include();
            sec3 = new Data();
            sec4 = new Code();
            sec5 = new IData();

            sec2.AddValue("%SALOINCLUDE%\\win32amacro.inc");
            Library kernel = defaultLibs.Find(a => a.name == "kernel32");
            kernel.used = true;
            CodeBlocks.Function exitProcess = kernel.functions.Find(a => a.name == "ExitProcess");
            exitProcess.used = true;
            exitProcess.usedFrom.Add("global");
            //sec5.AddValue(kernel);
            //sec5.AddValue(msvcrt);
        }
        public override string GetResult()
        {
            List<string> funcStrings = new List<string>(functions.Count);
            List<bool> funcUsed = new List<bool>(functions.Count);
            foreach (var f in functions)
            {
                string fString = f.ConvertToString();
                funcStrings.Add(fString);
                //sec4.AddValue(f.ConvertToString());
            }
            bool keepLooping = true;
            while (keepLooping)
            {
                keepLooping = UpdateIsUsedLib() || UpdateIsUsedLocal();
            }
            foreach (var f in functions)
            {
                funcUsed.Add(f.isUsed);
            }
            for(int i = 0; i < Math.Min(funcStrings.Count, funcUsed.Count); ++i)
            {
                if (funcUsed[i]) sec4.AddValue(funcStrings[i]);
            }
            foreach (var l in defaultLibs)
            {
                if (l.used) sec5.AddValue(l);
            }
            Result =
                sec1.ConvertToString() +
                sec2.ConvertToString() +
                sec4.ConvertToString() +
                sec3.ConvertToString() +
                sec5.ConvertToString();
            return Result;
        }

        public override void Parse(AST_Program input)
        {
            sec1.AddValue("format PE console");
            sec1.AddValue("entry main");

            foreach (AST_Node node in input.childNodes)
            {
                node.Accept(this);
            }
        }

        public override void Parse(AST_GlobalVariable input)
        {
            sec3.AddValue(new Variable(
                new AST_Variable(input, input.variable.DataType.GetName(), 
                    input.variable.DataType is PT_Struct ? "" : "?"), 
                input.variable.DataType, 
                new Address(input.variable.Data, -3)
                ));
        }

        public override void Parse(AST_Structure input)
        {
            Structure structure = new Structure(input);
            sec3.structures.Add(structure);
        }

        public override void Parse(AST_Comment input)
        {
            foreach (string s in input.text)
            {
                sec4.AddValue("; " + s + AST_Program.separator_line);
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
            sec2.AddValue(input.file);
        }

        public override void Parse(AST_Define input)
        {
            //TODO - implement defines with no value as preprocessor directives
            if (input.token == null)
                throw new NotImplementedException();
            sec1.AddValue(input.identifier + " equ " + input.token);
        }
        public class Structure
        {
            public AST_Structure ast_Base { get; protected set; }
            public List<Variable> variables;
            public PT_Struct ast_Type { get; protected set; }
            public Structure(AST_Structure ast_Base)
            {
                this.ast_Base = ast_Base;
                variables = new List<Variable>();
                foreach(AST_Variable variable in ast_Base.variables)
                {
                    Variable cb_var = new Variable(
                        variable, variable.DataType, new Address(variable.Data, -3));
                    variables.Add(cb_var);
                }
                ast_Type = new PT_Struct()
                {
                    name = ast_Base.name,
                    children = variables.Select(a => a.dataType).ToList(),
                    childNames = variables.Select(a => a.address.address).ToList()
                };
            }
            public string ConvertToString()
            {
                string result = "";
                result += "struct " + ast_Base.name + AST_Program.separator_line;
                foreach(var v in variables)
                {
                    result += "\t" + v.address.address + 
                        " " + CB_Assembler_New.ToDataTypeLabel(v.dataType) + 
                        " ?" + AST_Program.separator_line;
                }
                result += "ends" + AST_Program.separator_line;
                return result;
            }
            public int GetMemberOffset(Variable variable)
            {
                int res = 0;
                int index = 0;
                while (index < variables.Count && variables[index] != variable)
                {
                    res += variables[index].dataType.GetLengthInBytes();
                    index++;
                }
                if (index >= variables.Count)
                    throw new ASS_Exception("Couldn\'t find variable " + variable.ConvertToString(), -1);
                return res;
            }
            public Variable FindByName(string varName)
            {
                return variables.Find(a => a.address.address == varName);
            }
        }
        public class Address
        {
            public string address;
            public int offset;
            /// <summary>
            /// -1 for register, -2 for constant, -3 for global variable , -4 for function labels
            /// </summary>
            public int length;
            public bool isStructure = false;
            public Address(string baseAddress)
            {
                this.address = baseAddress;
            }
            public Address(string baseAddress, int length)
            {
                this.address = baseAddress;
                this.length = length;
            }
            public Address(string baseAddress, int length, int offset)
            {
                this.address = baseAddress;
                this.length = length;
                this.offset = offset;
            }
            public Address(string baseAddress, IParameterType length, int offset) :
                this(baseAddress, GetLength(length), offset)
            {

            }
            public static Address Advance(Address baseAddress, IParameterType length)
            {
                return new Address(baseAddress.address, length, baseAddress.offset + GetLength(length));
            }
            public static Address Advance(Address baseAddress, int length)
            {
                return new Address(baseAddress.address, length, baseAddress.offset + length);
            }
            public string ConvertToString()
            {
                return GetPtr(length);
            }
            public string GetPtr(int length)
            {
                switch (length)
                {
                    case -4:
                        return address;
                    case -3:
                        return address;
                    case -2:
                        return address;
                    case -1:
                        return address;
                    case 1:
                        if (IsRegister())
                        {
                            //eax -> ax -> a -> al
                            return address.Remove(0, 1).Remove(1) + "l";
                        }
                        if (offset == 0) return "byte ptr " + address;
                        return "byte ptr " + address +  (offset < 0 ? "-" + (-offset).
                            ToString(CultureInfo.InvariantCulture) :
                            "+" + offset.ToString(CultureInfo.InvariantCulture));
                    case 2:
                        if (IsRegister())
                        {
                            return address.Remove(0, 1);
                        }
                        if (offset == 0) return "word ptr " + address;
                        return "word ptr " + address + (offset < 0 ? "-" + (-offset).
                            ToString(CultureInfo.InvariantCulture) :
                            "+" + offset.ToString(CultureInfo.InvariantCulture));
                    default:
                        if (offset == 0) return "dword ptr " + address;
                        return "dword ptr " + address + (offset < 0 ? "-" + (-offset).
                            ToString(CultureInfo.InvariantCulture) :
                            "+" + offset.ToString(CultureInfo.InvariantCulture));
                }
            }
            public static int GetLength(IParameterType dataType)
            {
                return dataType.GetLengthInBytes();
            }
            public bool IsRegister()
            {
                if (length != -1)
                {
                    return false;
                }
                if (address == "eax" || address == "ebx" || address == "ecx" || address == "edx")
                {
                    return true;
                }
                if (address.StartsWith("xmm")) return false;
                throw new NotImplementedException("Register " + address + " is not supported");
            }
            public bool IsConstant()
            {
                if (length == -2) return true;
                return false;
            }
            public bool IsFloatRegister()
            {
                if (length != -1) return false;
                return address.StartsWith("xmm");
            }
            public bool IsGlobal()
            {
                if (length == -3) return true;
                return false;
            }
            public bool IsStack()
            {
                if (IsConstant()) return false;
                if (IsGlobal()) return false;
                if (IsRegister()) return false;
                if (address == "ebp") return true;
                return false;
            }
        }
        public class Variable
        {
            public AST_Variable ast_variable;
            public Address address;
            public IParameterType dataType;
            public Variable(AST_Variable ast_variable, IParameterType dataType, Address address)
            {
                this.ast_variable = ast_variable;
                this.dataType = dataType;
                this.address = address;
            }
            public string ConvertToString()
            {
                if(address.length == -1 && address.IsRegister() && dataType.GetLengthInBytes() != 4)
                {
                    return address.GetPtr(dataType.GetLengthInBytes());
                }
                else if (address.length == -3 && !(dataType is PT_Lpcstr))
                {
                    return address.GetPtr(dataType.GetLengthInBytes());
                }
                else
                {
                    return address.ConvertToString();
                }
            }
        }
        public class MemoryManager
        {
            private bool axTaken = false, bxTaken = false, cxTaken = false, dxTaken = false;
            private bool[] xmmTaken = new bool[10];
            public bool usesBX = false, usesCX = false, usesDX = false;
            Variable stackStart;
            Stack<Variable> stack;
            Variable stackTop
            {
                get
                {
                    return stack == null ?
                        null : (stack.Count > 0 ? stack.Peek() : null);
                }
            }
            public MemoryManager(Variable newStackTop)
            {
                this.stackStart = newStackTop;
                this.stack = new Stack<Variable>();
                //stack.Push(newStackTop);
                axTaken = false;
                bxTaken = false;
                cxTaken = false;
                dxTaken = false;
            }
            public bool IsTaken(string name)
            {
                if (name == "eax") return axTaken;
                if (name == "ebx") return bxTaken;
                if (name == "ecx") return cxTaken;
                if (name == "edx") return dxTaken;
                throw new NotImplementedException("Can\'t check register " + name);
            }
            public void UpdateRegisterUsage(Variable newVar)
            {
                if (newVar.address.length != -1) return;
                if (newVar.address.address == "eax") axTaken = true;
                if (newVar.address.address == "ebx") { bxTaken = true; usesBX = true; }
                if (newVar.address.address == "ecx") { cxTaken = true; usesCX = true; }
                if (newVar.address.address == "edx") { dxTaken = true; usesDX = true; }
            }
            public Variable GetFreeAddress(AST_Variable variable, IParameterType dataType)
            {
                Variable result;
                /*if (!axTaken)
                {
                    axTaken = true;
                    return new Variable(null, dataType, new Address("eax", -1));
                }
                else */if (!bxTaken)
                {
                    bxTaken = true;
                    usesBX = true;
                    return new Variable(null, dataType, new Address("ebx", -1));
                }
                else if (!cxTaken)
                {
                    cxTaken = true;
                    usesCX = true;
                    return new Variable(null, dataType, new Address("ecx", -1));
                }
                else if (!dxTaken)
                {
                    dxTaken = true;
                    usesDX = true;
                    return new Variable(null, dataType, new Address("edx", -1));
                }
                else
                {
                    if (stack.Count == 0)
                    {
                        result = new Variable(variable, dataType, Address.Advance(stackStart.address, dataType));
                    }
                    else
                    {
                        result = new Variable(variable, dataType, Address.Advance(stackTop.address, dataType));
                    }

                    stack.Push(result);
                    return result;
                }
            }
            public Variable GetFreeFloatReg(PT_Float32 dataType)
            {
                int index = 0;
                while (index < 10 && xmmTaken[index]) index++;
                if (index >= 10) throw new NotImplementedException("No more xmm registers left");
                xmmTaken[index] = true;
                return new Variable(null, dataType, new Address(
                    "xmm" + index.ToString(CultureInfo.InvariantCulture), -1));
            }
            public void SetFreeFloatReg(Variable oldVariable)
            {
                if(oldVariable.address.length == -1 && oldVariable.address.address.StartsWith("xmm"))
                {
                    int index = int.Parse(
                        oldVariable.address.address.Replace("xmm", ""), 
                        CultureInfo.InvariantCulture);
                    if (!xmmTaken[index])
                        throw new ASS_Exception("Memory manager corrupt: " + oldVariable.address.address, -1);
                    xmmTaken[index] = false;
                }
                else
                {
                    SetFreeAddress(oldVariable);
                }
            }
            public Variable GetFreeRegister(IParameterType dataType)
            {
                if (dataType.GetLengthInBytes() > 0)
                {
                    string reg = "";
                    /*if (axTaken == false)
                    {
                        reg = "eax";
                        axTaken = true;
                    }
                    else */if (bxTaken == false)
                    {
                        reg = "ebx";
                        bxTaken = true;
                        usesBX = true;
                    }
                    else if (cxTaken == false)
                    {
                        reg = "ecx";
                        cxTaken = true;
                        usesCX = true;
                    }
                    else if (dxTaken == false)
                    {
                        reg = "edx";
                        dxTaken = true;
                        usesDX = true;
                    }
                    else throw new NotImplementedException("No more registers left");
                    return new Variable(null, dataType, new Address(reg, -1));
                }
                else throw new NotImplementedException(
                    dataType.ToString() + " is not yet supported for register assignment");
            }
            public void SetFreeAddress(Variable oldVariable)
            {
                if (oldVariable == null) return;
                if (oldVariable.address.length == -1)
                {
                    if (oldVariable.address.address.StartsWith("xmm"))
                    {
                        SetFreeFloatReg(oldVariable);
                        return;
                    }
                    if (oldVariable.address.address == "eax")
                    {
                        if (!axTaken)
                        {
                            throw new ASS_Exception("Memory manager corrupt (eax)", -1);
                        }
                        axTaken = false;
                    }
                    else if (oldVariable.address.address == "ebx")
                    {
                        if (!bxTaken)
                        {
                            throw new ASS_Exception("Memory manager corrupt (ebx)", -1);
                        }
                        bxTaken = false;
                    }
                    else if (oldVariable.address.address == "ecx")
                    {
                        if (!cxTaken)
                        {
                            throw new ASS_Exception("Memory manager corrupt (ecx)", -1);
                        }
                        cxTaken = false;
                    }
                    else if (oldVariable.address.address == "edx")
                    {
                        if (!dxTaken)
                        {
                            throw new ASS_Exception("Memory manager corrupt (edx)", -1);
                        }
                        dxTaken = false;
                    }
                    else
                    {
                        throw new NotImplementedException(
                            "Most registers are not supported: " + oldVariable.address.address);
                    }
                }
                else if (oldVariable.address.length == -2)
                {
                    return;
                }
                else if (oldVariable.address.length == -3)
                {
                    return;
                }
                else if (oldVariable.address.length == -4)
                {
                    return;
                }
                else
                {
                    //Check for function parameters
                    if (stack.Count == 0 &&
                        oldVariable.address.address == "ebp"/* &&
                        oldVariable.address.offset > 0*/) return;
                    if (oldVariable.address.isStructure) return;
                    if (stack.Peek() != oldVariable)
                    {
                        throw new ASS_Exception("Memory manager stack corrupt", -1);
                    }
                    stack.Pop();
                }
            }
        }
        public class Function
        {
            private AST_Function ast_function;
            public List<Variable> parameters, locals;
            public string name;
            public AccessLevel accessLevel;
            public FunctionType functionType;
            public AST_Type returnValue;
            string start, end;
            bool usesReturns = false, usesLocals = false;
            MemoryManager memoryManager;
            CB_Assembler_New parent;
            int localLabels = 0;
            public bool isUsed = false;
            public List<object> usedFrom = new List<object>();
            public Function(AST_Function ast_function, CB_Assembler_New parent)
            {
                this.parent = parent;
                this.name = ast_function.name;
                if(ast_function.name == "main")
                {
                    isUsed = true;
                    usedFrom.Add("global");
                }
                this.ast_function = ast_function;
                this.parameters = new List<Variable>();
                this.locals = new List<Variable>();
                this.accessLevel = ast_function.accessLevel;
                //TODO - use function type
                this.functionType = ast_function.functionType;
                this.returnValue = ast_function.retValue;
                start = end = "";

                int addr = 4;
                for (int i = 0; i < ast_function.parameters.Count; ++i)
                {
                    var p = ast_function.parameters.ElementAt(i);
                    addr += 4;// p.DataType.GetLengthInBytes();
                    this.parameters.Add(new Variable(p, p.DataType, new Address("ebp", p.DataType, addr)));
                }
                

                if (this.parameters.Count > 0)
                    memoryManager = new MemoryManager(this.parameters[this.parameters.Count - 1]);
                else memoryManager = new MemoryManager(
                    new Variable(null, ParameterType.GetParameterType("none"), new Address("ebp", 4, 4)));
            }

            public int GetByteCount(IEnumerable<Variable> variables)
            {
                int result = 0;
                foreach (var v in variables)
                {
                    //TODO - are you sure you want 4?
                    if (v.address.length >= 0)
                        result += v.address.length;
                    else result += 4;
                }
                return result;
            }
            //This function was ambiguous
            //public int GetByteCount()
            //{
            //    return GetByteCount(parameters);
            //}
            public string ConvertToString()
            {
                string result = "";
                foreach (var t in parameters)
                {
                    result += "; " + t.ast_variable.Data +
                              " : " + t.ast_variable.DataType.GetName() +
                              " as " + t.ConvertToString() +
                              AST_Program.separator_line;
                }
                result += name + ":" + AST_Program.separator_line;
                string tempResult = "";

                if(ast_function.expressions == null)
                {
                    throw new ASS_Exception("Function " + ast_function.name + " has no expressions", -1);
                }

                foreach (AST_Expression input in ast_function.expressions)
                {
                    string resString;
                    if (input is AST_Native)
                    {
                        ParseNative((AST_Native)input, out resString);
                    }
                    else if (input is AST_LocalVariable)
                    {
                        ParseLocal((AST_LocalVariable)input, out resString);
                    }
                    else if (input is AST_Logic)
                    {
                        ParseLogic((AST_Logic)input, out resString);
                    }
                    else
                    {
                        var resVariable = ParseExpression(input, out resString);
                        if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                    }
                    tempResult += resString;
                }
                //Add start and end
                if ((this.parameters != null && this.parameters.Count > 0 &&
                !(this.parameters.Count == 1 &&
                  this.parameters[0].dataType.GetName() == "void"))
                || (this.locals != null && this.locals.Count > 0))
                {
                    start += "\t push \tebp" + AST_Program.separator_line;
                    start += "\t  mov  \tebp,\tesp" + AST_Program.separator_line;
                    end += "\t  mov  \tesp,\tebp" + AST_Program.separator_line;
                    end += "\t  pop  \tebp" + AST_Program.separator_line;
                }
                if (name == "main")
                {
                    end += "\tinvoke\tExitProcess,\teax" + AST_Program.separator_line;
                }
                else
                {
                    if (functionType == FunctionType.cdecl)
                    {
                        end += "  ret" + AST_Program.separator_line;
                    }
                    else if (functionType == FunctionType.stdcall)
                    {
                        end += "  ret " + GetByteCount(parameters).ToString(CultureInfo.InvariantCulture)
                            + AST_Program.separator_line;
                    }
                    else throw new NotImplementedException(functionType + " is not supported");
                }

                result += start;

                if (locals.Count > 0 && usesLocals)
                {
                    int ceilingInput = GetByteCount(locals);
                    int ceilingResult = ((ceilingInput / 16) + (ceilingInput % 16 == 0 ? 0 : 1)) * 16;
                    result += "\t  sub\tesp,\t" + ceilingResult.ToString(CultureInfo.InvariantCulture)
                        + AST_Program.separator_line;
                }

                if (memoryManager.usesBX)
                {
                    result += "\t push\tebx" + AST_Program.separator_line;
                }
                if (memoryManager.usesCX)
                {
                    result += "\t push\tecx" + AST_Program.separator_line;
                }
                if (memoryManager.usesDX)
                {
                    result += "\t push\tedx" + AST_Program.separator_line;
                }

                result += tempResult;
                if (usesReturns)
                {
                    result += "." + name + "_return:" + AST_Program.separator_line;
                }
                if (memoryManager.usesDX)
                {
                    result += "\t  pop\tedx" + AST_Program.separator_line;
                }
                if (memoryManager.usesCX)
                {
                    result += "\t  pop\tecx" + AST_Program.separator_line;
                }
                if (memoryManager.usesBX)
                {
                    result += "\t  pop\tebx" + AST_Program.separator_line;
                }
                result += end;
                result += AST_Program.separator_line;
                return result;
            }
            private void ParseLogic (AST_Logic input, out string resString)
            {
                string result = "";
                if (input is AST_If)
                {
                    AST_If statement = (AST_If)input;

                    Exp_Statement exp = new Exp_Statement(statement.inside.nodes);
#if DEBUG
                    string output = "";
                    exp.Print("", false, ref output);
                    Console.WriteLine(output);
#endif
                    if ((exp.head.exp_Type == Exp_Type.Operator && IsOperatorLogic(exp.head.exp_Operator))
                        || exp.head.exp_Type == Exp_Type.Constant)
                    {
                        if(statement.else_expressions == null)
                        {
                            //Create a label to get out of scope
                            string restLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);

                            //Parse conditional expression
                            string conditionResultString = "";
                            var conditionReturn = ParseLogicNode(statement.inside, exp, exp.head,
                                out conditionResultString, restLabel);
                            memoryManager.SetFreeAddress(conditionReturn);
                            if (parent.addComments)
                            {
                                result += ";if statement " + string.Join(" ", statement.inside.nodes) +
                                    AST_Program.separator_line;
                            }
                            result += conditionResultString;

                            //Parse expressions inside of the scope
                            string expressionOutput = "";
                            foreach (var scopedAstExpression in statement.expressions)
                            {
                                string scopedExpressionOutput = "";

                                if (scopedAstExpression is AST_Native)
                                {
                                    ParseNative((AST_Native)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_LocalVariable)
                                {
                                    ParseLocal((AST_LocalVariable)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_Logic)
                                {
                                    ParseLogic((AST_Logic)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else
                                {
                                    var resVariable = ParseExpression(scopedAstExpression, out scopedExpressionOutput);
                                    if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                                }

                                expressionOutput += scopedExpressionOutput;
                            }
                            result += expressionOutput;

                            result += restLabel + ":" + AST_Program.separator_line;
                        }
                        else
                        {
                            //We have an if-else
                            //Create a label to get out of scope
                            string elseLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);
                            string restLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);

                            //Parse conditional expression
                            string conditionResultString = "";
                            var conditionReturn = ParseLogicNode(statement.inside, exp, exp.head,
                                out conditionResultString, elseLabel);
                            memoryManager.SetFreeAddress(conditionReturn);
                            if (parent.addComments)
                            {
                                result += ";if-else statement " + string.Join(" ", statement.inside.nodes) +
                                    AST_Program.separator_line;
                            }
                            result += conditionResultString;

                            //Parse expressions inside of the scope
                            string expressionOutput = "";
                            foreach (var scopedAstExpression in statement.expressions)
                            {
                                string scopedExpressionOutput = "";

                                if (scopedAstExpression is AST_Native)
                                {
                                    ParseNative((AST_Native)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_LocalVariable)
                                {
                                    ParseLocal((AST_LocalVariable)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_Logic)
                                {
                                    ParseLogic((AST_Logic)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else
                                {
                                    var resVariable = ParseExpression(scopedAstExpression, out scopedExpressionOutput);
                                    if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                                }

                                expressionOutput += scopedExpressionOutput;
                            }
                            result += expressionOutput;
                            result += "\t  jmp\t" + restLabel + AST_Program.separator_line;

                            result += elseLabel + ":" + AST_Program.separator_line;

                            expressionOutput = "";
                            foreach (var scopedAstExpression in statement.else_expressions)
                            {
                                string scopedExpressionOutput = "";

                                if (scopedAstExpression is AST_Native)
                                {
                                    ParseNative((AST_Native)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_LocalVariable)
                                {
                                    ParseLocal((AST_LocalVariable)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else if (scopedAstExpression is AST_Logic)
                                {
                                    ParseLogic((AST_Logic)scopedAstExpression, out scopedExpressionOutput);
                                }
                                else
                                {
                                    var resVariable = ParseExpression(scopedAstExpression, out scopedExpressionOutput);
                                    if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                                }

                                expressionOutput += scopedExpressionOutput;
                            }
                            result += expressionOutput;

                            result += restLabel + ":" + AST_Program.separator_line;
                        }
                    }
                    else throw new ASS_Exception("Bad expression format",
                        new ASS_Exception(exp.head.exp_Data + " is not supported", -1), -1);
                }
                else if (input is AST_While)
                {
                    AST_While statement = (AST_While)input;

                    Exp_Statement exp = new Exp_Statement(statement.inside.nodes);
#if DEBUG
                    string output = "";
                    exp.Print("", false, ref output);
                    Console.WriteLine(output);
#endif
                    if (IsOperatorLogic(exp.head.exp_Operator))
                    {
                        //Create a label to get out of scope
                        string startLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);
                        string restLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);

                        result += startLabel + ":" + AST_Program.separator_line;

                        //Parse conditional expression
                        string conditionResultString = "";
                        var conditionReturn = ParseLogicNode(statement.inside, exp, exp.head,
                            out conditionResultString, restLabel);
                        memoryManager.SetFreeAddress(conditionReturn);
                        if (parent.addComments)
                        {
                            result += ";logical statement " + string.Join(" ", statement.inside.nodes) +
                                AST_Program.separator_line;
                        }
                        result += conditionResultString;

                        //Parse expressions inside of the scope
                        string expressionOutput = "";
                        foreach (var scopedAstExpression in statement.expressions)
                        {
                            string scopedExpressionOutput = "";

                            if (scopedAstExpression is AST_Native)
                            {
                                ParseNative((AST_Native)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else if (scopedAstExpression is AST_LocalVariable)
                            {
                                ParseLocal((AST_LocalVariable)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else if (scopedAstExpression is AST_Logic)
                            {
                                ParseLogic((AST_Logic)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else
                            {
                                var resVariable = ParseExpression(scopedAstExpression, out scopedExpressionOutput);
                                if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                            }

                            expressionOutput += scopedExpressionOutput;
                        }
                        result += expressionOutput;
                        result += "\t  jmp\t" + startLabel + AST_Program.separator_line;

                        result += restLabel + ":" + AST_Program.separator_line;
                    }
                    else throw new ASS_Exception("Bad expression format",
                        new ASS_Exception(exp.head.exp_Data + " is not supported", -1), -1);
                }
                else if (input is AST_For)
                {
                    AST_For statement = (AST_For)input;

                    string initExpressionOutput = "";
                    var initResVariable = ParseExpression(statement.initializer, out initExpressionOutput);
                    if (initResVariable.address.IsRegister()) memoryManager.SetFreeAddress(initResVariable);
                    result += initExpressionOutput;

                    Exp_Statement exp = new Exp_Statement(statement.condition.nodes);
#if DEBUG
                    string output = "";
                    exp.Print("", false, ref output);
                    Console.WriteLine(output);
#endif
                    if (IsOperatorLogic(exp.head.exp_Operator))
                    {
                        //Create a label to get out of scope
                        string startLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);
                        string restLabel = ".L" + (localLabels++).ToString(CultureInfo.InvariantCulture);

                        result += startLabel + ":" + AST_Program.separator_line;

                        //Parse conditional expression
                        string conditionResultString = "";
                        var conditionReturn = ParseLogicNode(statement.condition, exp, exp.head,
                            out conditionResultString, restLabel);
                        memoryManager.SetFreeAddress(conditionReturn);
                        if (parent.addComments)
                        {
                            result += ";logical statement " + string.Join(" ", statement.condition.nodes) +
                                AST_Program.separator_line;
                        }
                        result += conditionResultString;

                        //Parse expressions inside of the scope
                        string expressionOutput = "";
                        foreach (var scopedAstExpression in statement.expressions)
                        {
                            string scopedExpressionOutput = "";

                            if (scopedAstExpression is AST_Native)
                            {
                                ParseNative((AST_Native)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else if (scopedAstExpression is AST_LocalVariable)
                            {
                                ParseLocal((AST_LocalVariable)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else if (scopedAstExpression is AST_Logic)
                            {
                                ParseLogic((AST_Logic)scopedAstExpression, out scopedExpressionOutput);
                            }
                            else
                            {
                                var resVariable = ParseExpression(scopedAstExpression, out scopedExpressionOutput);
                                if (resVariable.address.IsRegister()) memoryManager.SetFreeAddress(resVariable);
                            }

                            expressionOutput += scopedExpressionOutput;
                        }
                        result += expressionOutput;

                        string loopExpressionOutput = "";
                        var loopResVariable = ParseExpression(statement.loop, out loopExpressionOutput);
                        if (loopResVariable.address.IsRegister()) memoryManager.SetFreeAddress(loopResVariable);
                        result += loopExpressionOutput;

                        result += "\t  jmp\t" + startLabel + AST_Program.separator_line;

                        result += restLabel + ":" + AST_Program.separator_line;
                    }
                    else throw new ASS_Exception("Bad expression format",
                        new ASS_Exception(exp.head.exp_Data + " is not supported", -1), -1);
                }
                else throw new NotImplementedException(input.GetType().ToString() + " is not yet supported");

                resString = result;
            }
            private bool IsOperatorLogicConnector(AST_Operator ast_operator)
            {
                if (!ast_operator.init) return false;
                switch (ast_operator.oper)
                {
                    case "!=":
                    case "||":
                    case "&&":
                    case "==": return true;
                    default: return false;
                }
            }
            private bool IsOperatorLogic(AST_Operator ast_operator)
            {
                if (!ast_operator.init) return false;
                switch (ast_operator.oper)
                {
                    case ">":
                    case "<":
                    case ">=":
                    case "<=":
                    case "==":
                    case "!=": return true;
                    default: return false;
                }
            }
            private void ParseNative(AST_Native input, out string resString)
            {
                resString = "";
                if (parent.addComments)
                {
                    resString += ";native" + AST_Program.separator_line;
                }
                resString += input.code + AST_Program.separator_line;
            }
            private bool HasOperator(Exp_Node input, string oper)
            {
                if (input.exp_Operator.init && input.exp_Operator.oper == oper) return true;
                if(input.left != null)
                {
                    if (HasOperator(input.left, oper)) return true;
                }
                if (input.right != null)
                {
                    if (HasOperator(input.right, oper)) return true;
                }
                return false;
            }
            private Variable ParseLogicNode(AST_Expression exp, Exp_Statement statement, 
                Exp_Node input, out string resString, string endLabel)
            {
                string result = "";
                Variable res = null;
                if (input.left != null && input.right != null)
                {
                    if(HasOperator(input, "="))
                    {
                        throw new ASS_Exception("Failed to compile child nodes of logical expression",
                            new ASS_Exception("Can\'t perform assignment inside a logic node", -1), -1);
                    }
                    //Parse left, then right, then operator
                    string leftOutStr = "";
                    Variable leftOutVar = null;
                    leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                    result += leftOutStr;

                    Variable rightOutVar = null;
                    string rightOutStr = "";
                    if ((input.right.exp_Type == Exp_Type.Operator && input.right.exp_Operator.init &&
                        input.right.exp_Operator.layer < input.exp_Operator.layer) ||
                        input.right.exp_Type == Exp_Type.Function ||
                        true)
                    {
                        rightOutVar = ParseExpNode(exp, statement, input.right, out rightOutStr);
                    }
                    if (leftOutVar.address.IsConstant() || leftOutVar.address.IsGlobal() ||
                        (leftOutVar.address.IsStack() &&
                        (rightOutVar.address.IsStack() || input.exp_Data != "=")))
                    {
                        //We have to move our expression result to a register
                        Variable leftTempVar = leftOutVar;
                        leftOutVar = memoryManager.GetFreeRegister(leftTempVar.dataType);
                        //memoryManager.UpdateRegisterUsage(leftOutVar);
                        result += "\t  mov\t" + leftOutVar.ConvertToString() +
                            ",\t" + leftTempVar.ConvertToString() + AST_Program.separator_line;
                        memoryManager.SetFreeAddress(leftTempVar);
                    }
                    result += rightOutStr;
                    if (!leftOutVar.dataType.Equals(rightOutVar.dataType))
                    {
                        throw new NotImplementedException("Can\'t perform " + input.exp_Data +
                            " on " + leftOutVar.dataType + " and " + rightOutVar.dataType);
                    }

                    if (input.exp_Data == "==")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  jne\t" + endLabel + AST_Program.separator_line;
                    }
                    else if (input.exp_Data == "!=")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  je\t" + endLabel + AST_Program.separator_line;
                    }
                    else if (input.exp_Data == ">")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  jle\t" + endLabel + AST_Program.separator_line;
                    }
                    else if (input.exp_Data == ">=")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  jl\t" + endLabel + AST_Program.separator_line;
                    }
                    else if (input.exp_Data == "<")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  jge\t" + endLabel + AST_Program.separator_line;
                    }
                    else if (input.exp_Data == "<=")
                    {
                        result += "\t  cmp\t" + leftOutVar.ConvertToString() +
                            ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                        //TODO - swap this to a bool variable? Then change memoryManager call in ParseLogic
                        res = leftOutVar;
                        memoryManager.SetFreeAddress(rightOutVar);
                        result += "\t  jg\t" + endLabel + AST_Program.separator_line;
                    }
                    else throw new NotImplementedException(input.exp_Data + " is not supported");
                }
                else if (input.exp_Type == Exp_Type.Constant)
                {
                    if(ParameterType.Parse(input.exp_Data, (dynamic)Exp_Statement.GetDataType(input)) == 0)
                    {
                        result += "\t  jmp\t" + endLabel + AST_Program.separator_line;
                    }
                    res = null;
                }
                else if (input.exp_Type == Exp_Type.Variable)
                {
                    string leftOutStr = "";
                    Variable leftOutVar = null;
                    leftOutVar = ParseExpNode(exp, statement, input, out leftOutStr);
                    result += leftOutStr;
                    //Compare to zero, jump if true
                    result += "\t   or\t" + leftOutVar.ConvertToString() +
                        ",\t" + leftOutVar.ConvertToString() + AST_Program.separator_line;
                    result += "\t   jz\t" + endLabel + AST_Program.separator_line;
                }
                else throw new NotImplementedException(input.exp_Data + " is not yet supported");

                resString = result;
                return res;
            }
            private void ParseLocal(AST_LocalVariable input, out string resString)
            {
                AST_Variable variable = (AST_Variable)input.childNodes.First.Value;
                int addr = 0;
                if(locals.Count > 0)
                {
                    addr = locals[locals.Count - 1].address.offset;
                }
                int varLength = variable.DataType.GetLengthInBytes();
                if (Math.Abs(addr) % varLength != 0)
                {
                    addr -= varLength - (Math.Abs(addr) % varLength);
                }
                addr -= varLength;
                if(locals.Find(a => a.ast_variable.Data == variable.Data) != null)
                {
                    //We already declared a local variable wih this name
                    throw new ASS_Exception("Local variable declaration exception",
                        new ASS_Exception("Variable " + variable.Data + " was already declared in this function",
                        -1), -1);
                }
                locals.Add(new Variable(variable, variable.DataType, new Address("ebp", variable.DataType, addr)));
                //In case we need it for the future
                resString = "";
                if (parent.addComments)
                {
                    resString += ";local " + variable.DataType.GetName() + 
                        " variable " + variable.Data + 
                        " at " + locals[locals.Count - 1].ConvertToString() + AST_Program.separator_line;
                }
            }
            private Variable ParseExpression(AST_Expression input, out string resString)
            {
                //TODO - move this to AST_Expression
                Exp_Statement exp = new Exp_Statement(input.nodes);
#if DEBUG
                string output = "";
                exp.Print("", false, ref output);
                Console.WriteLine(output);
#endif
                string tempResString = "";
                if ((exp.head.exp_Type == Exp_Type.Operator && exp.head.exp_Operator.init &&
                    (exp.head.exp_Data == "=" || exp.head.exp_Data == "return" || 
                    (exp.head.exp_Data == "." && exp.head.right.exp_Type == Exp_Type.Function) ||
                    (exp.head.exp_Data == "->" && exp.head.right.exp_Type == Exp_Type.Function)
                    )) ||
                    (exp.head.exp_Type == Exp_Type.Function))
                {
                    var res = ParseExpNode(input, exp, exp.head, out tempResString);
                    resString = "";
                    if (parent.addComments)
                    {
                        resString += ";expression " + string.Join(" ", input.nodes) + AST_Program.separator_line;
                    }
                    resString += tempResString;
                    return res;
                }
                else throw new ASS_Exception("Bad expression format",
                    new ASS_Exception("Statement should be assignment or a function call", -1), -1);
                //return null;
            }
            private Variable ParseExpNode(AST_Expression exp, Exp_Statement statement, 
                Exp_Node input, out string resString)
            {
                string result = "";
                Variable res = null;
                if (input.exp_Type == Exp_Type.Operator)
                {
                    if (input.left == null && input.right == null)
                    {
                        //We have a single-operator expression (e.g. return)
                        if (input.exp_Data == "return")
                        {
                            if (returnValue.DataType.GetName() != "void")
                            {
                                throw new ASS_Exception("Can\'t return from a non-void function " + name, -1);
                            }
                            result += "\t  mov\teax,\t0" + AST_Program.separator_line;
                            if (this.ast_function.expressions.Last.Value != exp)
                            {
                                usesReturns = true;
                                result += "\t  jmp\t." + name + "_return" + AST_Program.separator_line;
                            }
                            res = null;
                        }
                        else throw new NotImplementedException(input.exp_Data + " is not supported");
                    }
                    else if (input.left == null && input.right != null)
                    {
                        if (input.exp_Data == "return")
                        {
                            string rightOutStr = "";
                            Variable rightOutVar = null;
                            rightOutVar = ParseExpNode(exp, statement, input.right, out rightOutStr);
                            if (!rightOutVar.dataType.Equals(returnValue.DataType))
                            {
                                throw new ASS_Exception("You\'re returning " + rightOutVar.dataType +
                                    " while function returns " + returnValue.DataType, -1);
                            }
                            result += rightOutStr;
                            if (rightOutVar.address.length != -1 || rightOutVar.address.address != "eax")
                            {
                                //We have to move our expression result to eax
                                if (memoryManager.IsTaken("eax"))
                                {
                                    //throw new ASS_Exception("TODO - Should we break or override?", -1);
                                }
                                if(rightOutVar.dataType is PT_Float32 && rightOutVar.address.IsFloatRegister())
                                {
                                    result += "\tmovss\teax,\t" + rightOutVar.ConvertToString() +
                                        AST_Program.separator_line;
                                }
                                else
                                {
                                    result += "\t  mov\teax,\t" + rightOutVar.ConvertToString() +
                                        AST_Program.separator_line;
                                }
                                memoryManager.SetFreeAddress(rightOutVar);
                                rightOutVar = new Variable(null, rightOutVar.dataType, new Address("eax", -1));
                                memoryManager.UpdateRegisterUsage(rightOutVar);
                            }

                            if (this.ast_function.expressions.Last.Value != exp)
                            {
                                usesReturns = true;
                                result += "\t  jmp\t." + name + "_return" + AST_Program.separator_line;
                            }
                            res = rightOutVar;
                        }
                        else if(input.exp_Type == Exp_Type.Operator && input.exp_Operator.init)
                        {
                            string rightOutStr = "";
                            Variable rightOutVar = null;
                            rightOutVar = ParseExpNode(exp, statement, input.right, out rightOutStr);
                            result += rightOutStr;

                            if (input.exp_Data == "-")
                            {
                                throw new NotImplementedException("Unary - is not yet supported");
                            }
                            else if (input.exp_Data == "+")
                            {
                                if (rightOutVar.dataType is PT_Ptr)
                                {
                                    rightOutVar.dataType = new PT_Ptr() { innerParameterType = new PT_Void() };
                                    res = rightOutVar;
                                }
                            }
                            else if (input.exp_Data == "&")
                            {
                                if(input.right.exp_Type != Exp_Type.Variable)
                                {
                                    throw new ASS_Exception(
                                        "Address-of works only with variables, not with " + input.right.exp_Data, -1);
                                }
                                Variable rightTempVar = memoryManager.GetFreeAddress(null,
                                    ParameterType.GetParameterType(rightOutVar.dataType.GetName() + "_ptr"));
                                if (rightOutVar.dataType is PT_Void)
                                {
                                    result += "\t  mov\t" + rightTempVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }
                                else
                                {
                                    result += "\t  lea\t" + rightTempVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }

                                res = rightTempVar;
                            }
                            else if (input.exp_Data == "*")
                            {
                                if (input.right.exp_Type != Exp_Type.Variable)
                                {
                                    throw new ASS_Exception(
                                        "Dereference works only with variables, not with " + input.right.exp_Data, -1);
                                }

                                string rightType = rightOutVar.dataType.GetName();
                                if (!rightType.EndsWith("_ptr"))
                                {
                                    throw new ASS_Exception(
                                        "Can\'t dereference data type " + rightOutVar.dataType.GetName(), -1);
                                }
                                if(rightOutVar.address.length != -1)
                                {
                                    Variable rightTempVar = memoryManager.GetFreeRegister(rightOutVar.dataType);
                                    result += "\t  mov\t" + rightTempVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                    rightOutVar = rightTempVar;
                                }

                                Variable resultVar = memoryManager.GetFreeRegister(
                                    ParameterType.GetParameterType(
                                        rightType.Remove(rightType.Length - "_ptr".Length)));

                                result += "\t  mov\t" + resultVar.address.ConvertToString() +
                                    ",\tdword ptr " + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);

                                res = resultVar;
                            }
                            else throw new NotImplementedException("Unary " + input.exp_Data + " is not supported");
                        }
                        else throw new NotImplementedException(input.exp_Data + " is not supported");
                    }
                    else if (input.left != null && input.right != null)
                    {
                        if(input.exp_Type == Exp_Type.Operator && input.exp_Data == ".")
                        {
                            //Firstly parse only for type checking
                            string leftOutStr = "";
                            Variable leftOutVar = null;
                            leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                            memoryManager.SetFreeAddress(leftOutVar);
                            //result += leftOutStr;

                            if(leftOutVar.dataType is PT_Struct)
                            {
                                if(input.right.exp_Type != Exp_Type.Variable)
                                {
                                    throw new ASS_Exception("Structure member should be a variable", -1);
                                }
                                //Parse structure member access
                                PT_Struct leftType = (PT_Struct)leftOutVar.dataType;
                                Structure structure = parent.sec3.structures.Find(
                                    a => a.ast_Type.Equals(leftType));
                                if (structure == null)
                                {
                                    throw new ASS_Exception("Couldn\'t find a struct with such parameter", -1);
                                }

                                leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                                memoryManager.SetFreeAddress(leftOutVar);
                                result += leftOutStr;
                                var rightOutVar = structure.FindByName(input.right.exp_Data);
                                Address newAddr = Address.Advance(
                                        leftOutVar.address, structure.GetMemberOffset(rightOutVar));
                                newAddr.isStructure = true;
                                res = new Variable(null, rightOutVar.dataType, newAddr);
                            }
                            else
                            {
                                if(input.left.exp_Type == Exp_Type.Operator && 
                                    (input.left.exp_Data == "."/* || input.left.exp_Data == "->"*/))
                                {
                                    //throw new ASS_Exception("Can\'t use chains of function injects", -1);
                                }
                                //Parse single-parameter function call
                                if (!(leftOutVar.dataType is PT_Ptr) && input.right.exp_Type == Exp_Type.Function)
                                {
                                    Exp_Node injected = InjectParameterInFunction(statement, input.left, input.right);
                                    string injectedStr = "";
                                    res = ParseExpNode(exp, statement, injected, out injectedStr);
                                    result += injectedStr;
                                }
                                else
                                {
                                    throw new ASS_Exception("Can\'t use dot operator for "
                                        + input.left.exp_Data + " and " + input.right.exp_Data, -1);
                                }
                            }
                        }
                        else if (input.exp_Type == Exp_Type.Operator && input.exp_Data == "->")
                        {
                            //Firstly parse only for type checking
                            string leftOutStr = "";
                            Variable leftOutVar = null;
                            leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                            memoryManager.SetFreeAddress(leftOutVar);
                            //result += leftOutStr;
                            if (leftOutVar.dataType is PT_Ptr)
                            {
                                if (((PT_Ptr)leftOutVar.dataType).innerParameterType is PT_Struct)
                                {
                                    if (input.right.exp_Type != Exp_Type.Variable)
                                    {
                                        throw new ASS_Exception("Structure member should be a variable", -1);
                                    }
                                    //Parse structure member access
                                    PT_Struct leftType = (PT_Struct)((PT_Ptr)leftOutVar.dataType).innerParameterType;
                                    Structure structure = parent.sec3.structures.Find(
                                        a => a.ast_Type.Equals(leftType));
                                    if (structure == null)
                                    {
                                        throw new ASS_Exception("Couldn\'t find a struct with such parameter", -1);
                                    }
                                    //Get original from this pointer
                                    //Exp_Node_New valPtr = new Exp_Node_New(
                                    //    null, (Exp_Node_New)input.left, null, "*", Exp_Type.Operator,
                                    //    Array.Find(AST_Expression.operators_ast,
                                    //    a => a.oper == "*" && a.isLeftToRight == false && a.operandCount == 1));

                                    //Move struct address to a register
                                    leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                                    result += leftOutStr;

                                    var leftTempVar = memoryManager.GetFreeRegister(leftOutVar.dataType);
                                    result += "\t  mov\t" + leftTempVar.ConvertToString() +
                                        ",\t" + leftOutVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(leftOutVar);
                                    leftOutVar = leftTempVar;

                                    var rightOutVar = structure.FindByName(input.right.exp_Data);
                                    Address newAddr = Address.Advance(
                                            leftOutVar.address, structure.GetMemberOffset(rightOutVar));
                                    newAddr.isStructure = true;
                                    res = new Variable(null, rightOutVar.dataType, newAddr);
                                }
                                else
                                {
                                    if (input.left.exp_Type == Exp_Type.Operator &&
                                        (/*input.left.exp_Data == "." || */input.left.exp_Data == "->"))
                                    {
                                        //throw new ASS_Exception("Can\'t use chains of function injects", -1);
                                    }
                                    //Parse single-parameter function call
                                    if (input.right.exp_Type == Exp_Type.Function)
                                    {
                                        Exp_Node injected = InjectParameterInFunction(
                                            statement, input.left, input.right);
                                        string injectedStr = "";
                                        res = ParseExpNode(exp, statement, injected, out injectedStr);
                                        result += injectedStr;
                                    }
                                    else
                                    {
                                        throw new ASS_Exception(
                                            "Can\'t use dot operator for " + input.right.exp_Data, -1);
                                    }
                                }
                            }
                            else throw new ASS_Exception("Can\'t use operator -> for non-pointer variables", -1);
                        }
                        else
                        {
                            if (input.exp_Operator.init && input.exp_Operator.oper == "=" &&
                                input.left.exp_Type != Exp_Type.Variable && !
                                (input.left.exp_Type == Exp_Type.Operator && 
                                (input.left.exp_Operator.oper == "." || input.left.exp_Operator.oper == "->")))
                            {
                                throw new ASS_Exception(
                                    "Can\'t parse expression because lValue is not a variable", -1);
                            }
                            //Parse left, then right, then operator
                            string leftOutStr = "";
                            Variable leftOutVar = null;
                            leftOutVar = ParseExpNode(exp, statement, input.left, out leftOutStr);
                            result += leftOutStr;

                            if (leftOutVar.address.IsConstant()/* || leftOutVar.address.IsGlobalVariableLabel()*/ ||
                                ((leftOutVar.address.IsStack() || leftOutVar.address.IsGlobal()) &&
                                (!input.exp_Operator.init || input.exp_Operator.oper != "=") &&
                                ((input.right.exp_Operator.init == true && input.right.exp_Operator.oper != "=")
                                /* TODO - check if right is on stack */ || input.exp_Data != "=")))
                            {
                                //We have to move our expression result to a register
                                Variable leftTempVar = leftOutVar;
                                if (leftTempVar.dataType is PT_Float32)
                                {
                                    if (leftOutVar.address.IsStack())
                                    {
                                        leftOutVar = memoryManager.GetFreeFloatReg((PT_Float32)leftTempVar.dataType);
                                        result += "\tmovss\t" + leftOutVar.ConvertToString() +
                                            ",\t" + leftTempVar.ConvertToString() + AST_Program.separator_line;
                                        memoryManager.SetFreeAddress(leftTempVar);
                                    }
                                    else
                                    {
                                        if (leftOutVar.address.IsConstant())
                                        {
                                            var leftTempVar1 = memoryManager.GetFreeRegister(leftOutVar.dataType);
                                            result += "\t  mov\t" + leftTempVar1.ConvertToString() +
                                                ",\t" + leftOutVar.ConvertToString() + AST_Program.separator_line;
                                            memoryManager.SetFreeAddress(leftOutVar);
                                            leftOutVar = leftTempVar1;
                                        }
                                        leftOutVar = memoryManager.GetFreeFloatReg((PT_Float32)leftTempVar.dataType);
                                        result += "\t movd\t" + leftOutVar.ConvertToString() +
                                            ",\t" + leftTempVar.ConvertToString() + AST_Program.separator_line;
                                        memoryManager.SetFreeAddress(leftTempVar);
                                    }
                                }
                                else
                                {
                                    if (input.exp_Type == Exp_Type.Operator && input.exp_Operator.init &&
                                        input.exp_Operator.oper == "/")
                                    {
                                        //It's an operator that requires eax
                                        leftOutVar = new Variable(null, leftOutVar.dataType, new Address("eax", -1));
                                        memoryManager.UpdateRegisterUsage(leftOutVar);
                                    }
                                    else
                                    {
                                        leftOutVar = memoryManager.GetFreeRegister(leftTempVar.dataType);
                                    }
                                    //memoryManager.UpdateRegisterUsage(leftOutVar);
                                    result += "\t  mov\t" + leftOutVar.ConvertToString() +
                                        ",\t" + leftTempVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(leftTempVar);
                                }
                            }

                            Variable rightOutVar = null;
                            string rightOutStr = "";
                            /*if ((input.right.exp_Type == Exp_Type.Operator && input.right.exp_Operator.init &&
                                input.right.exp_Operator.layer < input.exp_Operator.layer) ||
                                input.right.exp_Type == Exp_Type.Function ||
                                true)*/
                            {
                                rightOutVar = ParseExpNode(exp, statement, input.right, out rightOutStr);
                            }
                            result += rightOutStr;
                            //For constants, we can assign them to different int sizes
                            if (rightOutVar.address.length == -2 &&
                                leftOutVar.dataType.GetName().StartsWith("int") &&
                                rightOutVar.dataType.GetName().StartsWith("int") &&
                                !leftOutVar.dataType.GetName().EndsWith("_ptr") &&
                                !rightOutVar.dataType.GetName().EndsWith("_ptr"))
                            {
                                rightOutVar.dataType = leftOutVar.dataType;
                            }
                            if (!leftOutVar.dataType.Equals(rightOutVar.dataType))
                            {
                                throw new NotImplementedException("Can\'t perform " + input.exp_Data +
                                    " on " + leftOutVar.dataType + " and " + rightOutVar.dataType);
                            }
                            if (leftOutVar.address.IsGlobal() && rightOutVar.address.IsStack())
                            {
                                var rightTempVar = memoryManager.GetFreeRegister(rightOutVar.dataType);
                                result += "\t  mov\t" + rightTempVar.ConvertToString() +
                                    ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);
                                rightOutVar = rightTempVar;
                            }
                            //Floating-point operations work only on xmm, xmm/r32
                            //If it is a constant, move it to a register, than
                            if (rightOutVar.dataType is PT_Float32 && rightOutVar.address.IsConstant()
                                && input.exp_Data != "=")
                            {
                                var rightTempVar = memoryManager.GetFreeRegister(rightOutVar.dataType);
                                result += "\t  mov\t" + rightTempVar.ConvertToString() +
                                    ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);
                                rightOutVar = rightTempVar;
                            }
                            if(rightOutVar.dataType is PT_Float32 && !(
                                rightOutVar.address.IsFloatRegister() || rightOutVar.address.IsStack()
                                )
                                && input.exp_Data != "=")
                            {
                                var rightTempVar = memoryManager.GetFreeFloatReg((PT_Float32)rightOutVar.dataType);
                                result += "\t movd\t" + rightTempVar.ConvertToString() +
                                    ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);
                                rightOutVar = rightTempVar;
                            }

                            if (input.exp_Data == "+")
                            {
                                if (leftOutVar.dataType is PT_Float32)
                                {
                                    result += "\taddss\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeFloatReg(rightOutVar);
                                }
                                else
                                {
                                    result += "\t  add\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }
                            }
                            else if (input.exp_Data == "-")
                            {
                                if (leftOutVar.dataType is PT_Float32)
                                {
                                    result += "\tsubss\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeFloatReg(rightOutVar);
                                }
                                else
                                {
                                    result += "\t  sub\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }
                            }
                            else if (input.exp_Data == "*")
                            {
                                if (leftOutVar.dataType is PT_Float32)
                                {
                                    result += "\tmulss\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeFloatReg(rightOutVar);
                                }
                                else
                                {
                                    result += "\t imul\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }
                            }
                            else if (input.exp_Data == "/")
                            {
                                if (leftOutVar.dataType is PT_Float32)
                                {
                                    result += "\tdivss\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeFloatReg(rightOutVar);
                                }
                                else
                                {
                                    bool popAX = false;
                                    if (leftOutVar.address.address != "eax")
                                    {
                                        if (memoryManager.IsTaken("eax"))
                                        {
                                            popAX = true;
                                            result += "\t push\teax" + AST_Program.separator_line;
                                        }

                                        //It's an operator that requires eax, MOV it there
                                        result += "\t  mov\teax,\t" + leftOutVar.ConvertToString() +
                                            AST_Program.separator_line;
                                        memoryManager.SetFreeAddress(leftOutVar);
                                        leftOutVar = new Variable(null, leftOutVar.dataType, new Address("eax", -1));
                                        memoryManager.UpdateRegisterUsage(leftOutVar);
                                    }

                                    if (rightOutVar.address.length == -2 || rightOutVar.address.address == "edx")
                                    {
                                        //We have a constant, move to ebx or ecx
                                        var tempRightOutVar = rightOutVar;
                                        rightOutVar = memoryManager.GetFreeRegister(tempRightOutVar.dataType);
                                        result += "\t  mov\t" + rightOutVar.ConvertToString() +
                                            ",\t" + tempRightOutVar.ConvertToString() + AST_Program.separator_line;
                                        memoryManager.SetFreeAddress(tempRightOutVar);
                                    }

                                    //Now left variable is in eax
                                    //Push edx in case something uses it
                                    result += "\t push\tedx" + AST_Program.separator_line;
                                    //Zero-out EDX
                                    result += "\t  xor\tedx,\tedx" + AST_Program.separator_line;
                                    //memoryManager.usesDX = true;
                                    result += "\t idiv\t" + rightOutVar.ConvertToString() +
                                        AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                    //Pop edx in case something uses it
                                    result += "\t  pop\tedx" + AST_Program.separator_line;
                                    //EAX has the result
                                    if (popAX)
                                    {
                                        res = memoryManager.GetFreeRegister(leftOutVar.dataType);
                                        //We know for a fact that left operand is in eax
                                        memoryManager.SetFreeAddress(leftOutVar);
                                        result += "\t  mov\t" + res.ConvertToString() +
                                            ",\teax" + AST_Program.separator_line;
                                        result += "\t  pop\teax" + AST_Program.separator_line;
                                    }
                                    else
                                    {
                                        //We just roll with eax
                                        res = leftOutVar;
                                    }
                                }
                            }
                            else if (input.exp_Data == "%")
                            {
                                if (leftOutVar.dataType is PT_Float32)
                                {
                                    throw new ASS_Exception("Can\'t perform operator % on floating-point values", -1);
                                }
                                bool popAX = false;
                                if (leftOutVar.address.address != "eax")
                                {
                                    if (memoryManager.IsTaken("eax"))
                                    {
                                        popAX = true;
                                        result += "\t push\teax" + AST_Program.separator_line;
                                    }

                                    //It's an operator that requires eax, MOV it there
                                    result += "\t  mov\teax,\t" + leftOutVar.ConvertToString() +
                                        AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(leftOutVar);
                                    leftOutVar = new Variable(null, leftOutVar.dataType, new Address("eax", -1));
                                    memoryManager.UpdateRegisterUsage(leftOutVar);
                                }

                                if (rightOutVar.address.length == -2 || rightOutVar.address.address == "edx")
                                {
                                    //We have a constant, move to ebx or ecx
                                    var tempRightOutVar = rightOutVar;
                                    rightOutVar = memoryManager.GetFreeRegister(tempRightOutVar.dataType);
                                    result += "\t  mov\t" + rightOutVar.ConvertToString() +
                                        ",\t" + tempRightOutVar.ConvertToString() + AST_Program.separator_line;
                                    memoryManager.SetFreeAddress(tempRightOutVar);
                                }

                                //Now left variable is in eax
                                //Push edx in case something uses it
                                result += "\t push\tedx" + AST_Program.separator_line;
                                //Zero-out EDX
                                result += "\t  xor\tedx,\tedx" + AST_Program.separator_line;
                                //memoryManager.usesDX = true;
                                result += "\t idiv\t" + rightOutVar.ConvertToString() +
                                    AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);
                                //Result is in edx, MOV it somewhere
                                var tempLeftOutVar = memoryManager.GetFreeRegister(leftOutVar.dataType);
                                result += "\t  mov\t" + tempLeftOutVar.ConvertToString() +
                                    ",\tedx" + AST_Program.separator_line;
                                res = tempLeftOutVar;
                                memoryManager.SetFreeAddress(leftOutVar);
                                //Pop edx in case something uses it
                                result += "\t  pop\tedx" + AST_Program.separator_line;
                                //EAX has the result
                                if (popAX)
                                {
                                    result += "\t  pop\teax" + AST_Program.separator_line;
                                }
                            }
                            else if (input.exp_Data == "=")
                            {
                                if (input.left.exp_Type != Exp_Type.Variable && !
                                    (input.left.exp_Type == Exp_Type.Operator && 
                                    (input.left.exp_Operator.oper == "." || input.left.exp_Operator.oper == "->")))
                                {
                                    throw new ASS_Exception("Can\'t assign to " + input.left.exp_Type.ToString(),
                                        new ASS_Exception("Assignment is supported only for variables", -1), -1);
                                }
                                if (leftOutVar.dataType is PT_Float32 && !rightOutVar.address.IsConstant())
                                {
                                    result += "\t movd\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeFloatReg(rightOutVar);
                                }
                                else
                                {
                                    result += "\t  mov\t" + leftOutVar.ConvertToString() +
                                        ",\t" + rightOutVar.ConvertToString() + AST_Program.separator_line;
                                    res = leftOutVar;
                                    memoryManager.SetFreeAddress(rightOutVar);
                                }
                            }
                            else throw new NotImplementedException(input.exp_Data + " is not supported");
                        }
                    }
                    else throw new NotImplementedException(input.exp_Data + " is not supported");
                }
                else if (input.exp_Type == Exp_Type.Constant)
                {
                    if (Exp_Statement.GetDataType(input).GetName() == "lpcstr")
                    {
                        res = parent.sec3.DeclareString(input.exp_Data);
                    }
                    else
                    {
                        res = new Variable(null, Exp_Statement.GetDataType(input), new Address(input.exp_Data, -2));
                    }
                }
                else if (input.exp_Type == Exp_Type.Variable)
                {
                    Variable foundVar = locals.Find(a => a.ast_variable != null &&
                        a.ast_variable.Data == input.exp_Data);
                    if (foundVar == null)
                    {
                        //Search function parameters
                        foundVar = parameters.Find(a => a.ast_variable != null &&
                            a.ast_variable.Data == input.exp_Data);
                        if (foundVar == null)
                        {
                            //Search global variables
                            foundVar = parent.sec3.Find(a => a.ast_variable != null &&
                                a.address.address == input.exp_Data);
                            if (foundVar == null)
                            {
                                //Search functions
                                var func = parent.functions.Find(a => a.name == input.exp_Data);
                                if(func != null)
                                {
                                    func.isUsed = true;
                                    func.usedFrom.Add(this);
                                    foundVar = new Variable(null, ParameterType.GetParameterType("void"),
                                        new Address(input.exp_Data, -4));
                                }
                                if (foundVar == null)
                                {
                                    throw new ASS_Exception("Failed to parse variable usage",
                                        new ASS_Exception("Variable " + input.exp_Data + " was not found", -1), -1);
                                }
                            }
                        }
                    }
                    else
                    {
                        usesLocals = true;
                    }
                    res = foundVar;
                }
                else if (input.exp_Type == Exp_Type.Function)
                {
                    //TODO - check parameter types
                    string rightOutStringParams = "";
                    LinkedList<Exp_Node> fParameters = ToParameterList(exp, input.right, out rightOutStringParams);
                    string rightOutStringValues = "";
                    LinkedList<Variable> fVariables = PushParameters(exp, statement, 
                        fParameters, out rightOutStringValues);
                    string[] fParameterTypes = fVariables.Select(a => a.dataType.GetName()).Reverse().ToArray();
                    bool[] fParameterIntConstants = 
                        fVariables.Select(a => 
                        a.address.length == -2 &&
                        a.dataType.GetName().StartsWith("int") &&
                        !a.dataType.GetName().EndsWith("_ptr")
                        ).Reverse().ToArray();
                    //result += rightOutString;
                    AST_Function fUsed = null;
                    //1 for default libraries, 2 for salo functions
                    int location = 0;
                    //TODO - check for exact match
                    var fCorresponding = parent.defaultLibs.
                        Find(a => a.Includes(input.exp_Data, fParameterTypes, fParameterIntConstants))?.
                        Find(input.exp_Data, fParameterTypes, fParameterIntConstants);
                    if (fCorresponding != null)
                    {
                        //Mark library and function as used
                        fCorresponding.used = true;
                        fCorresponding.usedFrom.Add(this);
                        Library lib = parent.defaultLibs.Find(a => a.functions.IndexOf(fCorresponding) != -1);
                        lib.used = true;

                        fUsed = fCorresponding.ToASTFunction();
                        location = 1;
                    }
                    else
                    {
                        //TODO - implement better search
                        Function fLocal = parent.functions.Find(a =>
                        {
                            if (a.ast_function.accessLevel == AccessLevel.Private &&
                                a.ast_function.path != ast_function.path)
                                return false;
                            if (a.name != input.exp_Data) return false;
                            if (fVariables.Count == 0 &&
                                a.parameters.Count == 1 && a.parameters[0].dataType.GetName() == "void")
                                return true;
                            if (a.parameters.Count != fVariables.Count) return false;
                            for (int loop = 0; loop < a.parameters.Count; ++loop)
                            {
                                if (!a.parameters[loop].dataType.Equals(
                                    fVariables.ElementAt(fVariables.Count - loop - 1).dataType)
                                    && !(
                                    a.parameters[loop].dataType.GetName().StartsWith("int") &&
                                    !a.parameters[loop].dataType.GetName().EndsWith("_ptr") &&
                                    fVariables.ElementAt(fVariables.Count - loop - 1).dataType.
                                    GetName().StartsWith("int") &&
                                    !fVariables.ElementAt(fVariables.Count - loop - 1).dataType.
                                    GetName().EndsWith("_ptr") &&
                                    fVariables.ElementAt(fVariables.Count - loop - 1).address.length == -2))
                                    return false;
                            }
                            return true;
                        });
                        if (fLocal != null)
                        {
                            fUsed = fLocal.ast_function;
                            fLocal.usedFrom.Add(this);
                            fLocal.isUsed = true;
                            location = 2;
                        }
                        else
                        {
                            string paramList = "";
                            IEnumerable<Variable> fParReversed = fVariables.Reverse();
                            foreach (var par in fParReversed)
                            {
                                paramList += par.dataType.GetName() + ", ";
                            }
                            if (fVariables.Count > 0)
                                paramList = paramList.Remove(paramList.Length - 2);
                            throw new ASS_Exception(
                                "Function " + input.exp_Data + "(" + paramList +
                                ") was not found. Perhaps, it was private?", -1);
                        }
                    }
                    if (fUsed != null)
                    {
                        foreach(var p in fUsed.parameters)
                        {
                            if (p.DataType is PT_Struct)
                            {
                                throw new ASS_Exception("Failed to parse function call",
                                    new NotImplementedException("Can\'t pass struct " + p.DataType.GetName() +
                                    " to " + fUsed.name), -1);
                            }
                        }
                        //We are dealing with a library function
                        //if (fUsed.functionType == FunctionType.cdecl)
                        //{
                            bool popEAX = false;
                            if (memoryManager.IsTaken("eax"))
                            {
                                popEAX = true;
                                result += "\t push\teax" + AST_Program.separator_line;
                            }
                            bool callToSelf = false;// fUsed == this.ast_function;
                            if (callToSelf)
                            {
                                //result += "\tpushad" + AST_Program.separator_line;
                                result += "\t push\tebx" + AST_Program.separator_line;
                                result += "\t push\tecx" + AST_Program.separator_line;
                                result += "\t push\tedx" + AST_Program.separator_line;
                            }

                            //var el = fParameters.Last;
                            //while (el != null)
                            //{
                            //    result += "\t push\t" + el.Value.ConvertToString() +
                            //        AST_Program.separator_line;
                            //    //TODO - should we free the address?
                            //    memoryManager.SetFreeAddress(el.Value);
                            //    el = el.Previous;
                            //}
                            result += rightOutStringParams;
                            result += rightOutStringValues;
                            if (location == 1)
                            {
                                result += "\t call\t[" + input.exp_Data + "]" + AST_Program.separator_line;
                            }
                            else if (location == 2)
                            {
                                result += "\t call\t" + input.exp_Data + AST_Program.separator_line;
                            }
                            else throw new NotImplementedException("Location " + location + " is not supported");
                            //TODO - track eax to avoid memory leaks - they should happen here
                            //TODO - dump variables

                            //Shift stack pointer to hide pushed vars without dumping them
                            if(fUsed.functionType == FunctionType.cdecl && GetByteCount(fVariables) > 0)
                            {
                                result += "\t  add\tesp,\t" + GetByteCount(fVariables) + AST_Program.separator_line;
                            }
                            if (callToSelf)
                            {
                                //result += "\tpopad" + AST_Program.separator_line;
                                result += "\t  pop\tedx" + AST_Program.separator_line;
                                result += "\t  pop\tecx" + AST_Program.separator_line;
                                result += "\t  pop\tebx" + AST_Program.separator_line;
                            }

                            if (popEAX)
                            {
                                res = memoryManager.GetFreeAddress(null, fUsed.retValue.DataType);
                                result += "\t  mov\t" + res.ConvertToString() + ",\teax" +
                                    AST_Program.separator_line;
                                result += "\t  pop\teax" + AST_Program.separator_line;
                            }
                            else
                            {
                                //TODO - calculate return type
                                res = new Variable(
                                    null,
                                    fUsed.retValue.DataType,
                                    //ParameterType.GetParameterType("int32"),
                                    new Address("eax", -1));
                                memoryManager.UpdateRegisterUsage(res);
                            }
                        //}
                        /*
                        else if (fUsed.functionType == FunctionType.stdcall)
                        {
                            bool popEAX = false;
                            if (memoryManager.IsTaken("eax"))
                            {
                                popEAX = true;
                                result += "\t push\teax" + AST_Program.separator_line;
                            }

                            if (location == 1)
                            {
                                result += "\tinvoke\t" + fUsed.name;
                            }
                            else if (location == 2)
                            {
                                result += "\tstdcall\t" + fUsed.name;
                            }
                            else throw new NotImplementedException("Location " + location + " is not supported");

                            var el = fParameters.First;
                            while (el != null)
                            {
                                result += ",\t" + el.Value.ConvertToString();
                                memoryManager.SetFreeAddress(el.Value);
                                el = el.Next;
                            }
                            result += AST_Program.separator_line;

                            if (popEAX)
                            {
                                res = memoryManager.GetFreeAddress(null, fUsed.retValue.DataType);
                                result += "\t  mov\t" + res.ConvertToString() + ",\teax" +
                                    AST_Program.separator_line;
                                result += "\t  pop\teax" + AST_Program.separator_line;
                            }
                            else
                            {
                                //TODO - calculate return type
                                res = new Variable(
                                    null,
                                    fUsed.retValue.DataType,
                                    //ParameterType.GetParameterType("int32"),
                                    new Address("eax", -1));
                            }
                        }*/
                        //else
                        //{
                        //    throw new NotImplementedException(fUsed.functionType + " is not supported");
                        //}
                    }
                    else throw new ASS_Exception(input.exp_Data + " function was not found", -1);
                }
                else throw new NotImplementedException(input.exp_Type + " is not supported");
                resString = result;
                return res;
            }
            private Exp_Node InjectParameterInFunction(
                Exp_Statement statement, Exp_Node parameter, Exp_Node function)
            {
                if (function.exp_Type != Exp_Type.Function)
                    throw new ASS_Exception("Can\'t inject parameter not in function", -1);
                Exp_Node_New functionStart = (Exp_Node_New)function.right;
                var funcClone = function.Clone();
                if (functionStart == null)
                {
                    funcClone.SetRight(parameter);
                    return funcClone;
                }
                else
                {
                    while (functionStart.left != null) functionStart = (Exp_Node_New)functionStart.left;
                    Exp_Node_New leftParent = (Exp_Node_New)statement.FindParent(functionStart, function).Clone();
                    if (leftParent == null) throw new ASS_Exception("Failed to find parent of injectee function", -1);
                    Exp_Node_New newComma = new Exp_Node_New(
                        (Exp_Node_New)parameter,
                        functionStart,
                        null,
                        ",",
                        Exp_Type.Operator,
                        Array.Find(AST_Expression.operators_ast, a => a.oper == ","));
                    if (leftParent.left == functionStart) leftParent.SetLeft(newComma);
                    if (leftParent.right == functionStart) leftParent.SetRight(newComma);
                    return funcClone;
                }
            }
            private LinkedList<Variable> PushParameters(
                AST_Expression exp, Exp_Statement statement, LinkedList<Exp_Node> parameters, out string result)
            {
                string resString = "";
                LinkedList<Variable> res = new LinkedList<Variable>();
                var val = parameters.Last;
                while(val != null)
                {
                    string variableResult = "";
                    Variable variable = ParseExpNode(exp, statement, val.Value, out variableResult);
                    IParameterType parameterType = variable.dataType;
                    resString += variableResult;

                    if (variable.dataType.GetLengthInBytes() != 4/* &&
                        variable.address.length == -1 &&
                        variable.address.IsRegister()*/)
                    {
                        //We need to zero-extend the value
                        var reg = memoryManager.GetFreeRegister(ParameterType.GetParameterType("int32"));
                        resString += "\tmovzx\t" + reg.ConvertToString() + ",\t" +
                        variable.ConvertToString() + AST_Program.separator_line;
                        memoryManager.SetFreeAddress(variable);
                        variable = reg;
                    }
                    if (variable.dataType is PT_Float32 && variable.address.IsFloatRegister())
                    {
                        resString += "\t  sub\tesp,\t4" + AST_Program.separator_line;
                        resString += "\t movd\tdword ptr esp,\t" + variable.ConvertToString()
                            + AST_Program.separator_line;
                    }
                    else
                    {
                        resString += "\t push\t" + variable.ConvertToString() + AST_Program.separator_line;
                    }
                    memoryManager.SetFreeAddress(variable);
                    //TODO - It was probably a bad decision to re-set the data type
                    variable.dataType = parameterType;
                    res.AddLast(variable);
                    val = val.Previous;
                }
                result = resString;
                return res;
            }
            private LinkedList<Exp_Node> ToParameterList(AST_Expression exp, Exp_Node head, out string result)
            {
                LinkedList<Exp_Node> res = new LinkedList<Exp_Node>();
                if (head == null)
                {
                    result = "";
                    return res;
                }
                string resString = "";
                if (head.exp_Type != Exp_Type.Operator || head.exp_Data != ",")
                {
                    //Variable variable = ParseExpNode(exp, head, out result);

                    //if (variable.dataType.GetLengthInBytes() != 4 && 
                    //    variable.address.length == -1 && 
                    //    variable.address.IsRegister())
                    //{
                    //    //We need to zero-extend the value
                    //    var reg = memoryManager.GetFreeRegister(ParameterType.GetParameterType("int32"));
                    //    resString += "\tmovzx\t" + reg + ",\t" + 
                    //    variable.ConvertToString() + AST_Program.separator_line;
                    //    memoryManager.SetFreeAddress(variable);
                    //    variable = reg;
                    //}
                    result = "";
                    res.AddLast(head);
                    return res;
                }
                string resLeft = "";
                SALO_Core.Tools.ClassExtensions.AppendRange(res, ToParameterList(exp, head.left, out resLeft));
                string resRight = "";
                SALO_Core.Tools.ClassExtensions.AppendRange(res, ToParameterList(exp, head.right, out resRight));
                result = resString + resLeft + resRight;
                return res;
            }
        }
        public override void Parse(AST_Function input)
        {
            Function f = new Function(input, this);
            if (functions.Find(a => a.name == f.name) != null || defaultLibs.Find(a => a.Includes(f.name)) != null)
            {
                throw new AST_BadFormatException("Function " + f.name + " is already present", -1);
            }
            functions.Add(f);
        }

        public override void Parse(AST_Expression input)
        {
            throw new NotImplementedException();
        }

        public override void Parse(Exp_Statement exp)
        {
            throw new NotImplementedException();
        }

        public override void Parse(Exp_Node node)
        {
            throw new NotImplementedException();
        }
    }
}
