using SALO_Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST.Data
{
    public class AST_GlobalVariable : AST_Node
    {
        public AST_Variable variable { get; protected set; }
        public AccessLevel accessLevel { get; protected set; }
        public string path { get; protected set; }
        public override void Parse(string input, int charIndex)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new AST_EmptyInputException("Provided string is empty", charIndex);

            int i = 0;
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            //Read to visibility attribute
            if (input.IndexOf("shared", i) == i)
            {
                accessLevel = AccessLevel.Shared;
                i += "shared".Length;
            }
            else if (input.IndexOf("private", i) == i)
            {
                accessLevel = AccessLevel.Private;
                i += "private".Length;
            }
            else
            {
                accessLevel = AccessLevel.Private;
            }
            //Read path
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse global variable path",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input[i] != '\"')
                throw new AST_BadFormatException("Failed to parse global variable path",
                            new ArgumentException("Global variable path not found"), charIndex + i);
            int pathStart = i;
            ++i;
            while (input[i] != '\"' || input[i - 1] == '\\') ++i;
            path = input.Substring(pathStart + 1, i - pathStart - 1);
            ++i;

            //Read structure type
            //TODO - add more structure types
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse global variable type",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("global", i) == i)
            {
                i += "global".Length;
            }
            else
            {
                throw new AST_BadFormatException("Unknown global variable format", charIndex + i);
            }
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse global variable name",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            input = input.Substring(i);

            variable = new AST_Variable(this, input, charIndex + i);
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
            output += "Global variable:" + "\r\n";
            variable.Print(indent, true, ref output);
            
            if (childNodes != null)
            {
                output += indent + "Children??" + "\r\n";
                for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
                {
                    ch.Value.Print(indent, ch.Next == null, ref output);
                }
                throw new AST_Exception("Global variable child nodes are not null, although it doesn't use them", -1);
            }
        }
        public AST_GlobalVariable(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
