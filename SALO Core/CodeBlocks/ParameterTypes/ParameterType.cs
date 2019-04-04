using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
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
                case "int16":
                    return new PT_Int16();
                case "int8":
                    return new PT_Int8();
                case "void":
                    return new PT_Void();
                case "none":
                    return new PT_None();
                default:
                    if (input.EndsWith("_ptr"))
                    {
                        return new PT_Ptr()
                        {
                            innerParameterType = GetParameterType(input.Remove(input.Length - "_ptr".Length))
                        };
                    }
                    else if (input.EndsWith("_struct"))
                    {
                        var ast_structure = Builders.Builder_AST.structures.Find(
                            a => a.name == input.Remove(input.Length - "_struct".Length));
                        if (ast_structure == null)
                            throw new Exceptions.AST_BadFormatException(
                                "Structure " + input.Remove(input.Length - "_struct".Length) + " is not found",
                                -1);
                        return new PT_Struct()
                        {
                            children = ast_structure.variables.Select(a => a.DataType).ToList(),
                            name = ast_structure.name,
                        };
                    }
                    throw new NotImplementedException(input + " is not yet supported");
            }
        }

        public static Int32 Parse(string value, PT_Int32 pt)
        {
            return Int32.Parse(value);
        }
    }
    public interface IParameterType : IEquatable<IParameterType>
    {
        int GetLengthInBytes();
        string GetName();
    }
}
