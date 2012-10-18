// switch.cs
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
    public sealed class Switch : AstNode
    {
        public AstNode Expression { get; private set; }
        public AstNodeList Cases { get; private set; }
        public bool BraceOnNewLine { get; set; }

        public Switch(Context context, JSParser parser, AstNode expression, AstNodeList cases, bool braceOnNewLine)
            : base(context, parser)
        {
            Expression = expression;
            Cases = cases;
            BraceOnNewLine = braceOnNewLine;

            if (Expression != null) Expression.Parent = this;
            if (Cases != null) Cases.Parent = this;
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        internal override bool RequiresSeparator
        {
            get
            {
                // switch always has curly-braces, so we don't
                // require the separator
                return false;
            }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Expression, Cases);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Expression == oldNode)
            {
                Expression = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (Cases == oldNode)
            {
                if (newNode == null)
                {
                    // remove it
                    Cases = null;
                    return true;
                }
                else
                {
                    // if the new node isn't an AstNodeList, ignore the call
                    AstNodeList newList = newNode as AstNodeList;
                    if (newList != null)
                    {
                        Cases = newList;
                        newNode.Parent = this;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
