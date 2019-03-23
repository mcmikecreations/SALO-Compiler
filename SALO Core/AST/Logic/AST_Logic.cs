using SALO_Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST.Logic
{
    public  abstract class AST_Logic : AST_Expression
    {
        protected LinkedList<AST_Expression> expressions;
        public AST_Logic(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
