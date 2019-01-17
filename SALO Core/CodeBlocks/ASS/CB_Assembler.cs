using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALO_Core.AST;
using SALO_Core.CodeBlocks.Expressions;
using SALO_Core.Exceptions;
using SALO_Core.Exceptions.ASS;

namespace SALO_Core.CodeBlocks
{
	public class CB_Assembler : CB
	{
		public CB_Assembler()
		{
		}

		public override void Parse(AST_Program input)
		{
			string header, footer;
			header = "format PE GUI 4.0\nentry main\n";
			footer = "";

			Result = header;
			
			foreach(AST_Node node in input.childNodes)
			{
				node.Accept(this);
			}

			Result += footer;
		}

		public override void Parse(AST_Comment input)
		{
			foreach (string s in input.text)
			{
				Result += "; " + s + "\n";
			}
		}

		public override void Parse(AST_Directive input)
		{
			if (input.childNodes == null || input.childNodes.Count < 1)
				throw new AST_BadFormatException("Input directive node has too few children", input.charIndex);
			if (input.childNodes.Count > 1)
				throw new AST_BadFormatException("Input directive node has too many children", input.charIndex);
			input.childNodes.First.Value.Accept(this);
		}

		public override void Parse(AST_Include input)
		{
			Result += "include \'" + input.file + "\'\n";
		}

		public override void Parse(AST_Define input)
		{
			if(input.token == null)
				throw new NotImplementedException();
			Result += input.identifier + " equ " + input.token + "\n";
		}

		public override void Parse(AST_Function input)
		{
			Result += input.name + ":\n";
			//Working with input parameters
			bool hasParams = false;
			if(input.parameters.Count > 0)
			{
				hasParams = true;
				Result += "\tpush \tebp\n";
				Result += "\tmov  \tebp,\tesp\n";
			}

            foreach(var exp in input.expressions)
            {
                exp.Accept(this);
            }

			if (hasParams)
			{
				Result += "\tmov  \tesp,\tebp\n";
				Result += "\tpop  \tebp\n";
			}
			Result += "\tret\n\n";
		}

        public override void Parse(AST_Expression input)
        {
            Exp exp = new Exp(input.nodes);
#if DEBUG
            string output = "";
            exp.Print("", false, ref output);
            Console.WriteLine(output);
#endif
        }
    }
}
