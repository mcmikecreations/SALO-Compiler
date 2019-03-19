using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SALO_Core.AST;
using SALO_Core.Exceptions;

namespace SALO_Core.CodeBlocks.Expressions
{
    public class Exp_Node_New : Exp_Node
    {
        private struct Exp_Piece
        {
            public string inStrg;
            public Exp_Node inNode;
            public int indexStart, indexEnd;
            public bool isString;
        }
        public Exp_Node_New(Exp_Node_New left, Exp_Node_New right, 
                            List<string> input, 
                            string exp_Data, Exp_Type exp_Type)
        {
            this.left = left;
            this.right = right;
            this.input = input;
            this.exp_Data = exp_Data;
            this.exp_Type = exp_Type;
        }
        private Exp_Node_New(List<Exp_Piece> input, int charInd)
        {
            //TODO - fix charInd references and broken List<string> input passes
            this.input = null;
            if (input.Count == 1)
            {
                if (input[0].isString)
                {
                    if (isConstant(input[0].inStrg))
                    {
                        exp_Type = Exp_Type.Constant;
                        exp_Data = input[0].inStrg;
                        left = null;
                        right = null;
                    }
                    else if (isVariable(input[0].inStrg))
                    {
                        exp_Type = Exp_Type.Variable;
                        exp_Data = input[0].inStrg;
                        left = null;
                        right = null;
                    }
                    else if (isEmptyOperation(input[0].inStrg, out AST_Operator @operator))
                    {
                        exp_Type = Exp_Type.Operator;
                        exp_Data = input[0].inStrg;
                        exp_Operator = @operator;
                        left = null;
                        right = null;
                    }
                    else throw new AST_BadFormatException(input[0].inStrg + " can\'t be parsed", charInd);
                }
                else
                {
                    exp_Type = input[0].inNode.exp_Type;
                    exp_Data = input[0].inNode.exp_Data;
                    left = input[0].inNode.left;
                    right = input[0].inNode.right;
                    this.input = input[0].inNode.input;
                }
                return;
            }
            int i = 0;
            bool found = false;
            int maxLayer = AST_Expression.operators_ast.Max(a => a.layer);
            for (int layer = 0; layer <= maxLayer; ++layer)
            {
                if (found) break;
                List<AST_Operator> ops = AST_Expression.operators_ast.Where(a => a.layer == layer).ToList();
                if (ops.Count == 0) continue;
                int[] opIndexesLeft = new int[ops.Count];
                int[] opIndexesRight = new int[ops.Count];
                int[] opIndexes = new int[ops.Count];
                for (int j = 0; j < ops.Count; ++j)
                {
                    opIndexesLeft[j] = -1;
                    opIndexesRight[j] = -1;
                    opIndexes[j] = -1;
                }
                int op = 0, opLeft = 0, opRight = 0;
                bool opLeftFilled = false, opRightFilled = false;
                for (int opInd = 0; opInd < ops.Count; ++opInd)
                {
                    string operation = "";
                    string[] opPieces = ops[opInd].oper.Split(
                        new char[] { ' ' }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    operation = opPieces[0];
                    //TODO - DONE - find RightToLeft operators separately and compare their distance to the last element
                    opIndexesLeft[opInd] = input.FindIndex(a => a.isString && a.inStrg == operation);
                    opIndexesRight[opInd] = input.FindLastIndex(a => a.isString && a.inStrg == operation);
                    if ((opIndexesLeft[opLeft] == -1 || 
                        opIndexesLeft[opInd] < opIndexesLeft[opLeft] || !opLeftFilled) && 
                        opIndexesLeft[opInd] != -1 && ops[opInd].isLeftToRight)
                    {
                        //Operator exists and is closer to the start of input
                        opLeft = opInd;
                        opLeftFilled = true;
                    }
                    if((opIndexesRight[opRight] == -1 || 
                        opIndexesRight[opInd] > opIndexesRight[opRight] || !opRightFilled) &&
                        opIndexesRight[opInd] != -1 && !ops[opInd].isLeftToRight)
                    {
                        opRight = opInd;
                        opRightFilled = true;
                    }
                }
                if (!opRightFilled && !opLeftFilled) continue;
                if(input.Count - opIndexesRight[opRight] - 1 < opIndexesLeft[opLeft] && (opRightFilled || !opLeftFilled))
                {
                    op = opRight;
                    opIndexes[op] = opIndexesRight[op];
                }
                else
                {
                    op = opLeft;
                    opIndexes[op] = opIndexesLeft[op];
                }
                if (opIndexes[op] == -1) continue;

                //We found an operation. Now we need to check if it is valid
                if (ops[op].isPaired)
                {
                    //This operation is a brackets operation
                    if (ops[op].operandCount != 1)
                        throw new AST_BadFormatException(
                            "Compiler error - a paired operation with not one operand exists",
                            charInd + opIndexes[op]);
                    //TODO - move this code somewhere so it is executed once
                    List<Tuple<string, string>> brackets =
                        AST_Expression.operators_ast
                            .Where(a => a.isPaired && a.operandCount == 1)
                            .Select(a =>
                            {
                                string[] bracketParts =
                                    a.oper.Split(
                                        new char[] { ' ' },
                                        StringSplitOptions.RemoveEmptyEntries);
                                if (bracketParts.Length != 2)
                                {
                                    throw new AST_BadFormatException(
                                        "Compiler error - a paired operation" +
                                        a.oper +
                                        " with not two pieces exists",
                                        charInd);
                                }
                                return new Tuple<string, string>(bracketParts[0], bracketParts[1]);
                            })
                            .ToList();
                    Stack<string> bracketStack = new Stack<string>();
                    i = opIndexes[op];
                    bracketStack.Push(input[i].inStrg);
                    ++i;
                    while (i < input.Count)
                    {
                        int bracketIndexStart = brackets.FindIndex(a => a.Item1 == input[i].inStrg);
                        int bracketIndexStop = brackets.FindIndex(a => a.Item2 == input[i].inStrg);
                        if (bracketIndexStop != -1)
                        {
                            if (bracketStack.Peek() == brackets[bracketIndexStop].Item1)
                            {
                                bracketStack.Pop();
                            }
                            else throw new AST_BadFormatException(
                                "Bracket sequence of the expression is incorrect", charInd + i);
                        }
                        else if (bracketIndexStart != -1)
                        {
                            bracketStack.Push(input[i].inStrg);
                        }
                        ++i;
                        if (bracketStack.Count == 0) break;
                    }
                    if (bracketStack.Count == 0)
                    {
                        //Closing bracket is at position i-1
                        found = true;
                        if (i - 2 == opIndexes[op])
                        {
                            //Brackets are empty
                            //TODO - call parameter-less functions with brackets
                            //TODO - initialize arrays with empty brackets
                            throw new AST_BadFormatException(
                                "Empty brackets " + input[opIndexes[op]], charInd + i);
                        }
                        //Create the content array
                        List<Exp_Piece> content = new List<Exp_Piece>(i - 2 - opIndexes[op]);
                        for (int j = 0; j < content.Capacity; ++j)
                        {
                            content.Add(input[opIndexes[op] + 1 + j]);
                        }
                        input.RemoveRange(opIndexes[op], i - opIndexes[op]);
                        Exp_Node_New bracketsNode = new Exp_Node_New(content, opIndexes[op] + 1) { exp_Operator = ops[op] };
                        Exp_Piece bracketsPiece;
                        if (opIndexes[op] - 1 >= 0 &&
                            input[opIndexes[op] - 1].isString &&
                            isVariable(input[opIndexes[op] - 1].inStrg))
                        {
                            //We have a function
                            Exp_Node_New functionNode = new Exp_Node_New(
                                null, bracketsNode,
                                null,
                                input[opIndexes[op] - 1].inStrg, Exp_Type.Function);
                            input.RemoveRange(opIndexes[op] - 1, 1);
                            bracketsPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op] - 1,
                                indexEnd = i - 1,
                                inNode = functionNode,
                                inStrg = null,
                                isString = false
                            };
                            input.Insert(opIndexes[op] - 1, bracketsPiece);
                        }
                        else
                        {
                            bracketsPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op],
                                indexEnd = i - 1,
                                inNode = bracketsNode,
                                inStrg = null,
                                isString = false
                            };
                            input.Insert(opIndexes[op], bracketsPiece);
                        }

                        Exp_Node_New result = new Exp_Node_New(input, charInd);
                        this.input = result.input;
                        this.exp_Type = result.exp_Type;
                        this.exp_Data = result.exp_Data;
                        this.exp_Operator = result.exp_Operator;
                        this.left = result.left;
                        this.right = result.right;
                        found = true;
                        return;
                    }
                    else
                    {
                        throw new AST_BadFormatException(
                            "No closing bracket found for " + bracketStack.Peek(), charInd + i);
                    }
                }
                else
                {
                    if (ops[op].isPrefix == true)
                    {
                        if (opIndexes[op] == input.Count - 1)
                        {
                            //Prefix at the end - false match
                            continue;
                        }
                        //We have a prefix operation
                        if (ops[op].operandCount == 0) throw new AST_BadFormatException(
                            "Compiler error - Found a 0-operand operator in a non-0-operand input",
                            charInd + opIndexes[op]);
                        if (ops[op].toEnd)
                        {
                            List<Exp_Piece> content = new List<Exp_Piece>(input.Count - opIndexes[op] - 1);
                            for (int j = 0; j < content.Capacity; ++j)
                            {
                                content.Add(input[opIndexes[op] + 1 + j]);
                            }
                            Exp_Node_New rightNode = new Exp_Node_New(content, opIndexes[op] + 1);
                            input.RemoveRange(opIndexes[op], input.Count - opIndexes[op]);

                            Exp_Node_New rightResult = new Exp_Node_New(
                                null, rightNode,
                                new List<string>(ToListString(content)),
                                ops[op].oper, Exp_Type.Operator){ exp_Operator = ops[op] };
                            Exp_Piece rightResultPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op],
                                indexEnd = opIndexes[op],
                                inStrg = null,
                                inNode = rightResult,
                                isString = false
                            };

                            input.Insert(opIndexes[op], rightResultPiece);
                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                        else
                        {
                            //TODO - leave only similar code
                            List<Exp_Piece> content = new List<Exp_Piece>(1);
                            content.Add(input[opIndexes[op] + 1]);
                            Exp_Node_New rightNode = new Exp_Node_New(content, opIndexes[op] + 1);
                            input.RemoveRange(opIndexes[op], 2);

                            Exp_Node_New rightResult = new Exp_Node_New(
                                null, rightNode,
                                new List<string>(ToListString(content)),
                                ops[op].oper, Exp_Type.Operator){ exp_Operator = ops[op] };
                            Exp_Piece rightResultPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op],
                                indexEnd = opIndexes[op],
                                inStrg = null,
                                inNode = rightResult,
                                isString = false
                            };

