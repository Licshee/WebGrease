// switchcase.cs
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
    public sealed class SwitchCase : AstNode
    {
        public AstNode CaseValue { get; private set; }

        private Block m_statements;
        public Block Statements
        {
            get { return m_statements; }
        }

        internal bool IsDefault
        {
            get { return (CaseValue == null); }
        }

        public SwitchCase(Context context, JSParser parser, AstNode caseValue, Block statements)
            : base(context, parser)
        {
            CaseValue = caseValue;
            if (caseValue != null)
            {
                caseValue.Parent = this;
            }

            if (statements != null)
            {
                if (statements.Count == 1)
                {
                    // if there is only one item in the block
                    // and that one item IS a block...
                    Block block = statements[0] as Block;
                    if (block != null)
                    {
                        // then we can skip the intermediary block because all it
                        // does is add braces around the block, which aren't needed
                        statements = block;
                    }
                }
            }
            m_statements = statements;
            if (statements != null)
            {
                statements.Parent = this;
            }
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        internal override string GetFunctionGuess(AstNode target)
        {
            return CaseValue.GetFunctionGuess(target);
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(CaseValue, m_statements);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (CaseValue == oldNode)
            {
                CaseValue = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (m_statements == oldNode)
            {
                if (newNode == null)
                {
                    // remove it
                    m_statements = null;
                    return true;
                }
                else
                {
                    // if the new node isn't a block, ignore the call
                    Block newBlock = newNode as Block;
                    if (newBlock != null)
                    {
                        m_statements = newBlock;
                        newNode.Parent = this;
                        return true;
                    }
                }
            }
            return false;
        }

        internal override bool RequiresSeparator
        {
            get
            {
                // no statements doesn't require a separator.
                // otherwise only if statements require it
                if (m_statements == null || m_statements.Count == 0)
                {
                    return false;
                }

                return m_statements[m_statements.Count - 1].RequiresSeparator;
            }
        }
    }
}