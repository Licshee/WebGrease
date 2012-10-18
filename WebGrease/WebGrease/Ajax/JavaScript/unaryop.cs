// unaryop.cs
//
// Copyright 2010 Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Ajax.Utilities
{
    public class UnaryOperator : Expression
    {
        public AstNode Operand { get; private set; }
        public JSToken OperatorToken { get; private set; }
        public bool IsPostfix { get; private set; }
        public bool OperatorInConditionalCompilationComment { get; set; }
        public bool ConditionalCommentContainsOn { get; set; }

        public UnaryOperator(Context context, JSParser parser, AstNode operand, JSToken operatorToken, bool isPostfix)
            : base(context, parser)
        {
            Operand = operand;
            OperatorToken = operatorToken;
            IsPostfix = isPostfix;

            if (Operand != null) Operand.Parent = this;

            Debug.Assert(!isPostfix || operatorToken == JSToken.Increment || operatorToken == JSToken.Decrement, "Postfix flag is only for Increment or Decrement");
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        public override PrimitiveType FindPrimitiveType()
        {
            switch (OperatorToken)
            {
                case JSToken.TypeOf:
                    // typeof ALWAYS returns type string
                    return PrimitiveType.String;

                case JSToken.LogicalNot:
                    // ! always return boolean
                    return PrimitiveType.Boolean;

                case JSToken.Void:
                case JSToken.Delete:
                    // void returns undefined.
                    // delete returns number, but just return other
                    return PrimitiveType.Other;

                default:
                    // all other unary operators return a number
                    return PrimitiveType.Number;
            }
        }

        public override OperatorPrecedence Precedence
        {
            get
            {
                // assume unary precedence
                return OperatorPrecedence.Unary;
            }
        }

        internal override string GetFunctionGuess(AstNode target)
        {
            return Operand.GetFunctionGuess(target);
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Operand);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Operand == oldNode)
            {
                Operand = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            return false;
        }

        protected bool NeedsParentheses
        {
            // binary and conditional operators are all lesser-precedence than unaries
            get
            {
                // we only need parens if the operand is a binary op or a conditional op
                return (Operand is BinaryOperator || Operand is Conditional);
            }
        }

        public override bool IsEquivalentTo(AstNode otherNode)
        {
            var otherUnary = otherNode as UnaryOperator;
            return otherUnary != null
                && OperatorToken == otherUnary.OperatorToken
                && Operand.IsEquivalentTo(otherUnary.Operand);
        }

        public override string ToString()
        {
            return OutputVisitor.OperatorString(OperatorToken)
                + (Operand == null ? "<null>" : Operand.ToString());
        }
    }
}
