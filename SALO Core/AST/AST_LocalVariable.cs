﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
    public class AST_LocalVariable : AST_Expression
    {
        public string code { get; protected set; }
        public override void Parse(string input, int charIndex)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new AST_EmptyInputException("Provided string is empty", charIndex);
            childNodes = new LinkedList<AST_Node>();
            childNodes.AddLast(new Data.AST_Variable(this, input, charIndex));
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
            output += "Local variable:\r\n";

            if (childNodes != null)
            {
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
            }
        }

        public AST_LocalVariable(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
