// functionobject.cs
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
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Microsoft.Ajax.Utilities
{
    public sealed class FunctionObject : AstNode
    {
        public Block Body { get; private set; }
        public FunctionType FunctionType { get; private set; }

        private ParameterDeclaration[] m_parameterDeclarations;
        public IList<ParameterDeclaration> ParameterDeclarations
        {
            get
            {
                return m_parameterDeclarations;
            }
        }

        private bool m_leftHandFunction;// = false;
        public bool LeftHandFunctionExpression
        {
            get
            {
                return (FunctionType == FunctionType.Expression && m_leftHandFunction);
            }
            set
            {
                m_leftHandFunction = value;
            }
        }

        public Lookup Identifier { get; private set; }
        private string m_name;
        public string Name
        {
            get
            {
                return (Identifier != null ? Identifier.Name : m_name);
            }
            set
            {
                if (Identifier != null)
                {
                    Identifier.Name = value;
                }
                else
                {
                    m_name = value;
                }
            }
        }
        public Context IdContext { get { return (Identifier == null ? null : Identifier.Context); } }

        public override bool IsExpression
        {
            get
            {
                // if this is a declaration, then it's not an expression. Otherwise treat it 
                // as if it were an expression.
                return !(FunctionType == FunctionType.Declaration);
            }
        }

        // when parsed, this flag indicates that a function declaration is in the
        // proper source-element location
        public bool IsSourceElement
        {
            get;
            set;
        }

        private JSVariableField m_variableField;
        public JSVariableField VariableField { get { return m_variableField; } }
        public int RefCount { get { return (m_variableField == null ? 0 : m_variableField.RefCount); } }

        private FunctionScope m_functionScope;
        public FunctionScope FunctionScope { get { return m_functionScope; } }

        public override ActivationObject EnclosingScope
        {
            get
            {
                return m_functionScope;
            }
        }

        public override OperatorPrecedence Precedence
        {
            get
            {
                // just assume primary -- should only get called for expressions anyway
                return OperatorPrecedence.Primary;
            }
        }

        public FunctionObject(Lookup identifier, JSParser parser, FunctionType functionType, ParameterDeclaration[] parameterDeclarations, Block bodyBlock, Context functionContext, FunctionScope functionScope)
            : base(functionContext, parser)
        {
            FunctionType = functionType;
            m_functionScope = functionScope;
            if (functionScope != null)
            {
                functionScope.FunctionObject = this;
            }

            m_name = string.Empty;
            Identifier = identifier;
            if (Identifier != null) { Identifier.Parent = this; }

            m_parameterDeclarations = parameterDeclarations;

            Body = bodyBlock;
            if (bodyBlock != null) { bodyBlock.Parent = this; }

            // now we need to make sure that the enclosing scope has the name of this function defined
            // so that any references get properly resolved once we start analyzing the parent scope
            // see if this is not anonymnous AND not a getter/setter
            bool isGetterSetter = (FunctionType == FunctionType.Getter || FunctionType == FunctionType.Setter);
            if (Identifier != null && !isGetterSetter)
            {
                // yes -- add the function name to the current enclosing
                // check whether the function name is in use already
                // shouldn't be any duplicate names
                ActivationObject enclosingScope = m_functionScope.Parent;
                // functions aren't owned by block scopes
                while (enclosingScope is BlockScope)
                {
                    enclosingScope = enclosingScope.Parent;
                }

                // if the enclosing scope already contains this name, then we know we have a dup
                string functionName = Identifier.Name;
                m_variableField = enclosingScope[functionName];
                if (m_variableField != null)
                {
                    // it's pointing to a function
                    m_variableField.IsFunction = true;

                    if (FunctionType == FunctionType.Expression)
                    {
                        // if the containing scope is itself a named function expression, then just
                        // continue on as if everything is fine. It will chain and be good.
                        if (m_variableField.FieldType != FieldType.NamedFunctionExpression)
                        {
                            if (m_variableField.NamedFunctionExpression != null)
                            {
                                // we have a second named function expression in the same scope
                                // with the same name. Not an error unless someone actually references
                                // it.

                                // we are now ambiguous.
                                m_variableField.IsAmbiguous = true;

                                // BUT because this field now points to multiple function object, we
                                // need to break the connection. We'll leave the inner NFEs pointing
                                // to this field as the outer field so the names all align, however.
                                DetachFromOuterField(true);

                                // create a new NFE pointing to the existing field as the outer so
                                // the names stay in sync, and with a value of our function object.
                                var namedExpressionField = 
                                    new JSVariableField(FieldType.NamedFunctionExpression, m_variableField);
                                namedExpressionField.FieldValue = this;
                                m_functionScope.AddField(namedExpressionField);

                                // hook our function object up to the named field
                                m_variableField = namedExpressionField;
                                Identifier.VariableField = namedExpressionField;

                                // if we want to preserve the function names, mark this field as not crunchable
                                if (Parser.Settings.PreserveFunctionNames)
                                {
                                    namedExpressionField.CanCrunch = false;
                                }

                                // we're done; quit.
                                return;
                            }
                            else if (m_variableField.IsAmbiguous)
                            {
                                // we're pointing to a field that is already marked as ambiguous.
                                // just create our own NFE pointing to this one, and hook us up.
                                var namedExpressionField = 
                                    new JSVariableField(FieldType.NamedFunctionExpression, m_variableField);
                                namedExpressionField.FieldValue = this;
                                m_functionScope.AddField(namedExpressionField);

                                // hook our function object up to the named field
                                m_variableField = namedExpressionField;
                                Identifier.VariableField = namedExpressionField;

                                // if we want to preserve the function names, mark this field as not crunchable
                                if (Parser.Settings.PreserveFunctionNames)
                                {
                                    namedExpressionField.CanCrunch = false;
                                }

                                // we're done; quit.
                                return;
                            }
                            else
                            {
                                // we are a named function expression in a scope that has already
                                // defined a local variable of the same name. Not good. Throw the 
                                // error but keep them attached because the names have to be synced
                                // to keep the same meaning in all browsers.
                                Identifier.Context.HandleError(JSError.AmbiguousNamedFunctionExpression, false);

                                // if we are preserving function names, then we need to mark this field
                                // as not crunchable
                                if (Parser.Settings.PreserveFunctionNames)
                                {
                                    m_variableField.CanCrunch = false;
                                }
                            }
                        }
                        /*else
                        {
                            // it's okay; just chain the NFEs as normal and everything will work out
                            // and the names will be properly synced.
                        }*/
                    }
                    else
                    {
                        // function declaration -- duplicate name
                        Identifier.Context.HandleError(JSError.DuplicateName, false);
                    }
                }
                else
                {
                    // doesn't exist -- create it now
                    m_variableField = enclosingScope.DeclareField(functionName, this, 0);
                    m_variableField.OriginalContext = Identifier.Context;

                    // and it's a pointing to a function object
                    m_variableField.IsFunction = true;
                }

                // set the identifier variable field now. We *know* what the field is now, and during
                // Analyze mode we aren't going to recurse into the identifier because that would add 
                // a reference to it.
                Identifier.VariableField = m_variableField;

                // if we're here, we have a name. if this is a function expression, then we have
                // a named function expression and we need to do a little more work to prepare for
                // the ambiguities of named function expressions in various browsers.
                if (FunctionType == FunctionType.Expression)
                {
                    // now add a field within the function scope that indicates that it's okay to reference
                    // this named function expression from WITHIN the function itself.
                    // the inner field points to the outer field since we're going to want to catch ambiguous
                    // references in the future
                    var namedExpressionField = new JSVariableField(FieldType.NamedFunctionExpression, m_variableField);
                    m_functionScope.AddField(namedExpressionField);
                    m_variableField.NamedFunctionExpression = namedExpressionField;

                    // if we want to preserve the function names, mark this field as not crunchable
                    if (Parser.Settings.PreserveFunctionNames)
                    {
                        namedExpressionField.CanCrunch = false;
                    }
                }
                else
                {
                    // function declarations are declared by definition
                    m_variableField.IsDeclared = true;
                }
            }
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }

        internal bool IsReferenced(int fieldRefCount)
        {
            // function expressions, getters, and setters are always referenced.
            // for function declarations, if the field refcount is zero, then we know we're not referenced.
            // otherwise, let's check with the scopes to see what's up.
            bool isReferenced = false;
            if (FunctionType != FunctionType.Declaration)
            {
                isReferenced = true;
            }
            else if (fieldRefCount > 0)
            {
                // we are going to visit each referenced scope and ask if any are
                // referenced. If any one is, then we are too. Since this will be a graph,
                // keep a hashtable of visited scopes so we don't get into endless loops.
                isReferenced = m_functionScope.IsReferenced(null);
            }
            return isReferenced;
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                return EnumerateNonNullNodes(Body);
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Body == oldNode)
            {
                if (newNode == null)
                {
                    // just remove it
                    Body = null;
                    return true;
                }
                else
                {
                    // if the new node isn't a block, ignore it
                    Block newBlock = newNode as Block;
                    if (newBlock != null)
                    {
                        Body = newBlock;
                        newNode.Parent = this;
                        return true;
                    }
                }
            }
            return false;
        }

        internal override bool RequiresSeparator
        {
            get { return HideFromOutput; }
        }

        internal bool IsArgumentTrimmable(JSVariableField targetArgumentField)
        {
            // walk backward until we either find the given argument field or the
            // first parameter that is referenced. 
            // If we find the argument field, then we can trim it because there are no
            // referenced parameters after it.
            // if we find a referenced argument, then the parameter is not trimmable.
            JSVariableField argumentField = null;
            if (m_parameterDeclarations != null)
            {
                for (int index = m_parameterDeclarations.Length - 1; index >= 0; --index)
                {
                    argumentField = m_parameterDeclarations[index].Field;
                    if (argumentField != null
                        && (argumentField == targetArgumentField || argumentField.IsReferenced))
                    {
                        break;
                    }
                }
            }

            // if the argument field we landed on is the same as the target argument field,
            // then we found the target argument BEFORE we found a referenced parameter. Therefore
            // the argument can be trimmed.
            return (argumentField == targetArgumentField);
        }

        public void DetachFromOuterField(bool leaveInnerPointingToOuter)
        {
            // we're going to change the reference from the outer variable to the inner variable
            // save the inner variable field
            var nfeField = m_variableField.NamedFunctionExpression;
            // break the connection from the outer to the inner
            m_variableField.NamedFunctionExpression = null;

            if (!leaveInnerPointingToOuter)
            {
                // detach the inner from the outer
                nfeField.Detach();
            }

            // the outer field no longer points to the function object
            m_variableField.FieldValue = null;
            // but the inner field should
            nfeField.FieldValue = this;

            // our variable field is now the inner field
            m_variableField = nfeField;

            // and so is out identifier
            Identifier.VariableField = nfeField;
        }
    }
}