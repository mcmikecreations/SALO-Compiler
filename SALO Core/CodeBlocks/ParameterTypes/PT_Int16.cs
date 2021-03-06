﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.CodeBlocks
{
    public class PT_Int16 : IParameterType
    {
        public bool Equals(IParameterType other)
        {
            return (other is PT_Int16 &&
                    GetLengthInBytes() == other.GetLengthInBytes() &&
                    GetName() == other.GetName());
        }
        public int GetLengthInBytes()
        {
            return 2;
        }
        public string GetName()
        {
            return "int16";
        }
    }
}
