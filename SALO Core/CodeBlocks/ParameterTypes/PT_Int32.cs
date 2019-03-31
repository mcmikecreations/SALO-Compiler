using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
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
}
