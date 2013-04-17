﻿// ResolutionVisitor.cs
//
// Copyright 2012 Microsoft Corporation
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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Ajax.Utilities
{
    /// <summary>
    /// Traverse the tree to build up scope lexically-declared names, var-declared names,
    /// and lookups, then resolve everything.
    /// </summary>
    public class ResolutionVisitor : IVisitor
    {
        #region private fields

        /// <summary>index to use for ordering the statements in this scope</summary>
        private long m_orderIndex;

        /// <summary>flag indicating whether we've encountered some unreachable code</summary>
        private bool m_isUnreachable;

        /// <summary>depth level of with-statements, needed so we can treat decls within with-scopes specially</summary>
        private int m_withDepth;

        /// <summary>stack to maintain the current lexical scope as we traverse the tree</summary>
        private Stack<ActivationObject> m_lexicalStack;

        /// <summary>stack to maintain the current variable scope as we traverse the tree</summary>
        private Stack<ActivationObject> m_variableStack;

        /// <summary>code setings</summary>
        private CodeSettings m_settings;

        #endregion

        #region private properties

        /// <summary>Current lexical scope</summary>
        private ActivationObject CurrentLexicalScope
        {
            get
            {
                return m_lexicalStack.Peek();
            }
        }

        /// <summary>current variable scope</summary>
        private ActivationObject CurrentVariableScope
        {
            get
            {
                return m_variableStack.Peek();
            }
        }

        /// <summary>retrieve the next order index</summary>
        private long NextOrderIndex
        {
            get
            {
                return m_isUnreachable ? 0 : ++m_orderIndex;
            }
        }

        #endregion

        #region private constructor

        private ResolutionVisitor(ActivationObject rootScope, CodeSettings settings)
        {
            // create the lexical and variable scope stacks and push the root scope onto them
            m_lexicalStack = new Stack<ActivationObject>();
            m_lexicalStack.Push(rootScope);

            m_variableStack = new Stack<ActivationObject>();
            m_variableStack.Push(rootScope);

            m_settings = settings;
        }

        #endregion

        public static void Apply(AstNode node, ActivationObject scope, CodeSettings settings)
        {
            if (node != null && scope != null)
            {
                // create the visitor and run it. This will create all the child
                // scopes and populate all the scopes with the var-decl, lex-decl,
                // and lookup references within them.
                var visitor = new ResolutionVisitor(scope, settings);
                node.Accept(visitor);

                // now that all the scopes are created and they all know what decls
                // they contains, create all the fields
                CreateFields(scope);

                // now that all the fields have been created in all the scopes,
                // let's go through and resolve all the references
                ResolveLookups(scope, settings);

                // now that everything is declared and resolved as per the language specs,
                // we need to go back and add ghosted fields for older versions of IE that
                // incorrectly implement catch-variables and named function expressions.
                AddGhostedFields(scope);
            }
        }

        #region private static methods

        private static void CollapseBlockScope(ActivationObject blockScope)
        {
            // copy over the stuff we want to carry over to the parent
            blockScope.ScopeLookups.CopyItemsTo(blockScope.Parent.ScopeLookups);
            blockScope.VarDeclaredNames.CopyItemsTo(blockScope.Parent.VarDeclaredNames);
            blockScope.ChildScopes.CopyItemsTo(blockScope.Parent.ChildScopes);
            blockScope.GhostedCatchParameters.CopyItemsTo(blockScope.Parent.GhostedCatchParameters);
            blockScope.GhostedFunctions.CopyItemsTo(blockScope.Parent.GhostedFunctions);

            // remove it from its parent's collection of child scopes
            blockScope.Parent.ChildScopes.Remove(blockScope);
        }

        private static void CreateFields(ActivationObject scope)
        {
            // declare this scope
            scope.DeclareScope();

            // and recurse
            foreach (var childScope in scope.ChildScopes)
            {
                CreateFields(childScope);
            }
        }

        private static void ResolveLookups(ActivationObject scope, CodeSettings settings)
        {
            // resolve each lookup this scope contains
            foreach (var lookup in scope.ScopeLookups)
            {
                ResolveLookup(scope, lookup, settings);
            }

            // and recurse
            foreach (var childScope in scope.ChildScopes)
            {
                ResolveLookups(childScope, settings);
            }

            // mark any variables defined in this scope that don't have any references
            // so we can throw warnings later. We can't rely on the reference count because
            // we might remove references while optimizing code -- if we throw an error when
            // the count gets to zero, then we would be reporting errors that don't exist.
            // but we DO know right now what isn't referenced at all.
            foreach (var field in scope.NameTable.Values)
            {
                if (field.RefCount == 0)
                {
                    field.HasNoReferences = true;
                }
            }
        }

        private static void MakeExpectedGlobal(JSVariableField varField)
        {
            // to make this an expected global, we're going to change the type of this field, 
            // then just keep walking up the outer field references doing the same
            do
            {
                varField.FieldType = FieldType.Global;
                varField = varField.OuterField;
            }
            while (varField != null);
        }

        private static void ResolveLookup(ActivationObject scope, Lookup lookup, CodeSettings settings)
        {
            // resolve lookup via the lexical scope
            lookup.VariableField = scope.FindReference(lookup.Name);
            if (lookup.VariableField.FieldType == FieldType.UndefinedGlobal)
            {
                // couldn't find it.
                // if the lookup isn't generated and isn't the object of a typeof operator,
                // then we want to throw an error.
                if (!lookup.IsGenerated)
                {
                    var parentUnaryOp = lookup.Parent as UnaryOperator;
                    if (parentUnaryOp != null && parentUnaryOp.OperatorToken == JSToken.TypeOf)
                    {
                        // this undefined lookup is the target of a typeof operator.
                        // I think it's safe to assume we're going to use it. Don't throw an error
                        // and instead add it to the "known" expected globals of the global scope
                        MakeExpectedGlobal(lookup.VariableField);
                    }
                    else
                    {
                        // report this undefined reference
                        lookup.Context.ReportUndefined(lookup);

                        // possibly undefined global (but definitely not local).
                        // see if this is a function or a variable.
                        var callNode = lookup.Parent as CallNode;
                        var isFunction = callNode != null && callNode.Function == lookup;
                        lookup.Context.HandleError((isFunction ? JSError.UndeclaredFunction : JSError.UndeclaredVariable), false);
                    }
                }
            }
            else if (lookup.VariableField.FieldType == FieldType.Predefined)
            {
                if (string.CompareOrdinal(lookup.Name, "window") == 0)
                {
                    // it's the global window object
                    // see if it's the child of a member or call-brackets node
                    var member = lookup.Parent as Member;
                    if (member != null)
                    {
                        // we have window.XXXX. Add XXXX to the known globals if it
                        // isn't already a known item.
                        scope.AddGlobal(member.Name);
                    }
                    else
                    {
                        var callNode = lookup.Parent as CallNode;
                        if (callNode != null && callNode.InBrackets
                            && callNode.Arguments.Count == 1
                            && callNode.Arguments[0] is ConstantWrapper
                            && callNode.Arguments[0].FindPrimitiveType() == PrimitiveType.String)
                        {
                            // we have window["XXXX"]. See if XXXX is a valid identifier.
                            // TODO: we could get rid of the ConstantWrapper restriction and use an Evaluate visitor
                            // to evaluate the argument, since we know for sure that it's a string.
                            var identifier = callNode.Arguments[0].ToString();
                            if (JSScanner.IsValidIdentifier(identifier))
                            {
                                // Add XXXX to the known globals if it isn't already a known item.
                                scope.AddGlobal(identifier);
                            }
                        }
                    }
                }
                else if (settings.EvalTreatment != EvalTreatment.Ignore
                    && string.CompareOrdinal(lookup.Name, "eval") == 0)
                {
                    // it's an eval -- but are we calling it?
                    // TODO: what if we are assigning it to a variable? Should we track that variable and see if we call it?
                    // What about passing it as a parameter to a function? track that as well in case the function calls it?
                    var parentCall = lookup.Parent as CallNode;
                    if (parentCall != null && parentCall.Function == lookup)
                    {
                        scope.IsKnownAtCompileTime = false;
                    }
                }
            }

            // add the reference
            lookup.VariableField.AddReference(lookup);

            // we are actually referencing this field, so it's no longer a placeholder field if it
            // happens to have been one.
            lookup.VariableField.IsPlaceholder = false;
        }

        private static void AddGhostedFields(ActivationObject scope)
        {
            foreach (var catchParameter in scope.GhostedCatchParameters)
            {
                ResolveGhostedCatchParameter(scope, catchParameter);
            }

            foreach (var ghostFunc in scope.GhostedFunctions)
            {
                ResolveGhostedFunctions(scope, ghostFunc);
            }

            // recurse
            foreach (var childScope in scope.ChildScopes)
            {
                AddGhostedFields(childScope);
            }
        }

        private static void ResolveGhostedCatchParameter(ActivationObject scope, ParameterDeclaration catchParameter)
        {
            // check to see if the name exists in the outer variable scope.
            var ghostField = scope[catchParameter.Name];
            if (ghostField == null)
            {
                // set up a ghost field to keep track of the relationship
                ghostField = new JSVariableField(FieldType.GhostCatch, catchParameter.Name, 0, null)
                {
                    OriginalContext = catchParameter.Context.Clone()
                };

                scope.AddField(ghostField);
            }
            else if (ghostField.FieldType == FieldType.GhostCatch)
            {
                // there is, but it's another ghost catch variable. That's fine; just use it.
                // don't even flag it as ambiguous because if someone is later referencing the error variable
                // used in a couple catch variables, we'll say something then because other browsers will have that
                // variable undefined or from an outer scope.
            }
            else
            {
                // there is, and it's NOT another ghosted catch variable. Possible naming
                // collision in IE -- if an error happens, it will clobber the existing field's value,
                // although that MAY be the intention; we don't know for sure. But it IS a cross-
                // browser behavior difference.
                ghostField.IsAmbiguous = true;

                if (ghostField.OuterField != null)
                {
                    // and to make matters worse, it's actually bound to an OUTER field
                    // in modern browsers, but will bind to this catch variable in older
                    // versions of IE! Definitely a cross-browser difference!
                    // throw a cross-browser issue error.
                    catchParameter.Context.HandleError(JSError.AmbiguousCatchVar);
                }
            }

            // link them so they all keep the same name going forward
            // (since they are named the same in the sources)
            catchParameter.VariableField.OuterField = ghostField;

            // TODO: this really should be a LIST of ghosted fields, since multiple 
            // elements can ghost to the same field.
            ghostField.GhostedField = catchParameter.VariableField;

            // if the actual field has references, we want to bubble those up
            // since we're now linking those fields
            if (catchParameter.VariableField.RefCount > 0)
            {
                // add the catch parameter's references to the ghost field
                ghostField.AddReferences(catchParameter.VariableField.References);
            }
        }

        private static void ResolveGhostedFunctions(ActivationObject scope, FunctionObject funcObject)
        {
            var functionField = funcObject.VariableField;

            // let's check on ghosted names in the outer variable scope
            var ghostField = scope[funcObject.Name];
            if (ghostField == null)
            {
                // nothing; good to go. Add a ghosted field to keep track of it.
                ghostField = new JSVariableField(FieldType.GhostFunction, funcObject.Name, 0, funcObject)
                {
                    OriginalContext = functionField.OriginalContext.Clone(),
                    CanCrunch = funcObject.VariableField.IfNotNull(v => v.CanCrunch)
                };

                scope.AddField(ghostField);
            }
            else if (ghostField.FieldType == FieldType.GhostFunction)
            {
                // there is, but it's another ghosted function expression.
                // what if a lookup is resolved to this field later? We probably still need to
                // at least flag it as ambiguous. We will only need to throw an error, though,
                // if someone actually references the outer ghost variable. 
                ghostField.IsAmbiguous = true;
            }
            else
            {
                // something already exists. Could be a naming collision for IE or at least a
                // a cross-browser behavior difference if it's not coded properly.
                // mark this field as a function, even if it wasn't before
                ghostField.IsFunction = true;

                if (ghostField.OuterField != null)
                {
                    // if the pre-existing field we are ghosting is a reference to
                    // an OUTER field, then we actually have a problem that creates a BIG
                    // difference between older IE browsers and everything else.
                    // modern browsers will have the link to the outer field, but older
                    // IE browsers will link to this function expression!
                    // fire a cross-browser error warning
                    ghostField.IsAmbiguous = true;
                    funcObject.IdContext.HandleError(JSError.AmbiguousNamedFunctionExpression);
                }
                else if (ghostField.IsReferenced)
                {
                    // if the ghosted field isn't even referenced, then who cares?
                    // but it is referenced. Let's see if it matters.
                    // something like: var nm = function nm() {}
                    // is TOTALLY cool common cross-browser syntax.
                    var parentVarDecl = funcObject.Parent as VariableDeclaration;
                    if (parentVarDecl == null
                        || parentVarDecl.Name != funcObject.Name)
                    {
                        // see if it's a simple assignment.
                        // something like: var nm; nm = function nm(){},
                        // would also be cool, although less-common than the vardecl version.
                        Lookup lookup;
                        var parentAssignment = funcObject.Parent as BinaryOperator;
                        if (parentAssignment == null || parentAssignment.OperatorToken != JSToken.Assign
                            || parentAssignment.Operand2 != funcObject
                            || (lookup = parentAssignment.Operand1 as Lookup) == null
                            || lookup.Name != funcObject.Name)
                        {
                            // something else. Flag it as ambiguous.
                            ghostField.IsAmbiguous = true;
                        }
                    }
                }
            }

            // link them so they all keep the same name going forward
            // (since they are named the same in the sources)
            functionField.OuterField = ghostField;

            // TODO: this really should be a LIST of ghosted fields, since multiple 
            // elements can ghost to the same field.
            ghostField.GhostedField = functionField;

            // if the actual field has references, we want to bubble those up
            // since we're now linking those fields
            if (functionField.RefCount > 0)
            {
                // add the function's references to the ghost field
                ghostField.AddReferences(functionField.References);
            }
        }

        #endregion

        #region IVisitor Members

        public void Visit(ArrayLiteral node)
        {
            if (node != null)
            {
                if (node.Elements != null)
                {
                    node.Elements.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(AspNetBlockNode node)
        {
            // nothing to do
        }

        public void Visit(AstNodeList node)
        {
            if (node != null)
            {
                // don't bother setting the order of the list itself, just the items
                for (var ndx = 0; ndx < node.Count; ++ndx)
                {
                    var item = node[ndx];
                    if (item != null)
                    {
                        item.Accept(this);
                    }
                }
            }
        }

        public void Visit(BinaryOperator node)
        {
            if (node != null)
            {
                if (node.Operand1 != null)
                {
                    node.Operand1.Accept(this);
                }

                if (node.Operand2 != null)
                {
                    node.Operand2.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(Block node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.BlockScope == null
                    && node.Parent != null
                    && !(node.Parent is SwitchCase)
                    && !(node.Parent is FunctionObject)
                    && !(node.Parent is ConditionalCompilationComment))
                {
                    node.BlockScope = new BlockScope(CurrentLexicalScope, node.Context, m_settings)
                    {
                        IsInWithScope = m_withDepth > 0
                    };
                }

                if (node.BlockScope != null)
                {
                    m_lexicalStack.Push(node.BlockScope);
                }

                try
                {
                    // recurse the block statements
                    for (var ndx = 0; ndx < node.Count; ++ndx)
                    {
                        var statement = node[ndx];
                        if (statement != null)
                        {
                            statement.Accept(this);
                        }
                    }
                }
                finally
                {
                    // be sure to reset the unreachable flag when we exit this block
                    m_isUnreachable = false;

                    if (node.BlockScope != null)
                    {
                        Debug.Assert(CurrentLexicalScope == node.BlockScope);
                        m_lexicalStack.Pop();
                    }
                }

                // now, if the block has no lex-decls, we really don't need a separate scope.
                if (node.BlockScope != null
                    && !(node.BlockScope is WithScope)
                    && !(node.BlockScope is CatchScope)
                    && node.BlockScope.LexicallyDeclaredNames.Count == 0)
                {
                    CollapseBlockScope(node.BlockScope);
                    node.BlockScope = null;
                }
            }
        }

        public void Visit(Break node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                // we can stop marking order for subsequent statements in this block,
                // since this stops execution
                m_isUnreachable = true;
            }
        }

        public void Visit(CallNode node)
        {
            if (node != null)
            {
                if (node.Function != null)
                {
                    node.Function.Accept(this);
                }

                if (node.Arguments != null)
                {
                    node.Arguments.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ConditionalCompilationComment node)
        {
            if (node != null)
            {
                if (node.Statements != null)
                {
                    node.Statements.Accept(this);
                }
            }
        }

        public void Visit(ConditionalCompilationElse node)
        {
            // nothing to do
        }

        public void Visit(ConditionalCompilationElseIf node)
        {
            // nothing to do
        }

        public void Visit(ConditionalCompilationEnd node)
        {
            // nothing to do
        }

        public void Visit(ConditionalCompilationIf node)
        {
            // nothing to do
        }

        public void Visit(ConditionalCompilationOn node)
        {
            // nothing to do
        }

        public void Visit(ConditionalCompilationSet node)
        {
            // nothing to do
        }

        public void Visit(Conditional node)
        {
            if (node != null)
            {
                if (node.Condition != null)
                {
                    node.Condition.Accept(this);
                }

                var startingIndex = m_orderIndex;
                if (node.TrueExpression != null)
                {
                    node.TrueExpression.Accept(this);
                }

                var trueEndIndex = m_orderIndex;
                m_orderIndex = startingIndex;

                if (node.FalseExpression != null)
                {
                    node.FalseExpression.Accept(this);
                }

                m_orderIndex = Math.Max(trueEndIndex, m_orderIndex);
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ConstantWrapper node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ConstantWrapperPP node)
        {
            // nothing to do
        }

        public void Visit(ConstStatement node)
        {
            if (node != null)
            {
                // declarations get -1 position
                node.Index = -1;

                // the statement itself doesn't get executed, but the initializers do
                for (var ndx = 0; ndx < node.Count; ++ndx)
                {
                    var item = node[ndx];
                    if (item != null)
                    {
                        item.Accept(this);
                    }
                }
            }
        }

        public void Visit(ContinueNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                // we can stop marking order for subsequent statements in this block,
                // since this stops execution
                m_isUnreachable = true;
            }
        }

        public void Visit(CustomNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(DebuggerNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(DirectivePrologue node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.UseStrict)
                {
                    CurrentVariableScope.UseStrict = true;
                }
            }
        }

        public void Visit(DoWhile node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.Body != null)
                {
                    node.Body.Accept(this);
                }

                if (node.Condition != null)
                {
                    node.Condition.Accept(this);
                }
            }
        }

        public void Visit(EmptyStatement node)
        {
            // nothing to do
        }

        public void Visit(ForIn node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                if (node.Collection != null)
                {
                    node.Collection.Accept(this);
                }

                if (node.Variable != null)
                {
                    // if the variable portion of the for-in statement is a lexical
                    // declaration, then we will create the block node for its body right now
                    // and add the declaration. This will prevent the body from deleting
                    // an empty lexical scope.
                    var lexDeclaration = node.Variable as LexicalDeclaration;
                    if (lexDeclaration != null)
                    {
                        // create the scope on the block
                        node.BlockScope = new BlockScope(CurrentLexicalScope, node.Context, m_settings)
                        {
                            IsInWithScope = m_withDepth > 0
                        };
                        m_lexicalStack.Push(node.BlockScope);
                    }
                }

                try
                {
                    if (node.Variable != null)
                    {
                        node.Variable.Accept(this);
                    }

                    if (node.Body != null)
                    {
                        node.Body.Accept(this);
                    }
                }
                finally
                {
                    if (node.BlockScope != null)
                    {
                        Debug.Assert(CurrentLexicalScope == node.BlockScope);
                        m_lexicalStack.Pop();
                    }
                }
            }
        }

        public void Visit(ForNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                if (node.Initializer != null)
                {
                    // if the variable portion of the for-in statement is a lexical
                    // declaration, then we will create the block node for its body right now
                    // and add the declaration. This will prevent the body from both creating
                    // a new lexical scope and from deleting an empty one.
                    var lexDeclaration = node.Initializer as LexicalDeclaration;
                    if (lexDeclaration != null)
                    {
                        // create the scope on the block
                        node.BlockScope = new BlockScope(CurrentLexicalScope, node.Context, m_settings)
                        {
                            IsInWithScope = m_withDepth > 0
                        };
                        m_lexicalStack.Push(node.BlockScope);
                    }
                }

                try
                {
                    if (node.Initializer != null)
                    {
                        node.Initializer.Accept(this);
                    }

                    if (node.Condition != null)
                    {
                        node.Condition.Accept(this);
                    }

                    if (node.Body != null)
                    {
                        node.Body.Accept(this);
                    }

                    if (node.Incrementer != null)
                    {
                        node.Incrementer.Accept(this);
                    }
                }
                finally
                {
                    if (node.BlockScope != null)
                    {
                        Debug.Assert(CurrentLexicalScope == node.BlockScope);
                        m_lexicalStack.Pop();
                    }
                }
            }
        }

        public void Visit(FunctionObject node)
        {
            if (node != null)
            {
                // it's a declaration; put the index to -1.
                node.Index = -1;

                // create a function scope, assign it to the function object,
                // and push it on the stack
                var parentScope = CurrentLexicalScope;
                if (node.FunctionType == FunctionType.Expression 
                    && !string.IsNullOrEmpty(node.Name))
                {
                    // function expressions have an intermediate scope between the parent and the
                    // function's scope that contains just the function name so the function can
                    // be self-referencing without the function expression polluting the parent scope.
                    // don't add the function name field yet, because it's not a decl per se.
                    parentScope = new FunctionScope(parentScope, true, m_settings, node)
                    {
                        IsInWithScope = m_withDepth > 0
                    };

                    // add this function object to the list of function objects the variable scope
                    // will need to ghost later
                    CurrentVariableScope.GhostedFunctions.Add(node);
                }

                node.FunctionScope = new FunctionScope(parentScope, node.FunctionType != FunctionType.Declaration, m_settings, node)
                {
                    IsInWithScope = m_withDepth > 0
                };
                m_lexicalStack.Push(node.FunctionScope);
                m_variableStack.Push(node.FunctionScope);

                var savedIndex = m_orderIndex;
                try
                {
                    // recurse into the function to handle it after saving the current index and resetting it
                    if (node.Body != null)
                    {
                        m_orderIndex = 0;
                        node.Body.Accept(this);
                    }
                }
                finally
                {
                    Debug.Assert(CurrentLexicalScope == node.FunctionScope);
                    m_lexicalStack.Pop();
                    m_variableStack.Pop();

                    m_orderIndex = savedIndex;
                }

                // nothing to add to the var-decl list.
                // but add the function name to the current lex-decl list
                // IF it is a declaration and it has a name (and it SHOULD unless there was an error)
                if (node.FunctionType == FunctionType.Declaration && 
                    !string.IsNullOrEmpty(node.Name))
                {
                    var lexicalScope = CurrentLexicalScope;
                    lexicalScope.LexicallyDeclaredNames.Add(node);

                    if (lexicalScope != CurrentVariableScope)
                    {
                        // the current lexical scope is the variable scope.
                        // this is ES6 syntax: a function declaration inside a block scope. Not allowed
                        // in ES5 code, so throw a warning and ghost this function in the outer variable scope 
                        // to make sure that we don't generate any naming collisions.
                        node.NameContext.HandleError(JSError.MisplacedFunctionDeclaration, false);
                        CurrentVariableScope.GhostedFunctions.Add(node);
                    }
                }
            }
        }

        public void Visit(GetterSetter node)
        {
            // nothing to do
        }

        public void Visit(GroupingOperator node)
        {
            if (node != null)
            {
                if (node.Operand != null)
                {
                    node.Operand.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(IfNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.Condition != null)
                {
                    node.Condition.Accept(this);
                }

                // make true and false block numbered from the same starting point?
                var startingPoint = m_orderIndex;
                if (node.TrueBlock != null)
                {
                    node.TrueBlock.Accept(this);
                }

                var trueStop = m_orderIndex;
                m_orderIndex = startingPoint;

                if (node.FalseBlock != null)
                {
                    node.FalseBlock.Accept(this);
                }

                // and keep counting from the farthest point
                m_orderIndex = Math.Max(trueStop, m_orderIndex);
            }
        }

        public void Visit(ImportantComment node)
        {
            // nothing to do
        }

        public void Visit(LabeledStatement node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.Statement != null)
                {
                    node.Statement.Accept(this);
                }
            }
        }

        public void Visit(LexicalDeclaration node)
        {
            if (node != null)
            {
                // lexical declarations get an order index
                node.Index = NextOrderIndex;
                for (var ndx = 0; ndx < node.Count; ++ndx)
                {
                    var decl = node[ndx];
                    if (decl != null)
                    {
                        decl.Accept(this);
                    }
                }
            }
        }

        public void Visit(Lookup node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                // add the lookup node to the current lexical scope, because
                // that's the starting point for this node's lookup resolution.
                CurrentLexicalScope.ScopeLookups.Add(node);
            }
        }

        public void Visit(Member node)
        {
            if (node != null)
            {
                if (node.Root != null)
                {
                    node.Root.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ObjectLiteral node)
        {
            if (node != null)
            {
                node.Properties.Accept(this);
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ObjectLiteralField node)
        {
            // nothing to do
        }

        public void Visit(ObjectLiteralProperty node)
        {
            if (node != null)
            {
                // don't care about the property names; just recurse the values
                if (node.Value != null)
                {
                    node.Value.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ParameterDeclaration node)
        {
            // nothing to do
        }

        public void Visit(RegExpLiteral node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ReturnNode node)
        {
            if (node != null)
            {
                if (node.Operand != null)
                {
                    node.Operand.Accept(this);
                }

                node.Index = NextOrderIndex;

                // we can stop marking order for subsequent statements in this block,
                // since this stops execution
                m_isUnreachable = true;
            }
        }

        public void Visit(Switch node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.Expression != null)
                {
                    node.Expression.Accept(this);
                }

                // the switch has its own block scope to use for all the blocks that are under
                // its child switch-case nodes
                node.BlockScope = new BlockScope(CurrentLexicalScope, node.Context, m_settings)
                {
                    IsInWithScope = m_withDepth > 0
                };
                m_lexicalStack.Push(node.BlockScope);

                try
                {
                    if (node.Cases != null)
                    {
                        node.Cases.Accept(this);
                    }
                }
                finally
                {
                    Debug.Assert(CurrentLexicalScope == node.BlockScope);
                    m_lexicalStack.Pop();
                }

                // if the block has no lex-decls, we really don't need a separate scope.
                if (node.BlockScope.LexicallyDeclaredNames.Count == 0)
                {
                    CollapseBlockScope(node.BlockScope);
                    node.BlockScope = null;
                }
            }
        }

        public void Visit(SwitchCase node)
        {
            if (node != null)
            {
                if (node.CaseValue != null)
                {
                    node.CaseValue.Accept(this);
                }

                if (node.Statements != null)
                {
                    node.Statements.Accept(this);
                }
            }
        }

        public void Visit(ThisLiteral node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
            }
        }

        public void Visit(ThrowNode node)
        {
            if (node != null)
            {
                if (node.Operand != null)
                {
                    node.Operand.Accept(this);
                }

                node.Index = NextOrderIndex;

                // we can stop marking order for subsequent statements in this block,
                // since this stops execution
                m_isUnreachable = true;
            }
        }

        public void Visit(TryNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;

                if (node.TryBlock != null)
                {
                    node.TryBlock.Accept(this);
                }

                // add this catch parameter to the list of catch parameters the variable
                // scope will need to ghost later.
                if (node.CatchParameter != null)
                {
                    CurrentVariableScope.GhostedCatchParameters.Add(node.CatchParameter);
                }

                if (node.CatchBlock != null)
                {
                    // create the catch-scope, add the catch parameter to it, and recurse the catch block.
                    // the block itself will push the scope onto the stack and pop it off, so we don't have to.
                    node.CatchBlock.BlockScope = new CatchScope(CurrentLexicalScope, node.CatchBlock.Context, m_settings, node.CatchParameter)
                    {
                        IsInWithScope = m_withDepth > 0
                    };
                    node.CatchBlock.BlockScope.LexicallyDeclaredNames.Add(node.CatchParameter);
                    node.CatchBlock.Accept(this);
                }

                if (node.FinallyBlock != null)
                {
                    node.FinallyBlock.Accept(this);
                }
            }
        }

        public void Visit(UnaryOperator node)
        {
            if (node != null)
            {
                if (node.Operand != null)
                {
                    node.Operand.Accept(this);
                }

                node.Index = NextOrderIndex;
            }
        }

        public void Visit(Var node)
        {
            if (node != null)
            {
                // declarations get a -1 position index
                node.Index = -1;

                for (var ndx = 0; ndx < node.Count; ++ndx)
                {
                    var decl = node[ndx];
                    if (decl != null)
                    {
                        decl.Accept(this);
                    }
                }
            }
        }

        public void Visit(VariableDeclaration node)
        {
            if (node != null)
            {
                if (node.Parent is LexicalDeclaration)
                {
                    // ES6 let or const declaration. Only add to the current lexical scope.
                    CurrentLexicalScope.LexicallyDeclaredNames.Add(node);
                }
                else
                {
                    // must be var or const (mozilla-style). Add to both the lexical scope
                    // and the variable scope. The variable scope will actually use this node
                    // to create a field; the lexical stack will just use it to detect conflicts
                    // with lex-decls
                    CurrentLexicalScope.VarDeclaredNames.Add(node);
                    CurrentVariableScope.VarDeclaredNames.Add(node);
                }

                if (node.Initializer != null)
                {
                    // recurse the initializer
                    node.Initializer.Accept(this);
                    node.Index = NextOrderIndex;
                }
                else
                {
                    // no initializer; give it an index of -1 because it doesn't actually
                    // do anything at execution time.
                    node.Index = -1;
                }
            }
        }

        public void Visit(WhileNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.Condition != null)
                {
                    node.Condition.Accept(this);
                }

                if (node.Body != null)
                {
                    node.Body.Accept(this);
                }
            }
        }

        public void Visit(WithNode node)
        {
            if (node != null)
            {
                node.Index = NextOrderIndex;
                if (node.WithObject != null)
                {
                    node.WithObject.Accept(this);
                }

                if (node.Body != null)
                {
                    // create the with-scope and recurse the block.
                    // the block itself will push the scope onto the stack and pop it off, so we don't have to.
                    node.Body.BlockScope = new WithScope(CurrentLexicalScope, node.Body.Context, m_settings);

                    try
                    {
                        ++m_withDepth;
                        node.Body.Accept(this);
                    }
                    finally
                    {
                        --m_withDepth;
                    }
                }
            }
        }

        #endregion
    }
}
