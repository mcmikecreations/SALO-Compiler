using System;
using System.Collections.Generic;
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
        public static string ToDataTypeLabel(IParameterType parameterType)
        {
            switch (parameterType.GetName())
            {
                case "void":
                case "none":
                    throw new ASS_Exception("Error converting parameter type to an assembler label",
                        new ASS_Exception("Empty parameter types can\'t be converted to an assembler label", -1), -1);
                case "int32":
                    return "dd";
                case "int16":
                    return "dw";
                case "lpcstr":
                case "int8":
                    return "db";
                default:
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
                return ToString(false);
            }
            public string ToString(bool unfoldPATH)
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
                        result += "include \'" + str + "\'" + AST_Program.separator_line;
                    }
                    else
                    {
                        result += "include \'" + t + "\'" + AST_Program.separator_line;
                    }
                }
                return result;
            }
        }
        protected class Data : Section<Variable>
        {
            public Data(string attributes) : base(attributes)
            {

            }
            public Data() : base("data readable writeable")
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
                byte[] b = Encoding.UTF8.GetBytes(new char[] { retVal });
                return b[0];
            }
            public Variable DeclareString(string input)
            {
                if (input[0] != '\"' || input[input.Length - 1] != '\"')
                    throw new ASS_Exception("Can\'t use a broken string literal", -1);
                input = input.Substring(1, input.Length - 2);
                string parsedInput = "";
                byte[] byteData = Encoding.UTF8.GetBytes(input.ToCharArray());
                //TODO - make better conversion
                for (int i = 0; i < byteData.Length; ++i)
                {
                    if (i < input.Length - 1 && IsSpecialSymbol(input[i + 1], input[i]))
                    {
                        //We have a special character
                        if (parsedInput.Length > 0 && parsedInput[parsedInput.Length - 1] == ',')
                        {
                            parsedInput += ToSpecialSymbol(input[i + 1], input[i]).ToString() + ",";
                        }
                        else if (parsedInput.Length == 0)
                        {
                            parsedInput += ToSpecialSymbol(input[i + 1], input[i]).ToString() + ",";
                        }
                        else
                        {
                            //We have a letter
                            parsedInput += "\"," + ToSpecialSymbol(input[i + 1], input[i]).ToString() + ",";
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
                    new Address("lpcstr" + values.Count.ToString(), -3));
                values.Add(v);
                return v;
            }
            public Variable Find(Predicate<Variable> predicate) => values.Find(predicate);
            public override string ConvertToString()
            {
                string result = "";
                if (values.Count > 0)
                {
                    result += "section \'.data\' " + attributes + AST_Program.separator_line;
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

                return result;
            }
        }
        protected class IData : Section<Library>
        {
            public IData(string attributes) : base(attributes)
            {

            }
            public IData() : base("data import readable")
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
                        result += "import " + t.name + ",\\" + AST_Program.separator_line;
                        foreach (var function in t.functions)
                        {
                            if (!function.used) continue;
                            result += function.name + ",\'" + function.path + "\'";
                            if (function != t.functions[t.functions.Count - 1])
                            {
                                result += ",\\";
                            }
                            result += AST_Program.separator_line;
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
            public Code() : base("code readable executable")
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
                return result;
            }
        }

        protected Format sec1;
        protected Include sec2;
        protected Data sec3;
        protected Code sec4;
        protected IData sec5;
        protected bool convertTo32;
        public List<Library> defaultLibs;
        public List<Function> functions;

        public CB_Assembler_New(bool convertTo32, List<CodeBlocks.Library> libraries)
        {
            this.convertTo32 = convertTo32;
            this.defaultLibs = libraries;
            this.functions = new List<Function>();
            sec1 = new Format();
            sec2 = new Include();
            sec3 = new Data();
            sec4 = new Code();
            sec5 = new IData();

            sec2.AddValue("%INCLUDE%/win32a.inc");
            Library kernel = defaultLibs.Find(a => a.name == "kernel32");
            kernel.used = true;
            CodeBlocks.Function exitProcess = kernel.functions.Find(a => a.name == "ExitProcess");
            exitProcess.used = true;
            //sec5.AddValue(kernel);
            //sec5.AddValue(msvcrt);
        }
        public override string GetResult()
        {
            foreach (var f in functions)
            {
                sec4.AddValue(f.ConvertToString());
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
        public class Address
        {
            public string address;
            public int offset;
            /// <summary>
            /// -1 for register, -2 for constant, -3 for global variable labels
            /// </summary>
            public int length;
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
            public string ConvertToString()
            {
                return GetPtr(length);
            }
            public string GetPtr(int length)
            {
                switch (length)
                {
                    case -3:
                        return address;
                    case -2:
                        return address;
                    case -1:
                        return address;
                    case 1:
                        return "byte ptr " + address +  (offset < 0 ? "-" + (-offset).ToString() :
                            "+" + offset.ToString());
                    case 2:
                        return "word ptr " + address + (offset < 0 ? "-" + (-offset).ToString() :
                            "+" + offset.ToString());
                    default:
                        return "dword ptr " + address + (offset < 0 ? "-" + (-offset).ToString() :
                            "+" + offset.ToString());
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
                throw new NotImplementedException("Register " + address + " is not supported");
            }
            public bool IsConstant()
            {
                if (length == -2) return true;
                return false;
            }
            public bool IsGlobalVariableLabel()
            {
                if (length == -3) return true;
                return false;
            }
            public bool IsStack()
            {
                if (IsConstant()) return false;
                if (IsGlobalVariableLabel()) return false;
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
        }
        public class MemoryManager
        {
            private bool axTaken = false, bxTaken = false, cxTaken = false, dxTaken = false;
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
                if (newVar.address.address == "ebx") bxTaken = true;
                if (newVar.address.address == "ecx") cxTaken = true;
                if (newVar.address.address == "edx") dxTaken = true;
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
                    return new Variable(null, dataType, new Address("ebx", -1));
                }
                else if (!cxTaken)
                {
                    cxTaken = true;
                    return new Variable(null, dataType, new Address("ecx", -1));
                }
                else if (!dxTaken)
                {
                    dxTaken = true;
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
                    }
                    else if (cxTaken == false)
                    {
                        reg = "ecx";
                        cxTaken = true;
                    }
                    else if (dxTaken == false)
                    {
                        reg = "edx";
                        dxTaken = true;
                    }
                    else throw new NotImplementedException("No more registers left");
                    return new Variable(null, dataType, new Address(reg, -1));
                }
                else throw new NotImplementedException(
                    dataType.ToString() + " is not yet supported for registry assignment");
            }
            public void SetFreeAddress(Variable oldVariable)
            {
                if (oldVariable.address.length == -1)
                {
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
                else
                {
                    //Check for function parameters
                    if (stack.Count == 0 &&
                        oldVariable.address.address == "ebp"/* &&
                        oldVariable.address.offset > 0*/) return;
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
            public Function(AST_Function ast_function, CB_Assembler_New parent)
            {
                this.parent = parent;
                this.name = ast_function.name;
                this.ast_function = ast_function;
                this.parameters = new List<Variable>();
                this.locals = new List<Variable>();
                this.accessLevel = ast_function.accessLevel;
                //TODO - use function type
                this.functionType = ast_function.functionType;
                this.returnValue = ast_function.retValue;
                start = end = "";

                int addr = 4;
                for (int i = ast_function.parameters.Count - 1; i >= 0; --i)
                {
                    var p = ast_function.parameters.ElementAt(i);
                    addr += p.DataType.GetLengthInBytes();
                    this.parameters.Add(new Variable(p, p.DataType, new Address("ebp", p.DataType, addr)));
                }
                if ((ast_function.parameters != null && ast_function.parameters.Count > 0 &&
                !(ast_function.parameters.Count == 1 &&
                  ast_function.parameters.First.Value.DataType.GetName() == "void"))
                || (ast_function.locals != null && ast_function.locals.Count > 0))
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
                        end += "  ret " + GetByteCount(parameters).ToString() + AST_Program.separator_line;
                    }
                    else throw new NotImplementedException(functionType + " is not supported");
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
                              " as " + t.address.ConvertToString() +
                              AST_Program.separator_line;
                }
                result += name + ":" + AST_Program.separator_line;
                result += start;
                string tempResult = "";

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
                if(locals.Count > 0 && usesLocals)
                {
                    int ceilingInput = GetByteCount(locals);
                    int ceilingResult = ((ceilingInput / 16) + (ceilingInput % 16 == 0 ? 0 : 1)) * 16;
                    result += "\t  sub\tesp,\t" + ceilingResult.ToString() + AST_Program.separator_line;
                }
                result += tempResult;
                if (usesReturns)
                {
                    result += "." + name + "_return:" + AST_Program.separator_line;
                }
                result += end;
                return result;
            }
            private void ParseLogic (AST_Logic input, out string resString)
            {
                resString = "";
            }
            private void ParseNative(AST_Native input, out string resString)
            {
                resString = input.code + AST_Program.separator_line;
            }
            private void ParseLocal(AST_LocalVariable input, out string resString)
            {
                AST_Variable variable = (AST_Variable)input.childNodes.First.Value;
                int addr = 0;
                if(locals.Count > 0)
                {
                    addr = locals[locals.Count - 1].address.offset;
                }
                addr -= variable.DataType.GetLengthInBytes();
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
                resString = "";
                if ((exp.head.exp_Type == Exp_Type.Operator && exp.head.exp_Operator.init &&
                    (exp.head.exp_Data == "=" || exp.head.exp_Data == "return")) ||
                    (exp.head.exp_Type == Exp_Type.Function))
                {
                    return ParseExpNode(input, exp.head, out resString);
                }
                else throw new ASS_Exception("Bad expression format",
                    new ASS_Exception("Statement should be assignment or a function call", -1), -1);
                //return null;
            }
            private Variable ParseExpNode(AST_Expression exp, Exp_Node input, out string resString)
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
                            rightOutVar = ParseExpNode(exp, input.right, out rightOutStr);
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
                                result += "\t  mov\teax,\t" + rightOutVar.address.ConvertToString() +
                                    AST_Program.separator_line;
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
                            rightOutVar = ParseExpNode(exp, input.right, out rightOutStr);
                            result += rightOutStr;

                            if (input.exp_Data == "&")
                            {
                                Variable rightTempVar = memoryManager.GetFreeAddress(null,
                                    ParameterType.GetParameterType("int32"));

                                result += "\t  lea\t" + rightTempVar.address.ConvertToString() +
                                    ",\t" + rightOutVar.address.ConvertToString() + AST_Program.separator_line;
                                memoryManager.SetFreeAddress(rightOutVar);

                                res = rightTempVar;
                            }
                            else throw new NotImplementedException(input.exp_Data + " is not supported");
                        }
                        else throw new NotImplementedException(input.exp_Data + " is not supported");
                    }
                    else if (input.left != null && input.right != null)
                    {
                        //Parse left, then right, then operator
                        string leftOutStr = "";
                        Variable leftOutVar = null;
                        leftOutVar = ParseExpNode(exp, input.left, out leftOutStr);
                        result += leftOutStr;

                        Variable rightOutVar = null;
                        string rightOutStr = "";
                        if ((input.right.exp_Type == Exp_Type.Operator && input.right.exp_Operator.init &&
                            input.right.exp_Operator.layer < input.exp_Operator.layer) ||
                            input.right.exp_Type == Exp_Type.Function ||
                            true)
                        {
                            rightOutVar = ParseExpNode(exp, input.right, out rightOutStr);
                        }
                        if (leftOutVar.address.IsConstant() || leftOutVar.address.IsGlobalVariableLabel() ||
                            (leftOutVar.address.IsStack() && 
                            (rightOutVar.address.IsStack() || input.exp_Data != "=")))
                        {
                            //We have to move our expression result to eax
                            Variable leftTempVar = leftOutVar;
                            leftOutVar = memoryManager.GetFreeRegister(leftTempVar.dataType);
                            //memoryManager.UpdateRegisterUsage(leftOutVar);
                            result += "\t  mov\t" + leftOutVar.address.ConvertToString() +
                                ",\t" + leftTempVar.address.ConvertToString() + AST_Program.separator_line;
                            memoryManager.SetFreeAddress(leftTempVar);
                        }
                        result += rightOutStr;
                        if (!leftOutVar.dataType.Equals(rightOutVar.dataType))
                        {
                            throw new NotImplementedException("Can\'t perform " + input.exp_Data +
                                " on " + leftOutVar.dataType + " and " + rightOutVar.dataType);
                        }

                        if (input.exp_Data == "+")
                        {
                            result += "\t  add\t" + leftOutVar.address.ConvertToString() +
                                ",\t" + rightOutVar.address.ConvertToString() + AST_Program.separator_line;
                            res = leftOutVar;
                            memoryManager.SetFreeAddress(rightOutVar);
                        }
                        else if (input.exp_Data == "-")
                        {
                            result += "\t  sub\t" + leftOutVar.address.ConvertToString() +
                                ",\t" + rightOutVar.address.ConvertToString() + AST_Program.separator_line;
                            res = leftOutVar;
                            memoryManager.SetFreeAddress(rightOutVar);
                        }
                        else if (input.exp_Data == "=")
                        {
                            if (input.left.exp_Type != Exp_Type.Variable)
                            {
                                throw new ASS_Exception("Can\'t assign to " + input.left.exp_Type.ToString(),
                                    new ASS_Exception("Assignment is supported only for variables", -1), -1);
                            }
                            result += "\t  mov\t" + leftOutVar.address.ConvertToString() +
                                ",\t" + rightOutVar.address.ConvertToString() + AST_Program.separator_line;
                            res = leftOutVar;
                            memoryManager.SetFreeAddress(rightOutVar);
                        }
                        else throw new NotImplementedException(input.exp_Data + " is not supported");
                    }
                    else throw new NotImplementedException(input.exp_Data + " is not supported");
                }
                else if (input.exp_Type == Exp_Type.Constant)
                {
                    if (GetDataType(input).GetName() == "lpcstr")
                    {
                        res = parent.sec3.DeclareString(input.exp_Data);
                    }
                    else
                    {
                        res = new Variable(null, GetDataType(input), new Address(input.exp_Data, -2));
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
                                throw new ASS_Exception("Failed to parse variable usage",
                                    new ASS_Exception("Variable " + input.exp_Data + " was not found", -1), -1);
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
                    string rightOutString = "";
                    LinkedList<Variable> fParameters = ToParameterList(exp, input.right, out rightOutString);
                    string[] fParameterTypes = fParameters.Select(a => a.dataType.GetName()).ToArray();
                    result += rightOutString;
                    AST_Function fUsed = null;
                    //1 for default libraries, 2 for salo functions
                    int location = 0;
                    //TODO - check for exact match
                    var fCorresponding = parent.defaultLibs.Find(a => a.Includes(input.exp_Data, fParameterTypes))?.
                        Find(input.exp_Data, fParameterTypes);
                    if (fCorresponding != null)
                    {
                        //Mark library and function as used
                        fCorresponding.used = true;
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
                            if (fParameters.Count == 0 &&
                                a.parameters.Count == 1 && a.parameters[0].dataType.GetName() == "void")
                                return true;
                            if (a.parameters.Count != fParameters.Count) return false;
                            for (int loop = 0; loop < a.parameters.Count; ++loop)
                            {
                                if (!a.parameters[loop].dataType.Equals(fParameters.ElementAt(loop).dataType))
                                    return false;
                            }
                            return true;
                        });
                        if (fLocal != null)
                        {
                            fUsed = fLocal.ast_function;
                            location = 2;
                        }
                        else
                        {
                            string paramList = "";
                            foreach (var par in fParameters)
                            {
                                paramList += par.dataType.GetName() + ", ";
                            }
                            if (fParameters.Count > 0)
                                paramList = paramList.Remove(paramList.Length - 2);
                            throw new ASS_Exception(
                                "Function " + input.exp_Data + "(" + paramList +
                                ") was not found. Perhaps, it was private?", -1);
                        }
                    }
                    if (fUsed != null)
                    {
                        //We are dealing with a library function
                        if (fUsed.functionType == FunctionType.cdecl)
                        {
                            bool popEAX = false;
                            if (memoryManager.IsTaken("eax"))
                            {
                                popEAX = true;
                                result += "\t push\teax" + AST_Program.separator_line;
                            }

                            var el = fParameters.Last;
                            while (el != null)
                            {
                                result += "\t push\t" + el.Value.address.ConvertToString() +
                                    AST_Program.separator_line;
                                //TODO - should we free the address?
                                memoryManager.SetFreeAddress(el.Value);
                                el = el.Previous;
                            }
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
                            result += "\t  add\tesp,\t" + GetByteCount(fParameters) + AST_Program.separator_line;

                            if (popEAX)
                            {
                                res = memoryManager.GetFreeAddress(null, fUsed.retValue.DataType);
                                result += "\t  mov\t" + res.address.ConvertToString() + ",\teax" +
                                    AST_Program.separator_line;
                                result += "\t  pop\teax" + AST_Program.separator_line;
                            }
                            else
                            {
                                //TODO - calculate return type
                                res = new Variable(null, ParameterType.GetParameterType("int32"),
                                    new Address("eax", -1));
                                memoryManager.UpdateRegisterUsage(res);
                            }
                        }
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
                                result += ",\t" + el.Value.address.ConvertToString();
                                memoryManager.SetFreeAddress(el.Value);
                                el = el.Next;
                            }
                            result += AST_Program.separator_line;

                            if (popEAX)
                            {
                                res = memoryManager.GetFreeAddress(null, fUsed.retValue.DataType);
                                result += "\t  mov\t" + res.address.ConvertToString() + ",\teax" +
                                    AST_Program.separator_line;
                                result += "\t  pop\teax" + AST_Program.separator_line;
                            }
                            else
                            {
                                //TODO - calculate return type
                                res = new Variable(
                                    null,
                                    ParameterType.GetParameterType("int32"),
                                    new Address("eax", -1));
                            }
                        }
                        else
                        {
                            throw new NotImplementedException(fUsed.functionType + " is not supported");
                        }
                    }
                    else throw new ASS_Exception(input.exp_Data + " function was not found", -1);
                }
                else throw new NotImplementedException(input.exp_Type + " is not supported");
                resString = result;
                return res;
            }
            private LinkedList<Variable> ToParameterList(AST_Expression exp, Exp_Node head, out string result)
            {
                LinkedList<Variable> res = new LinkedList<Variable>();
                if (head == null)
                {
                    result = "";
                    return res;
                }
                if (head.exp_Type != Exp_Type.Operator || head.exp_Data != ",")
                {
                    res.AddLast(ParseExpNode(exp, head, out result));
                    return res;
                }
                string resString = "";
                SALO_Core.Tools.Translitor.AppendRange(res, ToParameterList(exp, head.left, out resString));
                SALO_Core.Tools.Translitor.AppendRange(res, ToParameterList(exp, head.right, out resString));
                result = resString;
                return res;
            }
            static IParameterType GetDataType(Exp_Node node)
            {
                if (node.exp_Type != Exp_Type.Constant) return ParameterType.GetParameterType("none");
                else if (!Exp_Node.isConstant(node.exp_Data))
                {
                    return ParameterType.GetParameterType("none");
                }
                else
                {
                    if (node.exp_Data[0] == '\"' && node.exp_Data[node.exp_Data.Length - 1] == '\"')
                    {
                        return ParameterType.GetParameterType("lpcstr");
                    }
                    Int32 valInt32 = 0;
                    if (Int32.TryParse(node.exp_Data, out valInt32))
                    {
                        return ParameterType.GetParameterType("int32");
                    }
                    Int16 valInt16 = 0;
                    if (Int16.TryParse(node.exp_Data, out valInt16))
                    {
                        return ParameterType.GetParameterType("int16");
                    }
                    byte valInt8 = 0;
                    if (byte.TryParse(node.exp_Data, out valInt8))
                    {
                        return ParameterType.GetParameterType("int8");
                    }
                    bool valBool = false;
                    if (bool.TryParse(node.exp_Data, out valBool))
                    {
                        return ParameterType.GetParameterType("bool");
                    }
                    throw new NotImplementedException(node.exp_Data + " is not yet recognized");
                }
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
