// try.cs
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
    public sealed class TryNode : AstNode
    {
		public Block TryBlock { get; private set; }
		public Block CatchBlock { get; private set; }
		public Block FinallyBlock { get; private set; }

        public string CatchVarName { get; private set; }
        public Context CatchVarContext { get; private set; }

        private JSVariableField m_catchVariable;
        public JSVariableField CatchVariable { get { return m_catchVariable; } }

        public TryNode(Context context, JSParser parser, AstNode tryBlock, string catchVarName, Context catchVarContext, AstNode catchBlock, AstNode finallyBlock)
            : base(context, parser)
        {
            CatchVarName = catchVarName;
            TryBlock = ForceToBlock(tryBlock);
            CatchBlock = ForceToBlock(catchBlock);
            FinallyBlock = ForceToBlock(finallyBlock);
            if (TryBlock != null) { TryBlock.Parent = this; }
            if (CatchBlock != null) { CatchBlock.Parent = this; }
            if (FinallyBlock != null) { FinallyBlock.Parent = this; }

            CatchVarContext = catchVarContext;
        }

        public void SetCatchVariable(JSVariableField field)
        {
            m_catchVariable = field;
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(TryBlock, CatchBlock, FinallyBlock);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (TryBlock == oldNode)
            {
                TryBlock = ForceToBlock(newNode);
                if (TryBlock != null) { TryBlock.Parent = this; }
                return true;
            }
            if (CatchBlock == oldNode)
            {
                CatchBlock = ForceToBlock(newNode);
                if (CatchBlock != null) { CatchBlock.Parent = this; }
                return true;
            }
            if (FinallyBlock == oldNode)
            {
                FinallyBlock = ForceToBlock(newNode);
                if (FinallyBlock != null) { FinallyBlock.Parent = this; }
                return true;
            }
            return false;
        }

        internal override bool RequiresSeparator
        {
            get
            {
                // try requires no separator
                return false;
            }
        }
    }
}
