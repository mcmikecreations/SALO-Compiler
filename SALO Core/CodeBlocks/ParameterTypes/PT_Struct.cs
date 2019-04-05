using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
    public class PT_Struct : IParameterType
    {
        public List<IParameterType> children;
        public List<string> childNames;
        public string name;
        public bool Equals(IParameterType other)
        {
            if (!(other is PT_Struct)) return false;
            PT_Struct other_struct = (PT_Struct)other;
            if (other_struct.children == null && children != null) return false;
            if (other_struct.children != null && children == null) return false;
            if (other_struct.children == null && children == null) return true;
            if (other_struct.children.Count != children.Count) return false;
            for(int i=0;i<children.Count; i++)
            {
                if (!other_struct.children[i].Equals(children[i])) return false;
            }
            return true;
        }
        public int GetLengthInBytes()
        {
            return children.Select(a=>a.GetLengthInBytes()).Sum();
        }
        public string GetName()
        {
            return name + "_struct";
        }
    }
}
