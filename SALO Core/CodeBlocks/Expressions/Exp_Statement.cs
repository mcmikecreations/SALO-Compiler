﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.Exceptions;

namespace SALO_Core.CodeBlocks.Expressions
{
    public class Exp_Statement
    {
        protected List<string> items;
        public Exp_Node head { get; protected set; }
        public Exp_Statement(List<string> list)
        {
            items = list;
            if (list == null)
                throw new AST_EmptyInputException(
                    "Expression is empty",
                    new NullReferenceException("Expression piece list is null"),
                    0);
            head = new Exp_Node_New(list, 0);
            while (Simplify(head)) ;
        }
        public void Print(string indent, bool last, ref string output)
        {
            head.Print(indent, last, ref output);
        }
        public void Accept(CB cb)
        {
            cb.Parse(this);
        }
        public bool Simplify(Exp_Node node)
        {
            if (node.left != null/* && node.left.exp_Type == Exp_Type.Operator*/)
            {
                while (Simplify(node.left)) ;
            }
            if (node.right != null/* && node.right.exp_Type == Exp_Type.Operator*/)
            {
                while (Simplify(node.right)) ;
            }

            if (node.exp_Type == Exp_Type.Operator)
            {
                if (node.left != null && node.right != null && 
                    node.left.exp_Type == Exp_Type.Constant && 
                    node.right.exp_Type == Exp_Type.Constant)
                {
                    var lType = GetDataType(node.left);
                    var rType = GetDataType(node.right);
                    var lValue = node.left.exp_Data;
                    var rValue = node.right.exp_Data;
                    if (!lType.Equals(rType)) return false;
                    //throw new NotImplementedException(
                    // "Operations between " + lType.GetName() + " and " + rType.GetName() +
                    // " are not suppported");

                    if (lType is PT_Void || lType is PT_None) return false;
                    var cl = ParameterType.Parse(lValue, (dynamic)lType);
                    var cr = ParameterType.Parse(rValue, (dynamic)rType);

                    if (node.exp_Operator.oper == "+")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl + cr).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "-")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl - cr).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "*")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl * cr).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "/")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl / cr).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "%")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl % cr).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "==")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl == cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "!=")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl != cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == ">")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl > cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "<")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl < cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == ">=")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl >= cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                    else if (node.exp_Operator.oper == "<=")
                    {
                        node.SetLeft(null);
                        node.SetRight(null);
                        node.SetData((cl <= cr ? 1 : 0).ToString());
                        node.SetType(Exp_Type.Constant);
                        node.SetOperator(new AST_Operator());
                        return true;
                    }
                }
            }
            return false;
        }
        public static IParameterType GetDataType(Exp_Node node)
        {
            if (node.exp_Type != Exp_Type.Constant) return ParameterType.GetParameterType("none");
            else if (!Exp_Node.isConstant(node.exp_Data))
            {
                return ParameterType.GetParameterType("none");
            }
            else
            {
                if (node.exp_Data[0] == '\"' && node.exp_Data[node.exp_Data.Length - 1] == '\"')
                {
                    return ParameterType.GetParameterType("lpcstr");
                }
                Int32 valInt32 = 0;
                if (Int32.TryParse(node.exp_Data, out valInt32))
                {
                    return ParameterType.GetParameterType("int32");
                }
                Int16 valInt16 = 0;
                if (Int16.TryParse(node.exp_Data, out valInt16))
                {
                    return ParameterType.GetParameterType("int16");
                }
                byte valInt8 = 0;
                if (byte.TryParse(node.exp_Data, out valInt8))
                {
                    return ParameterType.GetParameterType("int8");
                }
                bool valBool = false;
                if (bool.TryParse(node.exp_Data, out valBool))
                {
                    return ParameterType.GetParameterType("bool");
                }
                throw new NotImplementedException(node.exp_Data + " is not yet recognized");
            }
        }
    }
}
