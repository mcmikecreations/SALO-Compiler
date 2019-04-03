using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
    class PT_Ptr : IParameterType
    {
        public IParameterType innerParameterType { get; set; }
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
            return innerParameterType.GetName() + "_ptr";
        }
    }
}
