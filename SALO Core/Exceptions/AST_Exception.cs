﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Exceptions
{
	public class AST_Exception : SALO_Exception
	{
		public int CharIndex { get; private set; }
		public AST_Exception(int charIndex)
		{
			this.CharIndex = charIndex;
		}
		public AST_Exception(string message, int charIndex)
				: base(message)
		{
			this.CharIndex = charIndex;
		}
		public AST_Exception(string message, Exception inner, int charIndex)
				: base(message, inner)
		{
			this.CharIndex = charIndex;
		}
	}
}
