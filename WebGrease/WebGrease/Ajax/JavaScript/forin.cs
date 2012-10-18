// forin.cs
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
    public sealed class ForIn : AstNode
    {
        public AstNode Variable { get; private set; }
        public AstNode Collection { get; private set; }
        public Block Body { get; private set; }

        public ForIn(Context context, JSParser parser, AstNode var, AstNode collection, AstNode body)
            : base(context, parser)
        {
            Variable = var;
            Collection = collection;
            Body = ForceToBlock(body);
            if (Body != null) Body.Parent = this;
            if (Variable != null) Variable.Parent = this;
            if (Collection != null) Collection.Parent = this;
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
                return EnumerateNonNullNodes(Variable, Collection, Body);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Variable == oldNode)
            {
                Variable = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (Collection == oldNode)
            {
                Collection = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (Body == oldNode)
            {
                Body = ForceToBlock(newNode);
                if (Body != null) { Body.Parent = this; }
                return true;
            }
            return false;
        }

        internal override bool EncloseBlock(EncloseBlockType type)
        {
            // pass the query on to the body
            return Body == null ? false : Body.EncloseBlock(type);
        }

        internal override bool RequiresSeparator
        {
            get
            {
                // requires a separator if the body does
                return Body == null || Body.Count == 0 ? false : Body.RequiresSeparator;
            }
        }
    }
}
