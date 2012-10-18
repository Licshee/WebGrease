// member.cs
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
using System.Text;

namespace Microsoft.Ajax.Utilities
{

    public sealed class Member : Expression
    {
        public AstNode Root { get; private set; }
        public string Name { get; set; }
        public Context NameContext { get; private set; }

        public Member(Context context, JSParser parser, AstNode rootObject, string memberName, Context idContext)
            : base(context, parser)
        {
            Name = memberName;
            NameContext = idContext;

            Root = rootObject;
            if (Root != null) Root.Parent = this;
        }

        public override OperatorPrecedence Precedence
        {
            get
            {
                return OperatorPrecedence.FieldAccess;
            }
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        public override bool IsEquivalentTo(AstNode otherNode)
        {
            var otherMember = otherNode as Member;
            return otherMember != null
                && string.CompareOrdinal(this.Name, otherMember.Name) == 0
                && this.Root.IsEquivalentTo(otherMember.Root);
        }

        internal override string GetFunctionGuess(AstNode target)
        {
            // MSN VOODOO: treat the as and ns methods as special if the expression is the root,
            // the parent is the call, and there is one string parameter -- use the string parameter
            if (Root == target && (Name == "as" || Name == "ns"))
            {
                CallNode call = Parent as CallNode;
                if (call != null && call.Arguments.Count == 1)
                {
                    ConstantWrapper firstParam = call.Arguments[0] as ConstantWrapper;
                    if (firstParam != null)
                    {
                        return firstParam.ToString();
                    }
                }
            }
            return Name;
        }

        internal override bool IsDebuggerStatement
        {
            get
            {
                // depends on whether the root is
                return Root.IsDebuggerStatement;
            }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Root);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Root == oldNode)
            {
                Root = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            return false;
        }

        public override AstNode LeftHandSide
        {
            get
            {
                // the root object is on the left
                return Root.LeftHandSide;
            }
        }
    }
}
