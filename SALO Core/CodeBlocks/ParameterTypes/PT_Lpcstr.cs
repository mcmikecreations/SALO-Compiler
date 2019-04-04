using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
    public class PT_Lpcstr : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (other is PT_Lpcstr &&
                    GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            //It is a pointer to a byte
            return 4;
        }
        public string GetName()
        {
            return "lpcstr";
        }
    }
}
