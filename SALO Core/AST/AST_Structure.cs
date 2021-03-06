﻿using SALO_Core.AST.Data;
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
        public LinkedList<AST_Variable> variables { get; protected set; }
        public AccessLevel accessLevel { get; protected set; }
        public string name { get; protected set; }
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
                throw new AST_BadFormatException("Failed to parse structure path",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input[i] != '\"')
                throw new AST_BadFormatException("Failed to parse structure path",
                            new ArgumentException("Function path not found"), charIndex + i);
            int pathStart = i;
            ++i;
            while (input[i] != '\"' || input[i - 1] == '\\') ++i;
            path = input.Substring(pathStart + 1, i - pathStart - 1);
            ++i;

            //Read structure type
            //TODO - add more structure types
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse structure type",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (input.IndexOf("structure", i) == i)
            {
                i += "structure".Length;
            }
            else
            {
                throw new AST_BadFormatException("Unknown structure format", charIndex + i);
            }
            while (i < input.Length && AST_Program.separator_ast.Contains(input[i])) ++i;
            if (i >= input.Length)
                throw new AST_BadFormatException("Failed to parse structure name",
                            new ArgumentOutOfRangeException("input", "Reached the end of input"), charIndex + input.Length - 1);
            if (!(char.IsLetter(input[i]) || AST_Expression.naming_ast.Contains(input[i])))
                throw new AST_BadFormatException("Structure name not allowed",
                            new FormatException("Structure name should start with a letter or " + AST_Expression.naming_ast), charIndex + i);
            string nm = "";
            while (char.IsLetterOrDigit(input[i]) || AST_Expression.naming_ast.Contains(input[i]))
            {
                nm += input[i];
                ++i;
                if (i >= input.Length) break;
            }
            name = nm;
            input = input.Substring(i);

            string[] variableStrings = input.Split(';');
            if (variableStrings == null || variableStrings.Length == 0)
                throw new AST_EmptyInputException("Provided string does not contain any variable", charIndex);
            variables = new LinkedList<AST_Variable>();
            foreach (string s in variableStrings)
            {
                //TODO - fix variable charIndex
                if (!string.IsNullOrWhiteSpace(s))
                    variables.AddLast(new AST_Variable(this, s, charIndex + i));
            }
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
                throw new AST_Exception("Structure child nodes are not null, although it doesn't use them", -1);
            }
        }
        public AST_Structure(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
        {

        }
    }
}
