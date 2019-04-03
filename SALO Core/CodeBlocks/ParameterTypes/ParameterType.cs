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
