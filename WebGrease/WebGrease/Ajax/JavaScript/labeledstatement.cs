// labeledstatement.cs
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
    public sealed class LabeledStatement : AstNode
    {
        public int NestCount { get; private set; }
        public AstNode Statement { get; private set; }
        public string Label { get; set; }

        public LabeledStatement(Context context, JSParser parser, string label, int nestCount, AstNode statement)
            : base(context, parser)
        {
            Label = label;
            Statement = statement;
            NestCount = nestCount;

            if (Statement != null)
            {
                Statement.Parent = this;
            }
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
                // requires a separator if the statement does
                return (Statement != null ? Statement.RequiresSeparator : false);
            }
        }

        public override AstNode LeftHandSide
        {
            get
            {
                // the label is on the left, but it's sorta ignored
                return (Statement != null ? Statement.LeftHandSide : null);
            }
        }

        internal override bool EncloseBlock(EncloseBlockType type)
        {
            // pass the query on to the statement
            return (Statement != null ? Statement.EncloseBlock(type) : false);
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Statement);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Statement == oldNode)
            {
                Statement = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            return false;
        }
    }
}
