using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.Exceptions;

namespace SALO_Core.AST
{
	public enum AST_Comment_Type
	{
		None,
		Single,
		Multi
	}
	public class AST_Comment : AST_Node
	{
		protected AST_Comment_Type comment_Type;
		public List<string> text { get; protected set; }
		public override void Parse(string input, int charIndex)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new AST_EmptyInputException("Provided string is empty", charIndex);
			string res = "";
			if (input.StartsWith("//"))
			{
				comment_Type = AST_Comment_Type.Single;
				res = input.Remove(0, 2);
				int endR = res.IndexOf('\r');
				int endN = res.IndexOf('\n');
				if (endR != -1 && endN != endR + 1)
				{
					throw new AST_BadFormatException(
						"\\r encountered without \\n", 
						new FormatException(), 
						charIndex + 2 + endR);
				}
				if (endR != -1)
				{
					res = res.Remove(endR);
				}
				else if (endN != -1)
				{
					res = res.Remove(endN);
				}
				text = new List<string>
				{
					res
				};
			}
			else if (input.StartsWith("/*"))
			{
				comment_Type = AST_Comment_Type.Multi;
				res = input.Remove(0, 2);
				if (res.EndsWith("*/"))
				{
					res = res.Remove(res.Length - 2);
				}
				text = new List<string>();
				text.AddRange(res.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
			}
			else
			{
				comment_Type = AST_Comment_Type.None;
				text = null;
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
			output += "Comment " + comment_Type.ToString() + "\r\n";
			if(text != null)
			{
				if(comment_Type == AST_Comment_Type.Single)
				{
					foreach (string s in text)
					{
						output += indent + "//" + s + "\r\n";
					}
				}
				else if(comment_Type == AST_Comment_Type.Multi)
				{
					output += indent + "/*\r\n";
					foreach (string s in text)
					{
						output += indent + s + "\r\n";
					}
					output += indent + "*/\r\n";
				}
			}
			if (childNodes != null)
			{
				for (LinkedListNode<AST_Node> ch = childNodes.First; ch != null; ch = ch.Next)
				{
					ch.Value.Print(indent, ch.Next == null, ref output);
				}
			}
		}
		public AST_Comment(AST_Node parent, string input, int charIndex) : base(parent, input, charIndex)
		{

		}
	}
}
