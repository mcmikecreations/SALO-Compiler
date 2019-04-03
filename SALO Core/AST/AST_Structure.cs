using SALO_Core.AST.Data;
using SALO_Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST
{
    public class AST_Structure : AST_Node
    {
        public LinkedList<AST_Variable> variables;
        public string name { get; protected set; }
        public override void Parse(string input, int charIndex)
        {

        }

        public override void Print(string indent, bool last, ref string output)
        {
            output += indent;
            if (last)
            {
                output += "\\-";
                indent += "  ";
            }
            else
            {
                output += "|-";
                indent += "| ";
            }
            output += "Structure: " + name + "\r\n";
            if (variables != null)
            {
                output += indent + "Variables:" + "\r\n";
                for (LinkedListNode<AST_Variable> ch = variables.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
            if (childNodes != null)
            {
                output += indent + "Children??" + "\r\n";
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
                throw new AST_Exception("Function child nodes are not null, although it doesn't use them", -1);
            }
        }
        public AST_Structure(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
