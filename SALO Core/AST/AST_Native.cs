using SALO_Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST
{
    class AST_Native : AST_Expression
    {
        public string code { get; protected set; }
        public override void Parse(string input, int charIndex)
        {
            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
                throw new AST_EmptyInputException("Provided string is empty", charIndex);
            if (!input.StartsWith("native does") || !input.EndsWith("ends native"))
                throw new AST_EmptyInputException("Provided string is not a native code piece", charIndex);
            input = input.Remove(0, "native does".Length);
            input = input.Remove(input.Length - "ends native".Length);
            code = input;
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
            output += "Native:\r\n";
            string[] lines = code.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string s in lines)
            {
                output += indent + s + "\r\n";
            }

            if (childNodes != null)
            {
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
        }

        public AST_Native(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
