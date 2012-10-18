// call.cs
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
    public sealed class CallNode : Expression
    {
        private AstNode m_func;
        public AstNode Function
        {
            get { return m_func; }
        }

        private bool m_isConstructor;
        public bool IsConstructor
        {
            get { return m_isConstructor; }
            set { m_isConstructor = value; }
        }

        private bool m_inBrackets;
        public bool InBrackets
        {
            get { return m_inBrackets; }
        }

        private AstNodeList m_args;
        public AstNodeList Arguments
        {
            get { return m_args; }
        }

        public CallNode(Context context, JSParser parser, AstNode function, AstNodeList args, bool inBrackets)
            : base(context, parser)
        {
            m_func = function;
            m_args = args;
            m_inBrackets = inBrackets;

            if (m_func != null)
            {
                m_func.Parent = this;
            }
            if (m_args != null)
            {
                m_args.Parent = this;
            }
        }

        public override OperatorPrecedence Precedence
        {
            get
            {
                // new-operator is the unary precedence; () and [] are  field access
                return /*IsConstructor ? OperatorPrecedence.Unary :*/ OperatorPrecedence.FieldAccess;
            }
        }

        public override bool IsExpression
        {
            get
            {
                // normall this would be an expression. BUT we want to check for a
                // call to a member function that is in the "onXXXX" pattern and passing
                // parameters. This is because of a bug in IE that will throw a script error 
                // if you call a native event handler like onclick and pass in a parameter
                // IN A LOGICAL EXPRESSION. For some reason, the simple statement:
                // elem.onclick(e) will work, but elem&&elem.onclick(e) will not. So treat
                // calls to any member operator where the property name starts with "on" and
                // we are passing in arguments as if it were NOT an expression, and it won't
                // get combined.
                Member callMember = Function as Member;
                if (callMember != null
                    && callMember.Name.StartsWith("on", StringComparison.Ordinal)
                    && Arguments.Count > 0)
                {
                    // popped positive -- don't treat it like an expression.
                    return false;
                }

                // otherwise it's okay -- it's an expression and can be combined.
                return true;
            }
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
                return EnumerateNonNullNodes(m_func, m_args);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (m_func == oldNode)
            {
                m_func = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            if (m_args == oldNode)
            {
                if (newNode == null)
                {
                    // remove it
                    m_args = null;
                    return true;
                }
                else
                {
                    // if the new node isn't an AstNodeList, ignore it
                    AstNodeList newList = newNode as AstNodeList;
                    if (newList != null)
                    {
                        m_args = newList;
                        newNode.Parent = this;
                        return true;
                    }
                }
            }
            return false;
        }

        public override AstNode LeftHandSide
        {
            get
            {
                // the function is on the left
                return m_func.LeftHandSide;
            }
        }

        public override bool IsEquivalentTo(AstNode otherNode)
        {
            // a call node is equivalent to another call node if the function and the arguments
            // are all equivalent (and be sure to check for brackets and constructor)
            var otherCall = otherNode as CallNode;
            return otherCall != null
                && this.InBrackets == otherCall.InBrackets
                && this.IsConstructor == otherCall.IsConstructor
                && this.Function.IsEquivalentTo(otherCall.Function)
                && this.Arguments.IsEquivalentTo(otherCall.Arguments);
        }

        internal override bool IsDebuggerStatement
        {
            get
            {
                // see if this is a member, lookup, or call node
                // if it is, then we will pop positive if the recursive call does
                return ((m_func is Member || m_func is CallNode || m_func is Lookup) && m_func.IsDebuggerStatement);
            }
        }

        internal override string GetFunctionGuess(AstNode target)
        {
            // get our guess from the function call
            string funcName = m_func.GetFunctionGuess(target);

            // MSN VOODOO: if this is the addMethod method, then the
            // name of the function is the first parameter. 
            // The syntax of the add method call is: obj.addMethod("name",function(){...})
            // so there should be two parameters....
            if (funcName == "addMethod" && m_args.Count == 2)
            {
                // the first one should be a string constant....
                ConstantWrapper firstParam = m_args[0] as ConstantWrapper;
                // and the second one should be the function expression we're looking for
                if ((firstParam != null) && (firstParam.Value is string) && (m_args[1] == target))
                {
                    // use that first parameter as the guess
                    funcName = firstParam.ToString();
                }
            }
            return funcName;
        }
    }
}