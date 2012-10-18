// ast.cs
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
using System.IO;
using System.Text;

namespace Microsoft.Ajax.Utilities
{
    internal enum EncloseBlockType
    {
        IfWithoutElse,
        SingleDoWhile
    }

    public abstract class AstNode
    {
        // this is used in the child enumeration for nodes that don't have any children
        private static readonly IEnumerable<AstNode> s_emptyChildrenCollection = new AstNode[0];

        public AstNode Parent { get; set; }
        public Context Context { get; set; }
        public JSParser Parser { get; private set; }

        protected AstNode(Context context, JSParser parser)
        {
            Parser = parser;
            if (context != null)
            {
                Context = context;
            }
            else
            {
                // generate a bogus context
                Context = new Context(parser);
            }
        }

        internal Stack<ActivationObject> ScopeStack { get { return Parser.ScopeStack; } }

        public virtual bool IsExpression { get { return false; } }

        public virtual string ToCode() 
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                var outputVisitor = new OutputVisitor(writer, Parser.Settings);
                this.Accept(outputVisitor);
                return writer.ToString();
            }
        }

        protected Block ForceToBlock(AstNode astNode)
        {
            // if the node is null or already a block, then we're 
            // good to go -- just return it.
            Block block = astNode as Block;
            if (astNode == null || block != null)
            {
                return block;
            }

            // it's not a block, so create a new block, append the astnode
            // and return the block
            block = new Block(null, Parser);
            block.Append(astNode);
            return block;
        }

        internal virtual string GetFunctionGuess(AstNode target)
        {
            // most objects serived from AST return an empty string
            return string.Empty;
        }

        internal virtual bool EncloseBlock(EncloseBlockType type)
        {
            // almost all statements return false
            return false;
        }

        internal virtual bool RequiresSeparator
        {
            get { return true; }
        }

        internal virtual bool IsDebuggerStatement
        {
            get { return false; }
        }

        public virtual OperatorPrecedence Precedence
        {
            get { return OperatorPrecedence.None; }
        }

        public bool HideFromOutput { get; set; }

        public virtual PrimitiveType FindPrimitiveType()
        {
            // by default, we don't know what the primitive type of this node is
            return PrimitiveType.Other;
        }

        public virtual IEnumerable<AstNode> Children
        {
            get { return s_emptyChildrenCollection; }
        }

        internal static IEnumerable<AstNode> EnumerateNonNullNodes<T>(IList<T> nodes) where T: AstNode
        {
            for (int ndx = 0; ndx < nodes.Count; ++ndx)
            {
                if (nodes[ndx] != null)
                {
                    yield return nodes[ndx];
                }
            }
        }

        internal static IEnumerable<AstNode> EnumerateNonNullNodes(AstNode n1, AstNode n2 = null, AstNode n3 = null, AstNode n4 = null) {
            return EnumerateNonNullNodes(new[] { n1, n2, n3, n4 });
        }

        public bool IsWindowLookup
        {
            get
            {
                Lookup lookup = this as Lookup;
                return (lookup != null
                        && string.CompareOrdinal(lookup.Name, "window") == 0
                        && (lookup.VariableField == null || lookup.VariableField.FieldType == FieldType.Predefined));
            }
        }

        public virtual bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            return false;
        }

        public virtual AstNode LeftHandSide
        {
            get
            {
                // default is just to return ourselves
                return this;
            }
        }

        public virtual ActivationObject EnclosingScope
        {
            get
            {
                // if we don't have a parent, then we are in the global scope.
                // otherwise, just ask our parent. Nodes with scope will override this property.
                return Parent != null ? Parent.EnclosingScope : Parser.GlobalScope;
            }
        }

        /// <summary>
        /// Abstract method to be implemented by every concrete class.
        /// Returns true of the other object is equivalent to this object
        /// </summary>
        /// <param name="otherNode"></param>
        /// <returns></returns>
        public virtual bool IsEquivalentTo(AstNode otherNode)
        {
            // by default nodes aren't equivalent to each other unless we know FOR SURE that they are
            return false;
        }

        /// <summary>
        /// Abstract method to be implemented by every concrete node class
        /// </summary>
        /// <param name="visitor">visitor to accept</param>
        public abstract void Accept(IVisitor visitor);

        /// <summary>
        /// Returns true if the node contains an in-operator
        /// </summary>
        public virtual bool ContainsInOperator
        {
            get
            {
                // recursivelt check all children
                foreach (var child in Children)
                {
                    if (child.ContainsInOperator)
                    {
                        return true;
                    }
                }

                // if we get here, we didn'thave any in-operators
                return false;
            }
        }
    }
}
