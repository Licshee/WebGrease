// if.cs
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Ajax.Utilities
{

    public sealed class IfNode : AstNode
    {
        public AstNode Condition { get; private set; }
        public Block TrueBlock { get; private set; }
        public Block FalseBlock { get; private set; }

        public IfNode(Context context, JSParser parser, AstNode condition, AstNode trueBranch, AstNode falseBranch)
            : base(context, parser)
        {
            Condition = condition;
            TrueBlock = ForceToBlock(trueBranch);
            FalseBlock = ForceToBlock(falseBranch);

            // make sure the parent element is set
            if (Condition != null) Condition.Parent = this;
            if (TrueBlock != null) TrueBlock.Parent = this;
            if (FalseBlock != null) FalseBlock.Parent = this;
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        public void SwapBranches()
        {
            Block temp = TrueBlock;
            TrueBlock = FalseBlock;
            FalseBlock = temp;
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Condition, TrueBlock, FalseBlock);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Condition == oldNode)
            {
                Condition = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (TrueBlock == oldNode)
            {
                TrueBlock = ForceToBlock(newNode);
                if (TrueBlock != null) { TrueBlock.Parent = this; }
                return true;
            }
            if (FalseBlock == oldNode)
            {
                FalseBlock = ForceToBlock(newNode);
                if (FalseBlock != null) { FalseBlock.Parent = this; }
                return true;
            }
            return false;
        }

        internal override bool RequiresSeparator
        {
            get
            {
                // if we have an else block, then the if statement
                // requires a separator if the else block does. 
                // otherwise only if the true case requires one.
                if (FalseBlock != null && FalseBlock.Count > 0)
                {
                    return FalseBlock.RequiresSeparator;
                }
                if (TrueBlock != null && TrueBlock.Count > 0)
                {
                    return TrueBlock.RequiresSeparator;
                }
                return false;
            }
        }

        internal override bool EncloseBlock(EncloseBlockType type)
        {
            // if there's an else block, recurse down that branch
            if (FalseBlock != null)
            {
                return FalseBlock.EncloseBlock(type);
            }
            else if (type == EncloseBlockType.IfWithoutElse)
            {
                // there is no else branch -- we might have to enclose the outer block
                return true;
            }
            else if (TrueBlock != null)
            {
                return TrueBlock.EncloseBlock(type);
            }
            return false;
        }
    }
}