using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.AST.Data
{
	public enum DataType
	{
		None,
		Void,
		Int32,
		Int8,
		Float32,
		Bool,
	}
	public abstract class AST_Data : AST_Node
	{
		protected DataType dataType;
		protected string data;
		public DataType DataType => dataType;
		public string Data => data;
		public AST_Data(AST_Node parent, string input) : base(parent, input)
		{

		}
	}
}
