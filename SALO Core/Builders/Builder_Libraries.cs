using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SALO_Core.AST;
using SALO_Core.Exceptions;
using SALO_Core.Exceptions.ASS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
    public interface IParameterType : IEquatable<IParameterType>
    {
        int GetLengthInBytes();
        string GetName();
    }
    public class PT_Lpcstr : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            //It is a pointer to a byte
            return 1;
        }
        public string GetName()
        {
            return "lpcstr";
        }
    }
    public class PT_Int32 : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            return 4;
        }
        public string GetName()
        {
            return "int32";
        }
    }
    public class PT_Void : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            return 0;
        }
        public string GetName()
        {
            return "void";
        }
    }
    public class PT_None : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            return 0;
        }
        public string GetName()
        {
            return "none";
        }
    }
    public static class ParameterType
    {
        public static IParameterType GetParameterType(string input)
        {
            input = input.ToLower();
            switch (input)
            {
                case "lpcstr":
                    return new PT_Lpcstr();
                case "int32":
                    return new PT_Int32();
                case "void":
                    return new PT_Void();
                case "none":
                    return new PT_None();
                default:
                    throw new NotImplementedException(input + " is not yet supported");
            }
        }
    }
    public class Parameter
    {
        public string name { get; set; }
        [JsonConverter(typeof(ParameterTypeConverter))]
        public IParameterType type { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public string path { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SALO_Core.AST.FunctionType type { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SALO_Core.AST.AccessLevel access { get; set; }
        [JsonConverter(typeof(ParameterTypeConverter))]
        public IParameterType retVal { get; set; }
        public List<Parameter> parameters { get; set; }
        public bool used = false;
        private Library parent = null;
        public void SetParent(Library library) { parent = library; }
        public AST_Function ToASTFunction()
        {
            string code = "";
            //TODO - decide on default access level
            switch (access)
            {
                case AccessLevel.Private:
                    code += "private ";
                    break;
                case AccessLevel.Shared:
                    code += "shared ";
                    break;
                default:
                    code += "shared ";
                    break;
            }
            if(parent != null)
            {
                code += "\"" + parent.path + "\" ";
            }
            else
            {
                throw new ASS_Exception("Function " + name + " does not have a parent library", -1);
                //code += "\"" + path + "\" ";
            }
            code += "function " + path + AST_Program.separator_line;

            code += "takes" + AST_Program.separator_line;
            if (parameters.Count == 0)
            {
                code += "void ₴" + AST_Program.separator_line;
            }
            foreach (var inp in parameters)
            {
                code += inp.type.GetName() + " " + inp.name + " ₴" + AST_Program.separator_line;
            }
            code += "ends" + AST_Program.separator_line;

            code += "gives" + AST_Program.separator_line;
            code += retVal.GetName() + " ₴" + AST_Program.separator_line;
            code += "ends" + AST_Program.separator_line;

            code += "does" + AST_Program.separator_line;
            code += "ends" + AST_Program.separator_line;

            code += "ends " + name + AST_Program.separator_line;

            return new AST_Function(null, code, 0);
        }
    }

    public class Library
    {
        public string name { get; set; }
        public string path { get; set; }
        public List<Function> functions { get; set; }
        public bool used = false;
        //TODO - implement parameter check
        public bool Includes(string function)
        {
            return functions.Find(a => a.name == function) != null;
        }
        public bool Includes(string functionName, params string[] parameterTypes)
        {
            return functions.Find(a =>
            {
                if (a.name != functionName) return false;
                if (parameterTypes.Length != a.parameters.Count) return false;
                for (int i = 0; i < parameterTypes.Length; ++i)
                {
                    if (parameterTypes[i] != a.parameters[i].type.GetName()) return false;
                }
                return true;
            }) != null;
        }
        public Function Find(string functionName, params string[] parameterTypes)
        {
            return functions.Find(a =>
            {
                if (a.name != functionName) return false;
                if (parameterTypes.Length != a.parameters.Count) return false;
                for (int i = 0; i < parameterTypes.Length; ++i)
                {
                    if (parameterTypes[i] != a.parameters[i].type.GetName()) return false;
                }
                return true;
            });
        }
    }

    public class ParameterTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var paramString = (string)reader.Value;

            return ParameterType.GetParameterType(paramString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IParameterType parameter = (IParameterType)value;
            writer.WriteValue(parameter.GetName());
        }
    }
}

namespace SALO_Core.Builders
{
    public class Builder_Libraries
    {
        public List<CodeBlocks.Library> libraries { get; protected set; }
        public Builder_Libraries(string path)
        {

            libraries = new List<CodeBlocks.Library>();
            string directory = new FileInfo(path).Directory.FullName;
            string[] files = Directory.GetFiles(directory);
            foreach (string f in files)
            {
                if (!File.Exists(f))
                    throw new IOException("For some reason one of the observed library files is gone");
                FileInfo fileInfo = new FileInfo(f);
                if (fileInfo.Name.StartsWith("Library") && fileInfo.Extension.ToLower() == ".json")
                {
                    try
                    {
                        string res = File.ReadAllText(f);
                        var libraryList = JsonConvert.DeserializeObject<List<CodeBlocks.Library>>(res);
                        foreach(var l in libraryList)
                        {
                            foreach(var fu in l.functions)
                            {
                                fu.SetParent(l);
                            }
                        }
                        libraries.AddRange(libraryList);
                    }
                    catch (Exception e)
                    {
                        throw new SALO_Exception("Error parsing library files", e);
                    }
                }
            }
        }
    }
}