                            input.Insert(opIndexes[op], rightResultPiece);
                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                    }
                    else if (ops[op].isPrefix == false)
                    {
                        if (opIndexes[op] == 0)
                        {
                            //Suffix at the start - false match
                            continue;
                        }
                        //We have a suffix operation
                        if (ops[op].operandCount == 0) throw new AST_BadFormatException(
                            "Compiler error - Found a 0-operand operator in a non-0-operand input",
                            charInd + opIndexes[op]);
                        if (ops[op].toEnd)
                        {
                            List<Exp_Piece> content = new List<Exp_Piece>(opIndexes[op]);
                            for (int j = 0; j < content.Capacity; ++j)
                            {
                                content.Add(input[j]);
                            }
                            Exp_Node_New leftNode = new Exp_Node_New(content, charInd);
                            input.RemoveRange(0, opIndexes[op] + 1);

                            Exp_Node_New leftResult = new Exp_Node_New(
                                leftNode, null,
                                new List<string>(ToListString(content)),
                                ops[op].oper, Exp_Type.Operator)
                                { exp_Operator = ops[op] };
                            Exp_Piece leftResultPiece = new Exp_Piece
                            {
                                indexStart = 0,
                                indexEnd = 0,
                                inStrg = null,
                                inNode = leftResult,
                                isString = false
                            };

                            input.Insert(0, leftResultPiece);
                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                        else
                        {
                            List<Exp_Piece> content = new List<Exp_Piece>(1);
                            content.Add(input[opIndexes[op] - 1]);
                            Exp_Node_New leftNode = new Exp_Node_New(content, charInd + opIndexes[op] - 1);
                            input.RemoveRange(opIndexes[op] - 1, 2);

                            Exp_Node_New leftResult = new Exp_Node_New(
                                leftNode, null,
                                new List<string>(ToListString(content)),
                                ops[op].oper, Exp_Type.Operator)
                                { exp_Operator = ops[op] };
                            Exp_Piece leftResultPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op] - 1,
                                indexEnd = opIndexes[op] - 1,
                                inStrg = null,
                                inNode = leftResult,
                                isString = false
                            };

                            input.Insert(opIndexes[op] - 1, leftResultPiece);
                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                    }
                    else
                    {
                        if (opIndexes[op] == 0 || opIndexes[op] == input.Count - 1)
                        {
                            //We have a false match
                            continue;
                        }
                        //We have a standard two-operand operator
                        if (ops[op].operandCount != 2) throw new AST_BadFormatException(
                            "Compiler error - Found a non-2-operand infix operator",
                            charInd + opIndexes[op]);
                        if (ops[op].toEnd)
                        {
                            List<Exp_Piece> contentLeft = new List<Exp_Piece>(opIndexes[op]);
                            for (int j = 0; j < contentLeft.Capacity; ++j)
                            {
                                contentLeft.Add(input[j]);
                            }
                            Exp_Node_New leftNode = new Exp_Node_New(contentLeft, charInd);
                            input.RemoveRange(0, opIndexes[op] + 1);


                            List<Exp_Piece> contentRight = new List<Exp_Piece>(input.Count);
                            for (int j = 0; j < contentRight.Capacity; ++j)
                            {
                                contentRight.Add(input[j]);
                            }
                            Exp_Node_New rightNode = new Exp_Node_New(contentRight, charInd + 1);
                            input.RemoveRange(0, input.Count);


                            Exp_Node_New infixResult = new Exp_Node_New(
                                leftNode, rightNode,
                                new List<string>(ToListString(contentLeft)).
                                    Union(new List<string>(ToListString(contentRight))).ToList(),
                                ops[op].oper, Exp_Type.Operator)
                                { exp_Operator = ops[op] };
                            Exp_Piece infixResultPiece = new Exp_Piece
                            {
                                indexStart = 0,
                                indexEnd = 0,
                                inStrg = null,
                                inNode = infixResult,
                                isString = false
                            };

                            input.Insert(0, infixResultPiece);

                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                        else
                        {
                            List<Exp_Piece> contentLeft = new List<Exp_Piece>(1);
                            contentLeft.Add(input[opIndexes[op] - 1]);
                            Exp_Node_New leftNode = new Exp_Node_New(contentLeft, charInd + opIndexes[op] - 1);
                            input.RemoveRange(opIndexes[op] - 1, 2);


                            List<Exp_Piece> contentRight = new List<Exp_Piece>(1);
                            contentRight.Add(input[opIndexes[op] - 1]);
                            Exp_Node_New rightNode = new Exp_Node_New(contentRight, charInd + opIndexes[op]);
                            input.RemoveRange(opIndexes[op] - 1, 1);


                            Exp_Node_New infixResult = new Exp_Node_New(
                                leftNode, rightNode,
                                new List<string>(ToListString(contentLeft)).
                                    Union(new List<string>(ToListString(contentRight))).ToList(),
                                ops[op].oper, Exp_Type.Operator)
                                { exp_Operator = ops[op] };
                            Exp_Piece infixResultPiece = new Exp_Piece
                            {
                                indexStart = opIndexes[op] - 1,
                                indexEnd = opIndexes[op] - 1,
                                inStrg = null,
                                inNode = infixResult,
                                isString = false
                            };

                            input.Insert(opIndexes[op] - 1, infixResultPiece);

                            Exp_Node_New result = new Exp_Node_New(input, charInd);
                            this.input = result.input;
                            this.exp_Type = result.exp_Type;
                            this.exp_Data = result.exp_Data;
                            this.exp_Operator = result.exp_Operator;
                            this.left = result.left;
                            this.right = result.right;
                            found = true;
                            return;
                        }
                    }
                }
            }
            if (!found) throw new AST_BadFormatException(
                 "No operator or operand found in input", charInd + input.Count - 1);
        }
        public Exp_Node_New(List<string> input, int charInd)
        {
            this.input = input;
            if (input.Count == 1)
            {
                if (isConstant(input[0]))
                {
                    exp_Type = Exp_Type.Constant;
                    exp_Data = input[0];
                    left = null;
                    right = null;
                }
                else if (isVariable(input[0]))
                {
                    exp_Type = Exp_Type.Variable;
                    exp_Data = input[0];
                    left = null;
                    right = null;
                }
                else if (isEmptyOperation(input[0], out AST_Operator @operator))
                {
                    exp_Type = Exp_Type.Operator;
                    exp_Data = input[0];
                    exp_Operator = @operator;
                    left = null;
                    right = null;
                }
                else throw new AST_BadFormatException(input[0] + " can\'t be parsed", charInd);
                return;
            }
            int i = 0;
            List<Exp_Piece> pieces = new List<Exp_Piece>();
            for (i = 0; i < input.Count; ++i)
            {
                pieces.Add(
                    new Exp_Piece
                    { inStrg = input[i], indexStart = i, indexEnd = i, inNode = null, isString = true }
                );
            }
            Exp_Node_New result = new Exp_Node_New(pieces, charInd);
            this.input = result.input;
            this.exp_Type = result.exp_Type;
            this.exp_Data = result.exp_Data;
            this.left = result.left;
            this.right = result.right;
        }
        private bool isEmptyOperation(string input, out AST_Operator oper)
        {
            foreach (var op in AST_Expression.operators_ast)
            {
                if (op.oper == input && op.operandCount == 0)
                {
                    oper = op;
                    return true;
                }
            }
            oper = new AST_Operator();
            return false;
        }

        private List<string> ToListString(List<Exp_Piece> list)
        {
            List<string> result = new List<string>();
            foreach (var piece in list)
            {
                if (piece.isString)
                {
                    result.Add(piece.inStrg);
                }
                else if (piece.inNode.input != null)
                {
                    result.AddRange(piece.inNode.input);
                }
            }
            return result;
        }
    }
}
