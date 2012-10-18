// block.cs
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

    public sealed class Block : AstNode
    {
        private List<AstNode> m_list;

        public AstNode this[int index]
        {
            get { return m_list[index]; }
            set
            {
                m_list[index] = value;
                if (value != null)
                { 
                    value.Parent = this; 
                }
            }
        }

        private BlockScope m_blockScope;
        internal BlockScope BlockScope
        {
            get { return m_blockScope; }
            set { m_blockScope = value; }
        }

        public override ActivationObject EnclosingScope
        {
            get
            {
                return m_blockScope != null ? m_blockScope : base.EnclosingScope;
            }
        }

        public bool BraceOnNewLine { get; set; }

        public Block(Context context, JSParser parser)
            : base(context, parser)
        {
            m_list = new List<AstNode>();
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
                // 0 statements, true (lone semicolon)
                // 1 and list[0].HideFromOutput = false 
                // 1 = ask list[0]
                // > 1, false (enclosed in braces
                // if there are 2 or more statements in the block, then
                // we'll wrap them in braces and they won't need a separator
                return (
                  m_list.Count == 0
                  ? true
                  : (m_list.Count == 1  && !m_list[0].HideFromOutput ? m_list[0].RequiresSeparator : false)
                  );
            }
        }

        internal override bool EncloseBlock(EncloseBlockType type)
        {
            // if there's more than one item, then return false.
            // otherwise recurse the call
            return (m_list.Count == 1 && m_list[0].EncloseBlock(type));
        }

        internal override bool IsDebuggerStatement
        {
            get
            {
                // a block will pop-positive for being a debugger statement
                // if all the statements within it are debugger statements. 
                // So loop through our list, and if any isn't, return false.
                // otherwise return true.
                // empty blocks do not pop positive for "debugger" statements
                if (m_list.Count == 0)
                {
                    return false;
                }

                foreach (AstNode statement in m_list)
                {
                    if (!statement.IsDebuggerStatement)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override bool IsExpression
        {
            get
            {
                // if this block contains a single statement, then recurse.
                // otherwise it isn't.
                //
                // TODO: if there are no statements -- empty block -- then is is an expression?
                // I mean, we can make an empty block be an expression by just making it a zero. 
                return m_list.Count == 1 && m_list[0].IsExpression;
            }
        }

        public int Count
        {
            get { return m_list.Count; }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(m_list);
            }
        }

        public int StatementIndex(AstNode childNode)
        {
            // find childNode in our collection of statements
            for (var ndx = 0; ndx < m_list.Count; ++ndx)
            {
                if (m_list[ndx] == childNode)
                {
                    return ndx;
                }
            }
            // if we got here, then childNode is not a statement in our collection!
            return -1;
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            for (int ndx = m_list.Count - 1; ndx >= 0; --ndx)
            {
                if (m_list[ndx] == oldNode)
                {
                    if (newNode == null)
                    {
                        // just remove it
                        m_list.RemoveAt(ndx);
                    }
                    else
                    {
                        Block newBlock = newNode as Block;
                        if (newBlock != null)
                        {
                            // the new "statement" is a block. That means we need to insert all
                            // the statements from the new block at the location of the old item.
                            m_list.RemoveAt(ndx);
                            m_list.InsertRange(ndx, newBlock.m_list);
                        }
                        else
                        {
                            // not a block -- slap it in there
                            m_list[ndx] = newNode;
                            newNode.Parent = this;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public void Append(AstNode element)
        {
            Block block = element as Block;
            if (block != null)
            {
                // adding a block to the block -- just append the elements
                // from the block to ourselves
                InsertRange(m_list.Count, block.Children);
            }
            else if (element != null)
            {
                // not a block....
                element.Parent = this;
                m_list.Add(element);
            }
        }

        public int IndexOf(AstNode child)
        {
            return m_list.IndexOf(child);
        }

        public void InsertAfter(AstNode after, AstNode item)
        {
            if (item != null)
            {
                int index = m_list.IndexOf(after);
                if (index >= 0)
                {
                    var block = item as Block;
                    if (block != null)
                    {
                        // don't insert a block into a block -- insert the new block's
                        // children instead (don't want nested blocks)
                        InsertRange(index + 1, block.Children);
                    }
                    else
                    {
                        item.Parent = this;
                        m_list.Insert(index + 1, item);
                    }
                }
            }
        }

        public void Insert(int position, AstNode item)
        {
            if (item != null)
            {
                var block = item as Block;
                if (block != null)
                {
                    InsertRange(position, block.Children);
                }
                else
                {
                    item.Parent = this;
                    m_list.Insert(position, item);
                }
            }
        }

        public void RemoveLast()
        {
            m_list.RemoveAt(m_list.Count - 1);
        }

        public void RemoveAt(int index)
        {
            if (0 <= index && index < m_list.Count)
            {
                m_list.RemoveAt(index);
            }
        }

        public void InsertRange(int index, IEnumerable<AstNode> newItems)
        {
            if (newItems != null)
            {
                m_list.InsertRange(index, newItems);
                foreach (AstNode newItem in newItems)
                {
                    newItem.Parent = this;
                }
            }
        }

        public void Clear()
        {
            m_list.Clear();
        }
    }
}