// arrayliteral.cs
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
using System.Globalization;
using System.Text;

namespace Microsoft.Ajax.Utilities
{
    public sealed class ArrayLiteral : Expression
    {
        public AstNodeList Elements { get; private set; }

        public ArrayLiteral(Context context, JSParser parser, AstNodeList elements)
            : base(context, parser)
        {
            Elements = elements;
            if (Elements != null) { Elements.Parent = this; }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Elements);
            }
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            // if the old node isn't our element list, ignore the cal
            if (oldNode == Elements)
            {
                if (newNode == null)
                {
                    // we want to remove the list altogether
                    Elements = null;
                    return true;
                }
                else
                {
                    // if the new node isn't an AstNodeList, then ignore the call
                    AstNodeList newList = newNode as AstNodeList;
                    if (newList != null)
                    {
                        // replace it
                        Elements = newList;
                        newList.Parent = this;
                        return true;
                    }
                }
            }
            return false;
        }

        internal override string GetFunctionGuess(AstNode target)
        {
            // find the index of the target item
            for (int ndx = 0; ndx < Elements.Count; ++ndx)
            {
                if (Elements[ndx] == target)
                {
                    // we'll append the index to the guess for this array
                    string parentGuess = Parent.GetFunctionGuess(this);
                    return "{0}_{1}".FormatInvariant(parentGuess, ndx);
                }
            }
            // didn't find it
            return string.Empty;
        }
    }
}
