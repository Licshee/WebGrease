// AnalyzeNodeVisitor.cs
//
// Copyright 2011 Microsoft Corporation
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
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Ajax.Utilities
{
    internal class AnalyzeNodeVisitor : TreeVisitor
    {
        private JSParser m_parser;
        private uint m_uniqueNumber;// = 0;
        private bool m_encounteredCCOn;// = false;
        private MatchPropertiesVisitor m_matchVisitor;// == null;

        private Stack<ActivationObject> ScopeStack { get { return m_parser.ScopeStack; } }

        private uint UniqueNumber
        {
            get
            {
                lock (this)
                {
                    // we'll want to roll over if for some reason we ever hit the max
                    if (m_uniqueNumber == int.MaxValue)
                    {
                        m_uniqueNumber = 0;
                    }
                    return m_uniqueNumber++;
                }
            }
        }

        public AnalyzeNodeVisitor(JSParser parser)
        {
            m_parser = parser;
        }

        public override void Visit(BinaryOperator node)
        {
            if (node != null)
            {
                base.Visit(node);

                // see if this operation is subtracting zero from a lookup -- that is typically done to
                // coerce a value to numeric. There's a simpler way: unary plus operator.
                if (node.OperatorToken == JSToken.Minus
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.SimplifyStringToNumericConversion))
                {
                    Lookup lookup = node.Operand1 as Lookup;
                    if (lookup != null)
                    {
                        ConstantWrapper right = node.Operand2 as ConstantWrapper;
                        if (right != null && right.IsIntegerLiteral && right.ToNumber() == 0)
                        {
                            // okay, so we have "lookup - 0"
                            // this is done frequently to force a value to be numeric. 
                            // There is an easier way: apply the unary + operator to it. 
                            var unary = new UnaryOperator(node.Context, m_parser, lookup, JSToken.Plus, false);
                            node.Parent.ReplaceChild(node, unary);

                            // because we recursed at the top of this function, we don't need to Analyze
                            // the new Unary node. This visitor's method for UnaryOperator only does something
                            // if the operand is a constant -- and this one is a Lookup. And we already analyzed
                            // the lookup.
                        }
                    }
                }
                else if ((node.OperatorToken == JSToken.StrictEqual || node.OperatorToken == JSToken.StrictNotEqual)
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.ReduceStrictOperatorIfTypesAreSame))
                {
                    PrimitiveType leftType = node.Operand1.FindPrimitiveType();
                    if (leftType != PrimitiveType.Other)
                    {
                        PrimitiveType rightType = node.Operand2.FindPrimitiveType();
                        if (leftType == rightType)
                        {
                            // the are the same known types. We can reduce the operators
                            node.OperatorToken = node.OperatorToken == JSToken.StrictEqual ? JSToken.Equal : JSToken.NotEqual;
                        }
                        else if (rightType != PrimitiveType.Other)
                        {
                            // they are not the same, but they are both known. We can completely remove the operator
                            // and replace it with true (!==) or false (===).
                            node.Context.HandleError(JSError.StrictComparisonIsAlwaysTrueOrFalse, false);
                            node.Parent.ReplaceChild(
                                node,
                                new ConstantWrapper(node.OperatorToken == JSToken.StrictNotEqual, PrimitiveType.Boolean, node.Context, m_parser));
                        }
                    }
                }
                else if (node.IsAssign)
                {
                    var lookup = node.Operand1 as Lookup;
                    if (lookup != null)
                    {
                        if (lookup.VariableField != null && lookup.VariableField.InitializationOnly)
                        {
                            // the field is an initialization-only field -- we should NOT be assigning to it
                            lookup.Context.HandleError(JSError.AssignmentToConstant, true);
                        }
                        else if (ScopeStack.Peek().UseStrict)
                        {
                            if (lookup.VariableField == null)
                            {
                                // strict mode cannot assign to undefined fields
                                node.Operand1.Context.HandleError(JSError.StrictModeUndefinedVariable, true);
                            }
                            else if(lookup.VariableField.FieldType == FieldType.Arguments
                                || (lookup.VariableField.FieldType == FieldType.Predefined && string.CompareOrdinal(lookup.Name, "eval") == 0))
                            {
                                // strict mode cannot assign to lookup "eval" or "arguments"
                                node.Operand1.Context.HandleError(JSError.StrictModeInvalidAssign, true);
                            }
                        }
                    }
                }
                else if ((node.Parent is Block || (node.Parent is CommaOperator && node.Parent.Parent is Block))
                    && (node.OperatorToken == JSToken.LogicalOr || node.OperatorToken == JSToken.LogicalAnd))
                {
                    // this is an expression statement where the operator is || or && -- basically
                    // it's a shortcut for an if-statement:
                    // expr1&&expr2; ==> if(expr1)expr2;
                    // expr1||expr2; ==> if(!expr1)expr2;
                    // let's check to see if the not of expr1 is smaller. If so, we can not the expression
                    // and change the operator
                    var logicalNot = new LogicalNot(node.Operand1, node.Parser);
                    if (logicalNot.Measure() < 0)
                    {
                        // it would be smaller! Change it.
                        // transform: expr1&&expr2 => !expr1||expr2
                        // transform: expr1||expr2 => !expr1&&expr2
                        logicalNot.Apply();
                        node.OperatorToken = node.OperatorToken == JSToken.LogicalAnd ? JSToken.LogicalOr : JSToken.LogicalAnd;
                    }
                }
            }
        }

        private void CombineExpressions(Block node)
        {
            // walk backwards because we'll be removing items as we go along.
            // and don't bother looking at the first element, because we'll be attempting to combine
            // the current element with the previous element -- and the first element (0) has no
            // previous element.
            // we will check for:
            //      1) previous=expr1; this=expr2           ==> expr1,expr2
            //      2) previous=expr1; this=for(;...)       ==> for(expr1;...)
            //      3) previous=expr1; this=for(expr2;...)  ==> for(expr1,expr2;...)
            //      4) previous=expr1; this=return expr2    ==> return expr1,expr2
            //      5) previous=expr1; this=if(cond)...     ==> if(expr1,cond)...
            //      6) previous=expr1; this=while(cond)...  ==> for(expr;cond;)...
            for (var ndx = node.Count - 1; ndx > 0; --ndx)
            {
                // see if the previous statement is an expression
                if (node[ndx - 1].IsExpression)
                {
                    IfNode ifNode;
                    ForNode forNode;
                    WhileNode whileNode;
                    ReturnNode returnNode;
                    if (node[ndx].IsExpression)
                    {
                        // transform: expr1;expr2 to expr1,expr2
                        // use the special comma operator object so we can handle it special
                        // and don't create stack-breakingly deep trees
                        var binOp = new CommaOperator(node[ndx - 1].Context.Clone().CombineWith(node[ndx].Context),
                            m_parser,
                            node[ndx - 1],
                            node[ndx]);

                        // replace the current node and delete the previous
                        if (node.ReplaceChild(node[ndx], binOp))
                        {
                            node.ReplaceChild(node[ndx - 1], null);
                        }
                    }
                    else if ((returnNode = node[ndx] as ReturnNode) != null)
                    {
                        // see if the return node has an expression operand
                        if (returnNode.Operand != null && returnNode.Operand.IsExpression)
                        {
                            // check for expr1[ASSIGN]expr2;return expr1 and replace with return expr1[ASSIGN]expr2
                            var beforeExpr = node[ndx - 1] as BinaryOperator;
                            if (beforeExpr != null && beforeExpr.IsAssign
                                && beforeExpr.Operand1.IsEquivalentTo(returnNode.Operand))
                            {
                                // tranaform: expr1[ASSIGN]expr2;return expr1 and replace with return expr1[ASSIGN]expr2
                                // replace the operand on the return node with the previous expression and
                                // delete the previous node
                                if (returnNode.ReplaceChild(returnNode.Operand, beforeExpr))
                                {
                                    node.ReplaceChild(node[ndx - 1], null);
                                }
                            }
                            else
                            {
                                // transform: expr1;return expr2 to return expr1,expr2
                                var binOp = new CommaOperator(null,
                                    m_parser,
                                    node[ndx - 1],
                                    returnNode.Operand);

                                // replace the operand on the return node with the new expression and
                                // delete the previous node
                                if (returnNode.ReplaceChild(returnNode.Operand, binOp))
                                {
                                    node.ReplaceChild(node[ndx - 1], null);
                                }
                            }
                        }
                    }
                    else if ((forNode = node[ndx] as ForNode) != null)
                    {
                        // if we aren't allowing in-operators to be moved into for-statements, then
                        // first check to see if that previous expression statement is free of in-operators
                        // before trying to move it.
                        if (m_parser.Settings.IsModificationAllowed(TreeModifications.MoveInExpressionsIntoForStatement)
                            || !node[ndx - 1].ContainsInOperator)
                        {
                            if (forNode.Initializer == null)
                            {
                                // transform: expr1;for(;...) to for(expr1;...)
                                // simply move the previous expression to the for-statement's initializer
                                forNode.SetInitializer(node[ndx - 1]);
                                node.ReplaceChild(node[ndx - 1], null);
                            }
                            else if (forNode.Initializer.IsExpression)
                            {
                                // transform: expr1;for(expr2;...) to for(expr1,expr2;...)
                                var binOp = new CommaOperator(null,
                                    m_parser,
                                    node[ndx - 1],
                                    forNode.Initializer);

                                // replace the initializer with the new binary operator and remove the previous node
                                if (forNode.ReplaceChild(forNode.Initializer, binOp))
                                {
                                    node.ReplaceChild(node[ndx - 1], null);
                                }
                            }
                        }
                    }
                    else if ((ifNode = node[ndx] as IfNode) != null)
                    {
                        // transform: expr;if(cond)... => if(expr,cond)...
                        // combine the previous expression with the if-condition via comma, then delete
                        // the previous statement.
                        ifNode.ReplaceChild(ifNode.Condition,
                            new CommaOperator(null, m_parser, node[ndx - 1], ifNode.Condition));
                        node.RemoveAt(ndx - 1);
                    }
                    else if ((whileNode = node[ndx] as WhileNode) != null
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.ChangeWhileToFor))
                    {
                        // transform: expr;while(cond)... => for(expr;cond;)...
                        // zero-sum, and maybe a little worse for performance because of the nop iterator,
                        // but combines two statements into one, which may have savings later on.
                        var initializer = node[ndx - 1];
                        node.RemoveAt(ndx - 1);
                        node.ReplaceChild(whileNode, new ForNode(
                            null,
                            m_parser,
                            initializer,
                            whileNode.Condition,
                            null,
                            whileNode.Body));
                    }
                }
            }
        }

        private static AstNode FindLastStatement(Block node)
        {
            // start with the last statement in the block and back up over any function declarations
            // or important comments until we get the last statement
            var lastStatementIndex = node.Count - 1;
            while (lastStatementIndex >= 0 
                && (node[lastStatementIndex] is FunctionObject || node[lastStatementIndex] is ImportantComment))
            {
                --lastStatementIndex;
            }

            return lastStatementIndex >= 0 ? node[lastStatementIndex] : null;
        }

        public override void Visit(Block node)
        {
            if (node != null)
            {
                // we might things differently if these statements are the body collection for a function
                // because we can assume the implicit return statement at the end of it
                bool isFunctionLevel = (node.Parent is FunctionObject);

                // if we want to remove debug statements...
                if (m_parser.Settings.StripDebugStatements && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements))
                {
                    // do it now before we try doing other things
                    StripDebugStatements(node);
                }

                // analyze all the statements in our block and recurse them
                if (node.BlockScope != null)
                {
                    ScopeStack.Push(node.BlockScope);
                }
                try
                {
                    // call the base class to recurse
                    base.Visit(node);
                }
                finally
                {
                    if (node.BlockScope != null)
                    {
                        ScopeStack.Pop();
                    }
                }

                if (m_parser.Settings.RemoveUnneededCode)
                {
                    // go forward, and check the count each iteration because we might be ADDING statements to the block.
                    // let's look at all our if-statements. If a true-clause ends in a return, then we don't
                    // need the else-clause; we can pull its statements out and stick them after the if-statement.
                    // also, if we encounter a return-, break- or continue-statement, we can axe everything after it
                    for (var ndx = 0; ndx < node.Count; ++ndx)
                    {
                        // see if it's an if-statement with both a true and a false block
                        var ifNode = node[ndx] as IfNode;
                        if (ifNode != null
                            && ifNode.TrueBlock != null
                            && ifNode.TrueBlock.Count > 0
                            && ifNode.FalseBlock != null)
                        {
                            // now check to see if the true block ends in a return statement
                            if (ifNode.TrueBlock[ifNode.TrueBlock.Count - 1] is ReturnNode)
                            {
                                // transform: if(cond){statements1;return}else{statements2} to if(cond){statements1;return}statements2
                                // it does. insert all the false-block statements after the if-statement
                                node.InsertRange(ndx + 1, ifNode.FalseBlock.Children);

                                // and then remove the false block altogether
                                ifNode.ReplaceChild(ifNode.FalseBlock, null);
                            }
                        }
                        else if (node[ndx] is ReturnNode
                            || node[ndx] is Break
                            || node[ndx] is ContinueNode)
                        {
                            // we have a return node -- no statments afterwards will be executed, so clear them out.
                            // transform: {...;return;...} to {...;return}
                            // transform: {...;break;...} to {...;break}
                            // transform: {...;continue;...} to {...;continue}
                            // we've found a return statement, and it's not the last statement in the function.
                            // walk the rest of the statements and delete anything that isn't a function declaration
                            // or a var- or const-statement.
                            for (var ndxRemove = node.Count - 1; ndxRemove > ndx; --ndxRemove)
                            {
                                var funcObject = node[ndxRemove] as FunctionObject;
                                if (funcObject == null || funcObject.FunctionType != FunctionType.Declaration)
                                {
                                    // if it's a const-statement, leave it.
                                    // we COULD check to see if the constant is referenced anywhere and delete
                                    // any that aren't. Maybe later.
                                    // we also don't want to do like the var-statements and remove the initializers.
                                    // Not sure if any browsers would fail a const WITHOUT an initializer.
                                    if (!(node[ndxRemove] is ConstStatement))
                                    {
                                        var varStatement = node[ndxRemove] as Var;
                                        if (varStatement != null)
                                        {
                                            // var statements can't be removed, but any initializers should
                                            // be deleted since they won't get executed.
                                            for (var ndxDecl = 0; ndxDecl < varStatement.Count; ++ndxDecl)
                                            {
                                                if (varStatement[ndxDecl].Initializer != null)
                                                {
                                                    varStatement[ndxDecl].ReplaceChild(varStatement[ndxDecl].Initializer, null);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // not a function declaration, and not a var statement -- get rid of it
                                            node.RemoveAt(ndxRemove);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // now check the last statement -- if it's an if-statement where the true-block is a single return
                // and there is no false block, convert this one statement to a conditional. We might back it out later
                // if we don't combine the conditional with other stuff.
                // but we can only do this if we're at the functional level because of the implied return at the end
                // of that block.
                if (isFunctionLevel && node.Count > 0
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionReturnToCondition))
                {
                    ReturnNode returnNode;
                    var ifNode = FindLastStatement(node) as IfNode;
                    if (ifNode != null && ifNode.FalseBlock == null
                        && ifNode.TrueBlock.Count == 1
                        && (returnNode = ifNode.TrueBlock[0] as ReturnNode) != null)
                    {
                        // if the return node doesn't have an operand, then we can just replace the if-statement with its conditional
                        if (returnNode.Operand == null)
                        {
                            // transform: if(cond)return;} to cond}
                            // TODO: if the condition is a constant, then eliminate it altogether
                            node.ReplaceChild(ifNode, ifNode.Condition);
                        }
                        else if (returnNode.Operand.IsExpression)
                        {
                            // transform: if(cond)return expr;} to return cond?expr:void 0}
                            var conditional = new Conditional(
                                null, m_parser, ifNode.Condition,
                                returnNode.Operand,
                                CreateVoidNode());

                            // replace the if-statement with the new return node
                            node.ReplaceChild(ifNode, new ReturnNode(ifNode.Context, m_parser, conditional));
                            Optimize(conditional);
                        }
                    }
                }

                // now walk through and combine adjacent expression statements, and adjacent var-for statements
                // and adjecent expression-return statements
                if (m_parser.Settings.IsModificationAllowed(TreeModifications.CombineAdjacentExpressionStatements))
                {
                    CombineExpressions(node);
                }

                // check to see if we want to combine a preceding var with a for-statement
                if (m_parser.Settings.IsModificationAllowed(TreeModifications.MoveVarIntoFor))
                {
                    // look at the statements in the block. 
                    // walk BACKWARDS down the list because we'll be removing items when we encounter
                    // var statements that can be moved inside a for statement's initializer
                    // we also don't need to check the first one, since there is nothing before it.
                    for (int ndx = node.Count - 1; ndx > 0; --ndx)
                    {
                        // see if the previous statement is a var statement
                        // (we've already combined adjacent var-statements)
                        ForNode forNode;
                        WhileNode whileNode;
                        var previousVar = node[ndx - 1] as Var;
                        if (previousVar != null && (forNode = node[ndx] as ForNode) != null)
                        {
                            // BUT if the var statement has any initializers containing an in-operator, first check
                            // to see if we haven't killed that move before we try moving it. Opera 11 seems to have
                            // an issue with that syntax, even if properly parenthesized.
                            if (m_parser.Settings.IsModificationAllowed(TreeModifications.MoveInExpressionsIntoForStatement)
                                || !previousVar.ContainsInOperator)
                            {
                                // and see if the forNode's initializer is empty
                                if (forNode.Initializer != null)
                                {
                                    // not empty -- see if it is a Var node
                                    Var varInitializer = forNode.Initializer as Var;
                                    if (varInitializer != null)
                                    {
                                        // transform: var decls1;for(var decls2;...) to for(var decls1,decls2;...)
                                        // we want to PREPEND the initializers in the previous var-statement
                                        // to our for-statement's initializer var-statement list
                                        varInitializer.InsertAt(0, previousVar);

                                        // then remove the previous var statement
                                        node.RemoveAt(ndx - 1);
                                        // this will bump the for node up one position in the list, so the next iteration
                                        // will be right back on this node in case there are other var statements we need
                                        // to combine
                                    }
                                    else
                                    {
                                        // we want to see if the initializer expression is a series of one or more
                                        // simple assignments to variables that are in the previous var statement.
                                        // if all the expressions are assignments to variables that are defined in the
                                        // previous var statement, then we can just move the var statement into the 
                                        // for statement.
                                        BinaryOperator binaryOp = forNode.Initializer as BinaryOperator;
                                        if (binaryOp != null && AreAssignmentsInVar(binaryOp, previousVar))
                                        {
                                            // transform: var decls;for(expr1;...) to for(var decls,expr1;...)
                                            // create a list and fill it with all the var-decls created from the assignment
                                            // operators in the expression
                                            var varDecls = new List<VariableDeclaration>();
                                            ConvertAssignmentsToVarDecls(binaryOp, varDecls, m_parser);

                                            // then go through and append each one to the var statement before us
                                            foreach (var varDecl in varDecls)
                                            {
                                                previousVar.Append(varDecl);
                                            }

                                            // move the previous var-statement into our initializer
                                            forNode.ReplaceChild(forNode.Initializer, previousVar);

                                            // and remove the previous var-statement from the list.
                                            node.RemoveAt(ndx - 1);

                                            // this will bump the for node up one position in the list, so the next iteration
                                            // will be right back on this node, but the initializer will not be null
                                        }
                                    }
                                }
                                else
                                {
                                    // transform: var decls;for(;...) to for(var decls;...)
                                    // if it's empty, then we're free to add the previous var statement
                                    // to this for statement's initializer. remove it from it's current
                                    // position and add it as the initializer
                                    node.RemoveAt(ndx - 1);
                                    forNode.SetInitializer(previousVar);
                                    // this will bump the for node up one position in the list, so the next iteration
                                    // will be right back on this node, but the initializer will not be null
                                }
                            }
                        }
                        else if (previousVar != null 
                            && (whileNode = node[ndx] as WhileNode) != null
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.ChangeWhileToFor))
                        {
                            // transform: var ...;while(cond)... => for(var ...;cond;)...
                            node.RemoveAt(ndx - 1);
                            node.ReplaceChild(whileNode, new ForNode(
                                null,
                                m_parser,
                                previousVar,
                                whileNode.Condition,
                                null,
                                whileNode.Body));
                        }
                    }
                }

                // see if the last statement is a return statement
                ReturnNode lastReturn;
                if ((lastReturn = FindLastStatement(node) as ReturnNode) != null)
                {
                    // set this flag to true if we end up adding an expression to the block.
                    // before exiting, we'll go through and combine adjacent expressions again if this
                    // flag has been set to true.
                    bool changedStatementToExpression = false;

                    // get the index of the statement before the last return
                    // (skip over function decls and importand comments)
                    var indexPrevious = PreviousStatementIndex(node, lastReturn);

                    // just out of curiosity, let's see if we fit a common pattern:
                    //      var name=expr;return name;
                    // or
                    //      const name=expr;return name;
                    // if so, we can cut out the var and simply return the expression
                    Lookup lookup;
                    if ((lookup = lastReturn.Operand as Lookup) != null && indexPrevious >= 0)
                    {
                        // use the base class for both the var- and const-statements so we will
                        // pick them both up at the same time
                        var varStatement = node[indexPrevious] as Declaration;
                        if (varStatement != null)
                        {
                            // if the last vardecl in the var statement matches the return lookup, and no
                            // other references exist for this field (refcount == 1)...
                            VariableDeclaration varDecl;
                            if ((varDecl = varStatement[varStatement.Count - 1]).Initializer != null
                                && varDecl.IsEquivalentTo(lookup)
                                && varDecl.Field.RefCount == 1)
                            {
                                if (varStatement.Count == 1)
                                {
                                    // transform: ...;var name=expr;return name} to ...;return expr}
                                    // there's only one vardecl in the var, so get rid of the entire statement
                                    lastReturn.ReplaceChild(lookup, varDecl.Initializer);
                                    node.RemoveAt(indexPrevious);
                                }
                                else
                                {
                                    // multiple vardecls are in the statement; we only need to get rid of the last one
                                    lastReturn.ReplaceChild(lookup, varDecl.Initializer);
                                    varStatement.ReplaceChild(varDecl, null);
                                }
                            }
                        }
                    }

                    // check to see if we can combine the return statement with a previous if-statement
                    // into a simple return-conditional. The true statement needs to have no false block,
                    // and only one statement in the true block.
                    Conditional conditional;
                    IfNode previousIf;
                    while (indexPrevious >= 0 
                        && lastReturn != null
                        && (previousIf = node[indexPrevious] as IfNode) != null
                        && previousIf.TrueBlock != null && previousIf.TrueBlock.Count == 1
                        && previousIf.FalseBlock == null)
                    {
                        // assume no change is made for this loop
                        bool somethingChanged = false;

                        // and that one true-block statement needs to be a return statement
                        var previousReturn = previousIf.TrueBlock[0] as ReturnNode;
                        if (previousReturn != null)
                        {
                            if (lastReturn.Operand == null)
                            {
                                if (previousReturn.Operand == null)
                                {
                                    // IF we are at the function level, then the block ends in an implicit return (undefined)
                                    // and we can change this if to just the condition. If we aren't at the function level,
                                    // then we have to leave the return, but we can replace the if with just the condition.
                                    if (!isFunctionLevel)
                                    {
                                        // transform: if(cond)return;return} to cond;return}
                                        node.ReplaceChild(previousIf, previousIf.Condition);
                                    }
                                    else
                                    {
                                        // transform: if(cond)return;return} to cond}
                                        // replace the final return with just the condition, then remove the previous if
                                        if (node.ReplaceChild(lastReturn, previousIf.Condition))
                                        {
                                            node.RemoveAt(indexPrevious);
                                            somethingChanged = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // transform: if(cond)return expr;return} to return cond?expr:void 0
                                    conditional = new Conditional(null, m_parser,
                                        previousIf.Condition,
                                        previousReturn.Operand,
                                        CreateVoidNode());

                                    // replace the final return with the new return, then delete the previous if-statement
                                    if (node.ReplaceChild(lastReturn, new ReturnNode(null, m_parser, conditional)))
                                    {
                                        node.RemoveAt(indexPrevious);
                                        Optimize(conditional);
                                        somethingChanged = true;
                                    }
                                }
                            }
                            else
                            {
                                if (previousReturn.Operand == null)
                                {
                                    // transform: if(cond)return;return expr} to return cond?void 0:expr
                                    conditional = new Conditional(null, m_parser,
                                        previousIf.Condition,
                                        CreateVoidNode(),
                                        lastReturn.Operand);

                                    // replace the final return with the new return, then delete the previous if-statement
                                    if (node.ReplaceChild(lastReturn, new ReturnNode(null, m_parser, conditional)))
                                    {
                                        node.RemoveAt(indexPrevious);
                                        Optimize(conditional);
                                        somethingChanged = true;
                                    }
                                }
                                else if (previousReturn.Operand.IsEquivalentTo(lastReturn.Operand))
                                {
                                    // transform: if(cond)return expr;return expr} to return cond,expr}
                                    // create a new binary op with the condition and the final-return operand,
                                    // replace the operand on the final-return with the new binary operator,
                                    // and then delete the previous if-statement
                                    if (lastReturn.ReplaceChild(lastReturn.Operand,
                                        new CommaOperator(null, m_parser, previousIf.Condition, lastReturn.Operand)))
                                    {
                                        node.RemoveAt(indexPrevious);
                                        somethingChanged = true;
                                    }
                                }
                                else
                                {
                                    // transform: if(cond)return expr1;return expr2} to return cond?expr1:expr2}
                                    // create a new conditional with the condition and the return operands,
                                    // replace the operand on the final-return with the new conditional operator,
                                    // and then delete the previous if-statement
                                    // transform: if(cond)return expr1;return expr2} to return cond?expr1:expr2}
                                    conditional = new Conditional(null, m_parser,
                                        previousIf.Condition, previousReturn.Operand, lastReturn.Operand);

                                    // replace the operand on the final-return with the new conditional operator,
                                    // and then delete the previous if-statement
                                    if (lastReturn.ReplaceChild(lastReturn.Operand, conditional))
                                    {
                                        node.RemoveAt(indexPrevious);
                                        Optimize(conditional);
                                        somethingChanged = true;
                                    }
                                }
                            }
                        }

                        if (!somethingChanged)
                        {
                            // nothing changed -- break out of the loop
                            break;
                        }
                        else
                        {
                            // set the flag that indicates something changed in at least one of these loops
                            changedStatementToExpression = true;
                            
                            // and since we changed something, we need to bump the index down one
                            // AFTER we grab the last return node (which has slipped into the same position
                            // as the previous node)
                            lastReturn = node[indexPrevious--] as ReturnNode;
                        }
                    }

                    // if we added any more expressions since we ran our expression-combination logic, 
                    // run it again.
                    if (changedStatementToExpression
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.CombineAdjacentExpressionStatements))
                    {
                        CombineExpressions(node);
                    }

                    // and FINALLY, we want to see if what we did previously didn't pan out and we end
                    // in something like return cond?expr:void 0, in which case we want to change it
                    // back to a simple if(condition)return expr; (saves four bytes).
                    // see if the last statement is a return statement that returns a conditional
                    if (lastReturn != null
                        && (conditional = lastReturn.Operand as Conditional) != null)
                    {
                        var unaryOperator = conditional.FalseExpression as UnaryOperator;
                        if (unaryOperator != null 
                            && unaryOperator.OperatorToken == JSToken.Void
                            && unaryOperator.Operand is ConstantWrapper)
                        {
                            unaryOperator = conditional.TrueExpression as UnaryOperator;
                            if (unaryOperator != null && unaryOperator.OperatorToken == JSToken.Void)
                            {
                                if (isFunctionLevel)
                                {
                                    // transform: ...;return cond?void 0:void 0} to ...;cond}
                                    // function level ends in an implicit "return void 0"
                                    node.ReplaceChild(lastReturn, conditional.Condition);
                                }
                                else
                                {
                                    // transform: ...;return cond?void 0:void 0} to ...;cond;return}
                                    // non-function level doesn't end in an implicit return,
                                    // so we need to break them out into two statements
                                    node.ReplaceChild(lastReturn, conditional.Condition);
                                    node.Append(new ReturnNode(null, m_parser, null));
                                }
                            }
                            else if (isFunctionLevel)
                            {
                                // transform: ...;return cond?expr:void 0} to ...;if(cond)return expr}
                                // (only works at the function-level because of the implicit return statement)
                                var ifNode = new IfNode(lastReturn.Context,
                                    m_parser,
                                    conditional.Condition,
                                    new ReturnNode(null, m_parser, conditional.TrueExpression),
                                    null);
                                node.ReplaceChild(lastReturn, ifNode);
                            }
                        }
                        else if (isFunctionLevel)
                        {
                            unaryOperator = conditional.TrueExpression as UnaryOperator;
                            if (unaryOperator != null 
                                && unaryOperator.OperatorToken == JSToken.Void
                                && unaryOperator.Operand is ConstantWrapper)
                            {
                                // transform: ...;return cond?void 0;expr} to ...;if(!cond)return expr}
                                // (only works at the function level because of the implicit return)
                                // get the logical-not of the conditional
                                var logicalNot = new LogicalNot(conditional.Condition, m_parser);
                                logicalNot.Apply();

                                // create a new if-node based on the condition, with the branches swapped 
                                // (true-expression goes to false-branch, false-expression goes to true-branch
                                var ifNode = new IfNode(lastReturn.Context,
                                    m_parser,
                                    conditional.Condition,
                                    new ReturnNode(null, m_parser, conditional.FalseExpression),
                                    null);
                                node.ReplaceChild(lastReturn, ifNode);
                            }
                        }
                    }
                }

                if (m_parser.Settings.IsModificationAllowed(TreeModifications.CombineEquivalentIfReturns))
                {
                    // walk backwards looking for if(cond1)return expr1;if(cond2)return expr2;
                    // (backwards, because we'll be combining those into one statement, reducing the number of statements.
                    // don't go all the way to zero, because each loop will compare the statement to the PREVIOUS
                    // statement, and the first statement (index==0) has no previous statement.
                    for (var ndx = node.Count - 1; ndx > 0; --ndx)
                    {
                        // see if the current statement is an if-statement with no else block, and a true
                        // block that contains a single return-statement WITH an expression.
                        AstNode matchedExpression = null;
                        AstNode condition2;
                        if (IsIfReturnExpr(node[ndx], out condition2, ref matchedExpression) != null)
                        {
                            // see if the previous statement is also the same pattern, but with
                            // the equivalent expression as its return operand
                            AstNode condition1;
                            var ifNode = IsIfReturnExpr(node[ndx - 1], out condition1, ref matchedExpression);
                            if (ifNode != null)
                            {
                                // it is a match!
                                // let's combine them -- we'll add the current condition to the
                                // previous condition with a logical-or and delete the current statement.
                                // transform: if(cond1)return expr;if(cond2)return expr; to if(cond1||cond2)return expr;
                                ifNode.ReplaceChild(ifNode.Condition,
                                    new BinaryOperator(null, m_parser, condition1, condition2, JSToken.LogicalOr));
                                node.RemoveAt(ndx);
                            }
                        }
                    }
                }

                if (isFunctionLevel
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.InvertIfReturn))
                {
                    // walk backwards looking for if (cond) return; whenever we encounter that statement,
                    // we can change it to if (!cond) and put all subsequent statements in the block inside the
                    // if's true-block.
                    for (var ndx = node.Count - 1; ndx >= 0; --ndx)
                    {
                        var ifNode = node[ndx] as IfNode;
                        if (ifNode != null
                            && ifNode.FalseBlock == null
                            && ifNode.TrueBlock != null
                            && ifNode.TrueBlock.Count == 1)
                        {
                            var returnNode = ifNode.TrueBlock[0] as ReturnNode;
                            if (returnNode != null && returnNode.Operand == null)
                            {
                                // we have if(cond)return;
                                // logical-not the condition, remove the return statement,
                                // and move all subsequent sibling statements inside the if-statement.
                                LogicalNot.Apply(ifNode.Condition, m_parser);
                                ifNode.TrueBlock.ReplaceChild(returnNode, null);

                                var ndxMove = ndx + 1;
                                if (node.Count == ndxMove + 1)
                                {
                                    // there's only one statement after our if-node.
                                    // see if it's ALSO an if-node with no else block.
                                    var secondIfNode = node[ndxMove] as IfNode;
                                    if (secondIfNode != null && (secondIfNode.FalseBlock == null || secondIfNode.FalseBlock.Count == 0))
                                    {
                                        // it is!
                                        // transform: if(cond1)return;if(cond2){...} => if(!cond1&&cond2){...}
                                        // (the cond1 is already inverted at this point)
                                        // combine cond2 with cond1 via a logical-and,
                                        // move all secondIf statements inside the if-node,
                                        // remove the secondIf node.
                                        node.RemoveAt(ndxMove);
                                        ifNode.ReplaceChild(ifNode.Condition, new BinaryOperator(
                                            null,
                                            m_parser,
                                            ifNode.Condition,
                                            secondIfNode.Condition,
                                            JSToken.LogicalAnd));

                                        ifNode.ReplaceChild(ifNode.TrueBlock, secondIfNode.TrueBlock);
                                    }
                                    else if (node[ndxMove].IsExpression
                                        && m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionCallToConditionAndCall))
                                    {
                                        // now we have if(cond)expr; optimize that!
                                        var expression = node[ndxMove];
                                        node.RemoveAt(ndxMove);
                                        IfConditionExpressionToExpression(ifNode, expression);
                                    }
                                }

                                // just move all the following statements inside the if-statement
                                while (node.Count > ndxMove)
                                {
                                    var movedNode = node[ndxMove];
                                    node.RemoveAt(ndxMove);
                                    ifNode.TrueBlock.Append(movedNode);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var isIteratorBlock = node.Parent is ForNode
                        || node.Parent is ForIn
                        || node.Parent is WhileNode
                        || node.Parent is DoWhile;

                    if (isIteratorBlock
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.InvertIfContinue))
                    {
                        // walk backwards looking for if (cond) continue; whenever we encounter that statement,
                        // we can change it to if (!cond) and put all subsequent statements in the block inside the
                        // if's true-block.
                        for (var ndx = node.Count - 1; ndx >= 0; --ndx)
                        {
                            var ifNode = node[ndx] as IfNode;
                            if (ifNode != null
                                && ifNode.FalseBlock == null
                                && ifNode.TrueBlock != null
                                && ifNode.TrueBlock.Count == 1)
                            {
                                var continueNode = ifNode.TrueBlock[0] as ContinueNode;

                                // if there's no label, then we're good. Otherwise we can only make this optimization
                                // if the label refers to the parent iterator node.
                                if (continueNode != null 
                                    && (string.IsNullOrEmpty(continueNode.Label) || (LabelMatchesParent(continueNode.Label, node.Parent))))
                                {
                                    // if this is the last statement, then we don't really need the if at all
                                    // and can just replace it with its condition
                                    if (ndx < node.Count - 1)
                                    {
                                        // we have if(cond)continue;st1;...stn;
                                        // logical-not the condition, remove the continue statement,
                                        // and move all subsequent sibling statements inside the if-statement.
                                        LogicalNot.Apply(ifNode.Condition, m_parser);
                                        ifNode.TrueBlock.ReplaceChild(continueNode, null);

                                        // TODO: if we removed a labeled continue, do we need to fix up some label references?

                                        var ndxMove = ndx + 1;
                                        if (node.Count == ndxMove + 1)
                                        {
                                            // there's only one statement after our if-node.
                                            // see if it's ALSO an if-node with no else block.
                                            var secondIfNode = node[ndxMove] as IfNode;
                                            if (secondIfNode != null && (secondIfNode.FalseBlock == null || secondIfNode.FalseBlock.Count == 0))
                                            {
                                                // it is!
                                                // transform: if(cond1)continue;if(cond2){...} => if(!cond1&&cond2){...}
                                                // (the cond1 is already inverted at this point)
                                                // combine cond2 with cond1 via a logical-and,
                                                // move all secondIf statements inside the if-node,
                                                // remove the secondIf node.
                                                ifNode.ReplaceChild(ifNode.Condition, new BinaryOperator(
                                                    null,
                                                    m_parser,
                                                    ifNode.Condition,
                                                    secondIfNode.Condition,
                                                    JSToken.LogicalAnd));

                                                ifNode.ReplaceChild(ifNode.TrueBlock, secondIfNode.TrueBlock);
                                                node.RemoveAt(ndxMove);
                                            }
                                            else if (node[ndxMove].IsExpression
                                                && m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionCallToConditionAndCall))
                                            {
                                                // now we have if(cond)expr; optimize that!
                                                var expression = node[ndxMove];
                                                node.RemoveAt(ndxMove);
                                                IfConditionExpressionToExpression(ifNode, expression);
                                            }
                                        }

                                        // just move all the following statements inside the if-statement
                                        while (node.Count > ndxMove)
                                        {
                                            var movedNode = node[ndxMove];
                                            node.RemoveAt(ndxMove);
                                            ifNode.TrueBlock.Append(movedNode);
                                        }
                                    }
                                    else
                                    {
                                        // we have if(cond)continue} -- nothing after the if.
                                        // the loop is going to continue anyway, so replace the if-statement
                                        // with the condition and be done
                                        ifNode.Parent.ReplaceChild(ifNode, ifNode.Condition);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool LabelMatchesParent(string label, AstNode parentNode)
        {
            var isMatch = false;

            // see if the parent's parent is a labeled statement
            LabeledStatement labeledStatement;
            while ((labeledStatement = parentNode.Parent as LabeledStatement) != null)
            {
                // see if the label we are looking for matches the labeled statement
                if (string.Compare(labeledStatement.Label, label, StringComparison.Ordinal) == 0)
                {
                    // it's a match -- we're done
                    isMatch = true;
                    break;
                }

                // try the next node up (a labeled statement can itself be labeled)
                parentNode = labeledStatement;
            }
            return isMatch;
        }

        private static IfNode IsIfReturnExpr(AstNode node, out AstNode condition, ref AstNode matchExpression)
        {
            // set the condition to null initially
            condition = null;

            // must be an if-node with no false block, and a true block with one statement
            var ifNode = node as IfNode;
            if (ifNode != null
                && ifNode.FalseBlock == null
                && ifNode.TrueBlock != null
                && ifNode.TrueBlock.Count == 1)
            {
                // and that one statement needs to be a return statement
                var returnNode = ifNode.TrueBlock[0] as ReturnNode;
                if (returnNode != null)
                {
                    if (matchExpression == null
                        || matchExpression.IsEquivalentTo(returnNode.Operand))
                    {
                        // either we don't care what the return expression is,
                        // or we do care and it's a match.
                        matchExpression = returnNode.Operand;
                        condition = ifNode.Condition;
                    }
                }
            }

            // but we will only return the if-node IF the matchedExpression and the
            // condition are both non-null (our TRUE state)
            return condition != null && matchExpression != null ? ifNode : null;
        }

        private static int PreviousStatementIndex(Block node, AstNode child)
        {
            // get the index of the statement before the last return
            // (skip over function decls and importand comments)
            var indexPrevious = node.IndexOf(child) - 1;
            while (indexPrevious >= 0 && (node[indexPrevious] is FunctionObject || node[indexPrevious] is ImportantComment))
            {
                --indexPrevious;
            }

            return indexPrevious;
        }

        public override void Visit(Break node)
        {
            if (node != null)
            {
                if (node.Label != null)
                {
                    // if the nest level is zero, then we might be able to remove the label altogether
                    // IF local renaming is not KeepAll AND the kill switch for removing them isn't set.
                    // the nest level will be zero if the label is undefined.
                    if (node.NestLevel == 0
                        && m_parser.Settings.LocalRenaming != LocalRenaming.KeepAll
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveUnnecessaryLabels))
                    {
                        node.Label = null;
                    }
                }

                // don't need to call the base; this statement has no children to recurse
                //base.Visit(node);
            }
        }

        public override void Visit(CallNode node)
        {
            if (node != null)
            {
                // see if this is a member (we'll need it for a couple checks)
                Member member = node.Function as Member;

                if (m_parser.Settings.StripDebugStatements
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements))
                {
                    // if this is a member, and it's a debugger object, and it's a constructor....
                    if (member != null && member.IsDebuggerStatement && node.IsConstructor)
                    {
                        // we need to replace our debugger object with a generic Object
                        node.ReplaceChild(node.Function, new Lookup("Object", node.Function.Context, m_parser));

                        // and make sure the node list is empty
                        if (node.Arguments != null && node.Arguments.Count > 0)
                        {
                            node.ReplaceChild(node.Arguments, new AstNodeList(node.Arguments.Context, m_parser));
                        }
                    }
                }

                // if this is a constructor and we want to collapse
                // some of them to literals...
                if (node.IsConstructor && m_parser.Settings.CollapseToLiteral)
                {
                    // see if this is a lookup, and if so, if it's pointing to one
                    // of the two constructors we want to collapse
                    Lookup lookup = node.Function as Lookup;
                    if (lookup != null)
                    {
                        if (lookup.Name == "Object"
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.NewObjectToObjectLiteral))
                        {
                            // no arguments -- the Object constructor with no arguments is the exact same as an empty
                            // object literal
                            if (node.Arguments == null || node.Arguments.Count == 0)
                            {
                                // replace our node with an object literal
                                ObjectLiteral objLiteral = new ObjectLiteral(node.Context, m_parser, null, null);
                                if (node.Parent.ReplaceChild(node, objLiteral))
                                {
                                    // and bail now. No need to recurse -- it's an empty literal
                                    return;
                                }
                            }
                            else if (node.Arguments.Count == 1)
                            {
                                // one argument
                                // check to see if it's an object literal.
                                ObjectLiteral objectLiteral = node.Arguments[0] as ObjectLiteral;
                                if (objectLiteral != null)
                                {
                                    // the Object constructor with an argument that is a JavaScript object merely returns the
                                    // argument. Since the argument is an object literal, it is by definition a JavaScript object
                                    // and therefore we can replace the constructor call with the object literal
                                    node.Parent.ReplaceChild(node, objectLiteral);

                                    // don't forget to recurse the object now
                                    objectLiteral.Accept(this);

                                    // and then bail -- we don't want to process this call
                                    // operation any more; we've gotten rid of it
                                    return;
                                }
                            }
                        }
                        else if (lookup.Name == "Array"
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.NewArrayToArrayLiteral))
                        {
                            // Array is trickier. 
                            // If there are no arguments, then just use [].
                            // if there are multiple arguments, then use [arg0,arg1...argN].
                            // but if there is one argument and it's numeric, we can't crunch it.
                            // also can't crunch if it's a function call or a member or something, since we won't
                            // KNOW whether or not it's numeric.
                            //
                            // so first see if it even is a single-argument constant wrapper. 
                            ConstantWrapper constWrapper = (node.Arguments != null && node.Arguments.Count == 1
                                ? node.Arguments[0] as ConstantWrapper
                                : null);

                            // if the argument count is not one, then we crunch.
                            // if the argument count IS one, we only crunch if we have a constant wrapper, 
                            // AND it's not numeric.
                            if (node.Arguments == null
                              || node.Arguments.Count != 1
                              || (constWrapper != null && !constWrapper.IsNumericLiteral))
                            {
                                // create the new array literal object
                                ArrayLiteral arrayLiteral = new ArrayLiteral(node.Context, m_parser, node.Arguments);

                                // replace ourself within our parent
                                if (node.Parent.ReplaceChild(node, arrayLiteral))
                                {
                                    // recurse
                                    arrayLiteral.Accept(this);
                                    // and bail -- we don't want to recurse this node any more
                                    return;
                                }
                            }
                        }
                    }
                }

                // if we are replacing resource references with strings generated from resource files
                // and this is a brackets call: lookup[args]
                var resourceList = m_parser.Settings.ResourceStrings;
                if (node.InBrackets && resourceList.Count > 0)
                {
                    // if we don't have a match visitor, create it now
                    if (m_matchVisitor == null)
                    {
                        m_matchVisitor = new MatchPropertiesVisitor();
                    }

                    // check each resource strings object to see if we have a match.
                    // Walk the list BACKWARDS so that later resource string definitions supercede previous ones.
                    for (var ndx = resourceList.Count - 1; ndx >= 0; --ndx)
                    {
                        var resourceStrings = resourceList[ndx];

                        // check to see if the resource strings name matches the function
                        if (resourceStrings != null && m_matchVisitor.Match(node.Function, resourceStrings.Name))
                        {
                            // we're going to replace this node with a string constant wrapper
                            // but first we need to make sure that this is a valid lookup.
                            // if the parameter contains anything that would vary at run-time, 
                            // then we need to throw an error.
                            // the parser will always have either one or zero nodes in the arguments
                            // arg list. We're not interested in zero args, so just make sure there is one
                            if (node.Arguments.Count == 1)
                            {
                                // must be a constant wrapper
                                ConstantWrapper argConstant = node.Arguments[0] as ConstantWrapper;
                                if (argConstant != null)
                                {
                                    string resourceName = argConstant.Value.ToString();

                                    // get the localized string from the resources object
                                    ConstantWrapper resourceLiteral = new ConstantWrapper(
                                        resourceStrings[resourceName],
                                        PrimitiveType.String,
                                        node.Context,
                                        m_parser);

                                    // replace this node with localized string, analyze it, and bail
                                    // so we don't anaylze the tree we just replaced
                                    node.Parent.ReplaceChild(node, resourceLiteral);
                                    resourceLiteral.Accept(this);
                                    return;
                                }
                                else
                                {
                                    // error! must be a constant
                                    node.Context.HandleError(
                                        JSError.ResourceReferenceMustBeConstant,
                                        true);
                                }
                            }
                            else
                            {
                                // error! can only be a single constant argument to the string resource object.
                                // the parser will only have zero or one arguments, so this must be zero
                                // (since the parser won't pass multiple args to a [] operator)
                                node.Context.HandleError(
                                    JSError.ResourceReferenceMustBeConstant,
                                    true);
                            }
                        }
                    }
                }

                // and finally, if this is a backets call and the argument is a constantwrapper that can
                // be an identifier, just change us to a member node:  obj["prop"] to obj.prop.
                // but ONLY if the string value is "safe" to be an identifier. Even though the ECMA-262
                // spec says certain Unicode categories are okay, in practice the various major browsers
                // all seem to have problems with certain characters in identifiers. Rather than risking
                // some browsers breaking when we change this syntax, don't do it for those "danger" categories.
                if (node.InBrackets && node.Arguments != null)
                {
                    // see if there is a single, constant argument
                    string argText = node.Arguments.SingleConstantArgument;
                    if (argText != null)
                    {
                        // see if we want to replace the name
                        string newName;
                        if (m_parser.Settings.HasRenamePairs && m_parser.Settings.ManualRenamesProperties
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.PropertyRenaming)
                            && !string.IsNullOrEmpty(newName = m_parser.Settings.GetNewName(argText)))
                        {
                            // yes -- we are going to replace the name, either as a string literal, or by converting
                            // to a member-dot operation.
                            // See if we can't turn it into a dot-operator. If we can't, then we just want to replace the operator with
                            // a new constant wrapper. Otherwise we'll just replace the operator with a new constant wrapper.
                            if (m_parser.Settings.IsModificationAllowed(TreeModifications.BracketMemberToDotMember)
                                && JSScanner.IsSafeIdentifier(newName)
                                && !JSScanner.IsKeyword(newName, node.EnclosingScope.UseStrict))
                            {
                                // the new name is safe to convert to a member-dot operator.
                                // but we don't want to convert the node to the NEW name, because we still need to Analyze the
                                // new member node -- and it might convert the new name to something else. So instead we're
                                // just going to convert this existing string to a member node WITH THE OLD STRING, 
                                // and THEN analyze it (which will convert the old string to newName)
                                Member replacementMember = new Member(node.Context, m_parser, node.Function, argText, node.Arguments[0].Context);
                                node.Parent.ReplaceChild(node, replacementMember);

                                // this analyze call will convert the old-name member to the newName value
                                replacementMember.Accept(this);
                                return;
                            }
                            else
                            {
                                // nope; can't convert to a dot-operator. 
                                // we're just going to replace the first argument with a new string literal
                                // and continue along our merry way.
                                node.Arguments.ReplaceChild(node.Arguments[0], new ConstantWrapper(newName, PrimitiveType.String, node.Arguments[0].Context, m_parser));
                            }
                        }
                        else if (m_parser.Settings.IsModificationAllowed(TreeModifications.BracketMemberToDotMember)
                            && JSScanner.IsSafeIdentifier(argText)
                            && !JSScanner.IsKeyword(argText, node.EnclosingScope.UseStrict))
                        {
                            // not a replacement, but the string literal is a safe identifier. So we will
                            // replace this call node with a Member-dot operation
                            Member replacementMember = new Member(node.Context, m_parser, node.Function, argText, node.Arguments[0].Context);
                            node.Parent.ReplaceChild(node, replacementMember);
                            replacementMember.Accept(this);
                            return;
                        }
                    }
                }

                // call the base class to recurse
                base.Visit(node);

                // might have changed
                member = node.Function as Member;

                // call this AFTER recursing to give the fields a chance to resolve, because we only
                // want to make this replacement if we are working on the global Date object.
                if (!node.InBrackets && !node.IsConstructor
                    && (node.Arguments == null || node.Arguments.Count == 0)
                    && member != null && string.CompareOrdinal(member.Name, "getTime") == 0
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.DateGetTimeToUnaryPlus))
                {
                    // this is not a constructor and it's not a brackets call, and there are no arguments.
                    // if the function is a member operation to "getTime" and the object of the member is a 
                    // constructor call to the global "Date" object (not a local), then we want to replace the call
                    // with a unary plus on the Date constructor. Converting to numeric type is the same as
                    // calling getTime, so it's the equivalent with much fewer bytes.
                    CallNode dateConstructor = member.Root as CallNode;
                    if (dateConstructor != null
                        && dateConstructor.IsConstructor)
                    {
                        // lookup for the predifined (not local) "Date" field
                        Lookup lookup = dateConstructor.Function as Lookup;
                        if (lookup != null && string.CompareOrdinal(lookup.Name, "Date") == 0
                            && (lookup.VariableField == null || lookup.VariableField.FieldType == FieldType.Predefined))
                        {
                            // this is in the pattern: (new Date()).getTime()
                            // we want to replace it with +new Date
                            // use the same date constructor node as the operand
                            var unary = new UnaryOperator(node.Context, m_parser, dateConstructor, JSToken.Plus, false);

                            // replace us (the call to the getTime method) with this unary operator
                            node.Parent.ReplaceChild(node, unary);

                            // don't need to recurse on the unary operator. The operand has already
                            // been analyzed when we recursed, and the unary operator wouldn't do anything
                            // special anyway (since the operand is not a numeric constant)
                        }
                    }
                }
                else
                {
                    var isEval = false;

                    var lookup = node.Function as Lookup;
                    if (lookup != null
                        && string.CompareOrdinal(lookup.Name, "eval") == 0
                        && lookup.VariableField.FieldType == FieldType.Predefined)
                    {
                        // call to predefined eval function
                        isEval = true;
                    }
                    else if (member != null && string.CompareOrdinal(member.Name, "eval") == 0)
                    {
                        // if this is a window.eval call, then we need to mark this scope as unknown just as
                        // we would if this was a regular eval call.
                        // (unless, of course, the parser settings say evals are safe)
                        // call AFTER recursing so we know the left-hand side properties have had a chance to
                        // lookup their fields to see if they are local or global
                        if (member.LeftHandSide.IsWindowLookup)
                        {
                            // this is a call to window.eval()
                            isEval = true;
                        }
                    }
                    else
                    {
                        CallNode callNode = node.Function as CallNode;
                        if (callNode != null
                            && callNode.InBrackets
                            && callNode.LeftHandSide.IsWindowLookup
                            && callNode.Arguments.IsSingleConstantArgument("eval"))
                        {
                            // this is a call to window["eval"]
                            isEval = true;
                        }
                    }

                    if (isEval)
                    {
                        if (m_parser.Settings.EvalTreatment != EvalTreatment.Ignore)
                        {
                            // mark this scope as unknown so we don't crunch out locals 
                            // we might reference in the eval at runtime
                            ScopeStack.Peek().IsKnownAtCompileTime = false;
                        }
                    }
                }
            }
        }

        private void Optimize(Conditional node)
        {
            // now check to see if the condition starts with a not-operator. If so, we can get rid of it
            // and swap the true/false children
            var unary = node.Condition as UnaryOperator;
            if (unary != null && unary.OperatorToken == JSToken.LogicalNot
                && !unary.OperatorInConditionalCompilationComment
                && m_parser.Settings.IsModificationAllowed(TreeModifications.IfNotTrueFalseToIfFalseTrue))
            {
                // get rid of the not by replacing it with its operand
                if (node.ReplaceChild(node.Condition, unary.Operand))
                {
                    // and swap the branches
                    node.SwapBranches();
                }
            }

            // see if the two branches are both assignment operations to the same variable.
            // if so, we can pull the assignment outside the conditional and have the conditional
            // be the assignment
            var trueAssign = node.TrueExpression as BinaryOperator;
            if (trueAssign != null && trueAssign.IsAssign)
            {
                var falseAssign = node.FalseExpression as BinaryOperator;
                if (falseAssign != null && falseAssign.OperatorToken == trueAssign.OperatorToken)
                {
                    // see if the left-hand-side is equivalent
                    if (trueAssign.Operand1.IsEquivalentTo(falseAssign.Operand1))
                    {
                        // transform: cond?lhs=expr1:lhs=expr2 to lhs=cond?expr1:expr2s
                        var binaryOp = new BinaryOperator(
                            node.Context,
                            m_parser,
                            trueAssign.Operand1,
                            new Conditional(
                                node.Context,
                                m_parser,
                                node.Condition,
                                trueAssign.Operand2,
                                falseAssign.Operand2),
                            falseAssign.OperatorToken);

                        node.Parent.ReplaceChild(node, binaryOp);
                    }
                }
            }
        }

        public override void Visit(Conditional node)
        {
            if (node != null)
            {
                // analye all the children
                base.Visit(node);

                // and then optimize our node
                Optimize(node);
            }
        }

        public override void Visit(ConditionalCompilationOn node)
        {
            // well, we've encountered a cc_on statement now
            m_encounteredCCOn = true;
        }

        private static bool StringSourceIsNotInlineSafe(string source)
        {
            var isNotSafe = false;
            if (!string.IsNullOrEmpty(source))
            {
                // most browsers won't close the <script> tag unless they see </script, but the
                // user has explicitly set the flag to throw an error if the string isn't safe, so
                // let's err on the side of caution. Also check for the closing of a CDATA element.
                isNotSafe = source.IndexOf("</", StringComparison.Ordinal) >= 0
                    || source.IndexOf("]]>", StringComparison.Ordinal) >= 0;
            }

            return isNotSafe;
        }

        public override void Visit(ConstantWrapper node)
        {
            if (node != null)
            {
                // if we want to throw an error when the string's source isn't inline safe...
                if (node.PrimitiveType == PrimitiveType.String
                    && node.Parser.Settings.ErrorIfNotInlineSafe
                    && node.Context != null
                    && StringSourceIsNotInlineSafe(node.Context.Code))
                {
                    // ...throw an error
                    node.Context.HandleError(JSError.StringNotInlineSafe, true);
                }

                // check to see if this node is an argument to a RegExp constructor.
                // if it is, we'll want to not use certain string escapes
                AstNode previousNode = null;
                AstNode parentNode = node.Parent;
                while (parentNode != null)
                {
                    // is this a call node and the previous node was one of the parameters?
                    CallNode callNode = parentNode as CallNode;
                    if (callNode != null && previousNode == callNode.Arguments)
                    {
                        // are we calling a simple lookup for "RegExp"?
                        Lookup lookup = callNode.Function as Lookup;
                        if (lookup != null && lookup.Name == "RegExp")
                        {
                            // we are -- so all string literals passed within this constructor should not use
                            // standard string escape sequences
                            node.IsParameterToRegExp = true;
                            // we can stop looking
                            break;
                        }
                    }

                    // next up the chain, keeping track of this current node as next iteration's "previous" node
                    previousNode = parentNode;
                    parentNode = parentNode.Parent;
                }

                // this node has no children, so don't bother calling the base
                //base.Visit(node);
            }
        }

        public override void Visit(ConstStatement node)
        {
            if (node != null)
            {
                // we want to weed out duplicates
                // var a=1, a=2 is okay, but var a, a=2 and var a=2, a should both be just var a=2, 
                // and var a, a should just be var a
                for (int ndx = 0; ndx < node.Count; ++ndx)
                {
                    string thisName = node[ndx].Identifier;

                    // we just want to throw an error if there are any duplicates. 
                    // we don't want to REMOVE anything, because we don't know if the browsers that
                    // implement this non-standard statement do first-win or last-win.
                    for (var ndx2 = ndx + 1; ndx2 < node.Count; ++ndx2)
                    {
                        if (string.CompareOrdinal(thisName, node[ndx2].Identifier) == 0)
                        {
                            node[ndx2].Context.HandleError(JSError.DuplicateConstantDeclaration, true);
                        }
                    }
                }

                // recurse the analyze
                base.Visit(node);
            }
        }

        public override void Visit(ContinueNode node)
        {
            if (node != null)
            {
                if (node.Label != null)
                {
                    // if the nest level is zero, then we might be able to remove the label altogether
                    // IF local renaming is not KeepAll AND the kill switch for removing them isn't set.
                    // the nest level will be zero if the label is undefined.
                    if (node.NestLevel == 0
                        && m_parser.Settings.LocalRenaming != LocalRenaming.KeepAll
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveUnnecessaryLabels))
                    {
                        node.Label = null;
                    }
                }

                // don't need to call the base; this statement has no children to recurse
                //base.Visit(node);
            }
        }

        public override void Visit(DoWhile node)
        {
            if (node != null)
            {
                // if we are stripping debugger statements and the body is
                // just a debugger statement, replace it with a null
                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements)
                     && node.Body != null
                     && node.Body.IsDebuggerStatement)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // recurse
                base.Visit(node);

                // if the body is now empty, make it null
                if (node.Body != null && node.Body.Count == 0)
                {
                    node.ReplaceChild(node.Body, null);
                }
            }
        }

        public override void Visit(ForNode node)
        {
            if (node != null)
            {
                // if we are stripping debugger statements and the body is
                // just a debugger statement, replace it with a null
                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements)
                     && node.Body != null
                     && node.Body.IsDebuggerStatement)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // recurse
                base.Visit(node);

                // if the body is now empty, make it null
                if (node.Body != null && node.Body.Count == 0)
                {
                    node.ReplaceChild(node.Body, null);
                }
            }
        }

        public override void Visit(ForIn node)
        {
            if (node != null)
            {
                // if we are stripping debugger statements and the body is
                // just a debugger statement, replace it with a null
                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements)
                     && node.Body != null
                     && node.Body.IsDebuggerStatement)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // recurse
                base.Visit(node);

                // if the body is now empty, make it null
                if (node.Body != null && node.Body.Count == 0)
                {
                    node.ReplaceChild(node.Body, null);
                }
            }
        }

        public override void Visit(FunctionObject node)
        {
            if (node != null)
            {
                // get the name of this function, calculate something if it's anonymous
                if (node.Identifier == null)
                {
                    node.Name = GuessAtName(node);
                }

                // don't analyze the identifier or we'll add an extra reference to it.
                // and we don't need to analyze the parameters because they were fielded-up
                // back when the function object was created, too

                if (ScopeStack.Peek().UseStrict)
                {
                    // if this is a function delcaration, it better be a source element.
                    // if not, we want to throw a warning that different browsers will treat this function declaration
                    // differently. Technically, this location is not allowed. IE and most other browsers will 
                    // simply treat it like every other function declaration in this scope. Firefox, however, won't
                    // add this function declaration's name to the containing scope until the function declaration
                    // is actually "executed." So if you try to call it BEFORE, you will get a "not defined" error.
                    if (!node.IsSourceElement && node.FunctionType == FunctionType.Declaration)
                    {
                        node.Context.HandleError(JSError.MisplacedFunctionDeclaration, true);
                    }


                    // we need to make sure the function isn't named "eval" or "arguments"
                    if (string.CompareOrdinal(node.Name, "eval") == 0
                        || string.CompareOrdinal(node.Name, "arguments") == 0)
                    {
                        if (node.IdContext != null)
                        {
                            node.IdContext.HandleError(JSError.StrictModeFunctionName, true);
                        }
                        else if (node.Context != null)
                        {
                            node.Context.HandleError(JSError.StrictModeFunctionName, true);
                        }
                    }

                    // we need to make sure:
                    //  1. there are no duplicate argument names, and
                    //  2. none of them are named "eval" or "arguments"
                    // create map that we'll use to determine if there are any dups
                    if (node.ParameterDeclarations != null
                        && node.ParameterDeclarations.Count > 0)
                    {
                        var parameterMap = new Dictionary<string, string>(node.ParameterDeclarations.Count);
                        foreach (var parameter in node.ParameterDeclarations)
                        {
                            // if it already exists in the map, then it's a dup
                            if (parameterMap.ContainsKey(parameter.Name))
                            {
                                // already exists -- throw an error
                                parameter.Context.HandleError(JSError.StrictModeDuplicateArgument, true);
                            }
                            else
                            {
                                // not in there, add it now
                                parameterMap.Add(parameter.Name, parameter.Name);

                                // now check to see if it's one of the two forbidden names
                                if (string.CompareOrdinal(parameter.Name, "eval") == 0
                                    || string.CompareOrdinal(parameter.Name, "arguments") == 0)
                                {
                                    parameter.Context.HandleError(JSError.StrictModeArgumentName, true);
                                }
                            }
                        }
                    }
                }
                else if (node.ParameterDeclarations != null
                    && node.ParameterDeclarations.Count > 0)
                {
                    // not strict
                    // if there are duplicate parameter names, throw a warning
                    var parameterMap = new Dictionary<string, string>(node.ParameterDeclarations.Count);
                    foreach (var parameter in node.ParameterDeclarations)
                    {
                        // if it already exists in the map, then it's a dup
                        if (parameterMap.ContainsKey(parameter.Name))
                        {
                            // already exists -- throw an error
                            parameter.Context.HandleError(JSError.DuplicateName, false);
                        }
                        else
                        {
                            // not in there, add it now
                            parameterMap.Add(parameter.Name, parameter.Name);
                        }
                    }
                }

                // push the stack and analyze the body
                ScopeStack.Push(node.FunctionScope);
                try
                {
                    // recurse the body
                    node.Body.Accept(this);
                }
                finally
                {
                    ScopeStack.Pop();
                }
            }
        }

        public override void Visit(IfNode node)
        {
            if (node != null)
            {
                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements))
                {
                    if (node.TrueBlock != null && node.TrueBlock.IsDebuggerStatement)
                    {
                        node.ReplaceChild(node.TrueBlock, null);
                    }

                    if (node.FalseBlock != null && node.FalseBlock.IsDebuggerStatement)
                    {
                        node.ReplaceChild(node.FalseBlock, null);
                    }
                }

                // recurse....
                base.Visit(node);

                // now check to see if the two branches are now empty.
                // if they are, null them out.
                if (node.TrueBlock != null && node.TrueBlock.Count == 0)
                {
                    node.ReplaceChild(node.TrueBlock, null);
                }
                if (node.FalseBlock != null && node.FalseBlock.Count == 0)
                {
                    node.ReplaceChild(node.FalseBlock, null);
                }

                if (node.TrueBlock != null && node.FalseBlock != null)
                {
                    // neither true block nor false block is null.
                    // if they're both expressions, convert them to a condition operator
                    if (node.TrueBlock.IsExpression && node.FalseBlock.IsExpression
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.IfExpressionsToExpression))
                    {
                        // if this statement has both true and false blocks, and they are both expressions,
                        // then we can simplify this to a conditional expression.
                        // because the blocks are expressions, we know they only have ONE statement in them,
                        // so we can just dereference them directly.
                        Conditional conditional;
                        var logicalNot = new LogicalNot(node.Condition, m_parser);
                        if (logicalNot.Measure() < 0)
                        {
                            // applying a logical-not makes the condition smaller -- reverse the branches
                            logicalNot.Apply();
                            conditional = new Conditional(
                                node.Context,
                                m_parser,
                                node.Condition,
                                node.FalseBlock[0],
                                node.TrueBlock[0]);
                        }
                        else
                        {
                            // regular order
                            conditional = new Conditional(
                                node.Context,
                                m_parser,
                                node.Condition,
                                node.TrueBlock[0],
                                node.FalseBlock[0]);
                        }

                        node.Parent.ReplaceChild(
                            node,
                            conditional);

                        Optimize(conditional);
                    }
                    else
                    {
                        // see if logical-notting the condition produces something smaller
                        var logicalNot = new LogicalNot(node.Condition, m_parser);
                        if (logicalNot.Measure() < 0)
                        {
                            // it does -- not the condition and swap the branches
                            logicalNot.Apply();
                            node.SwapBranches();
                        }

                        // see if the true- and false-branches each contain only a single statement
                        if (node.TrueBlock.Count == 1 && node.FalseBlock.Count == 1)
                        {
                            // they do -- see if the true-branch's statement is a return-statement
                            var trueReturn = node.TrueBlock[0] as ReturnNode;
                            if (trueReturn != null && trueReturn.Operand != null)
                            {
                                // it is -- see if the false-branch is also a return statement
                                var falseReturn = node.FalseBlock[0] as ReturnNode;
                                if (falseReturn != null && falseReturn.Operand != null)
                                {
                                    // transform: if(cond)return expr1;else return expr2 to return cond?expr1:expr2
                                    var conditional = new Conditional(null, m_parser,
                                        node.Condition,
                                        trueReturn.Operand,
                                        falseReturn.Operand);

                                    // create a new return node from the conditional and replace
                                    // our if-node with it
                                    var returnNode = new ReturnNode(
                                        node.Context,
                                        m_parser,
                                        conditional);

                                    node.Parent.ReplaceChild(
                                        node,
                                        returnNode);

                                    Optimize(conditional);
                                }
                            }
                        }
                    }
                }
                else if (node.FalseBlock != null)
                {
                    // true block must be null.
                    // if there is no true branch but a false branch, then
                    // put a not on the condition and move the false branch to the true branch.
                    if (node.FalseBlock.IsExpression
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionCallToConditionAndCall))
                    {
                        // if (cond); else expr ==> cond || expr
                        // but first -- which operator to use? if(a);else b --> a||b, and if(!a);else b --> a&&b
                        // so determine which one is smaller: a or !a
                        // assume we'll use the logical-or, since that doesn't require changing the condition
                        var newOperator = JSToken.LogicalOr;
                        var logicalNot = new LogicalNot(node.Condition, m_parser);
                        if (logicalNot.Measure() < 0)
                        {
                            // !a is smaller, so apply it and use the logical-or operator
                            logicalNot.Apply();
                            newOperator = JSToken.LogicalAnd;
                        }

                        var binaryOp = new BinaryOperator(
                            node.Context,
                            m_parser,
                            node.Condition,
                            node.FalseBlock[0],
                            newOperator);

                        // we don't need to analyse this new node because we've already analyzed
                        // the pieces parts as part of the if. And this visitor's method for the BinaryOperator
                        // doesn't really do anything else. Just replace our current node with this
                        // new node
                        node.Parent.ReplaceChild(node, binaryOp);
                    }
                    else if (m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionFalseToIfNotConditionTrue))
                    {
                        // logical-not the condition
                        // if(cond);else stmt ==> if(!cond)stmt
                        var logicalNot = new LogicalNot(node.Condition, m_parser);
                        logicalNot.Apply();

                        // and swap the branches
                        node.SwapBranches();
                    }
                }
                else if (node.TrueBlock != null)
                {
                    // false block must be null
                    if (node.TrueBlock.IsExpression
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.IfConditionCallToConditionAndCall))
                    {
                        // convert the if-node to an expression
                        IfConditionExpressionToExpression(node, node.TrueBlock[0]);
                    }
                }
                else if (m_parser.Settings.IsModificationAllowed(TreeModifications.IfEmptyToExpression))
                {
                    // NEITHER branches have anything now!

                    // something we can do in the future: as long as the condition doesn't
                    // contain calls or assignments, we should be able to completely delete
                    // the statement altogether rather than changing it to an expression
                    // statement on the condition.

                    // I'm just not doing it yet because I don't
                    // know what the effect will be on the iteration of block statements.
                    // if we're on item, 5, for instance, and we delete it, will the next
                    // item be item 6, or will it return the NEW item 5 (since the old item
                    // 5 was deleted and everything shifted up)?

                    // We don't know what it is and what the side-effects may be, so
                    // just change this statement into an expression statement by replacing us with 
                    // the expression
                    node.Parent.ReplaceChild(node, node.Condition);
                    // no need to analyze -- we already recursed
                }

                if (node.FalseBlock == null
                    && node.TrueBlock != null
                    && node.TrueBlock.Count == 1
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.CombineNestedIfs))
                {
                    var nestedIf = node.TrueBlock[0] as IfNode;
                    if (nestedIf != null && nestedIf.FalseBlock == null)
                    {
                        // we have nested if-blocks.
                        // transform if(cond1)if(cond2){...} to if(cond1&&cond2){...}
                        // change the first if-statement's condition to be cond1&&cond2
                        // move the nested if-statement's true block to the outer if-statement
                        node.ReplaceChild(node.Condition,
                            new BinaryOperator(null, m_parser, node.Condition, nestedIf.Condition, JSToken.LogicalAnd));
                        node.ReplaceChild(node.TrueBlock, nestedIf.TrueBlock);
                    }
                }
            }
        }

        private void IfConditionExpressionToExpression(IfNode ifNode, AstNode expression)
        {
            // but first -- which operator to use? if(a)b --> a&&b, and if(!a)b --> a||b
            // so determine which one is smaller: a or !a
            // assume we'll use the logical-and, since that doesn't require changing the condition
            var newOperator = JSToken.LogicalAnd;
            var logicalNot = new LogicalNot(ifNode.Condition, m_parser);
            if (logicalNot.Measure() < 0)
            {
                // !a is smaller, so apply it and use the logical-or operator
                logicalNot.Apply();
                newOperator = JSToken.LogicalOr;
            }

            // because the true block is an expression, we know it must only have
            // ONE statement in it, so we can just dereference it directly.
            var binaryOp = new BinaryOperator(
                ifNode.Context,
                m_parser,
                ifNode.Condition,
                expression,
                newOperator
                );

            // we don't need to analyse this new node because we've already analyzed
            // the pieces parts as part of the if. And this visitor's method for the BinaryOperator
            // doesn't really do anything else. Just replace our current node with this
            // new node
            ifNode.Parent.ReplaceChild(ifNode, binaryOp);
        }

        public override void Visit(Lookup node)
        {
            if (node != null)
            {
                // figure out if our reference type is a function or a constructor
                if (node.Parent is CallNode)
                {
                    node.RefType = (
                      ((CallNode)(node.Parent)).IsConstructor
                      ? ReferenceType.Constructor
                      : ReferenceType.Function
                      );
                }

                // check the name of the variable for reserved words that aren't allowed
                ActivationObject scope = ScopeStack.Peek();
                if (JSScanner.IsKeyword(node.Name, scope.UseStrict))
                {
                    node.Context.HandleError(JSError.KeywordUsedAsIdentifier, true);
                }

                node.VariableField = scope.FindReference(node.Name);
                if (node.VariableField == null)
                {
                    // this must be a global. if it isn't in the global space, throw an error
                    // this name is not in the global space.
                    // if it isn't generated, then we want to throw an error
                    // we also don't want to report an undefined variable if it is the object
                    // of a typeof operator
                    UnaryOperator unaryOperator;
                    if (!node.IsGenerated
                        && ((unaryOperator = node.Parent as UnaryOperator) == null || unaryOperator.OperatorToken != JSToken.TypeOf))
                    {
                        // report this undefined reference
                        node.Context.ReportUndefined(node);

                        // possibly undefined global (but definitely not local)
                        var isFunction = node.Parent is CallNode && ((CallNode)(node.Parent)).Function == node;
                        node.Context.HandleError(
                          (isFunction ? JSError.UndeclaredFunction : JSError.UndeclaredVariable),
                          false);
                    }

                    if (!(scope is GlobalScope))
                    {
                        // add it to the scope so we know this scope references the global
                        scope.AddField(new JSVariableField(
                            FieldType.Global,
                            node.Name,
                            0,
                            Missing.Value));
                    }
                }
                else
                {
                    // BUT if this field is a place-holder in the containing scope of a named
                    // function expression, then we need to throw an ambiguous named function expression
                    // error because this could cause problems.
                    // OR if the field is already marked as ambiguous, throw the error
                    if (node.VariableField.IsAmbiguous)
                    {
                        // throw an error
                        node.Context.HandleError(JSError.AmbiguousNamedFunctionExpression, false);

                        // if we are preserving function names, then we need to mark this field
                        // as not crunchable
                        if (m_parser.Settings.PreserveFunctionNames)
                        {
                            node.VariableField.CanCrunch = false;
                        }
                    }
                    else if (node.VariableField.NamedFunctionExpression != null)
                    {
                        // the field for this lookup is tied to a named function expression!
                        // we really only care if this variable is being assigned something
                        // OTHER than a function expression with the same name (which should
                        // be the NamedFunctionExpression property.
                        var binaryOperator = node.Parent as BinaryOperator;
                        if (binaryOperator != null && binaryOperator.IsAssign)
                        {
                            // this is ambiguous cross-browser
                            node.VariableField.IsAmbiguous = true;

                            // throw an error
                            node.Context.HandleError(JSError.AmbiguousNamedFunctionExpression, false);
                        }
                    }

                    // see if this scope already points to this name
                    if (scope[node.Name] == null)
                    {
                        // create an inner reference so we don't keep walking up the scope chain for this name
                        node.VariableField = scope.CreateInnerField(node.VariableField);
                    }

                    // add the reference
                    node.VariableField.AddReference(scope);

                    if (node.VariableField.FieldType == FieldType.Predefined)
                    {
                        // this is a predefined field. If it's Nan or Infinity, we should
                        // replace it with the numeric value in case we need to later combine
                        // some literal expressions.
                        if (string.CompareOrdinal(node.Name, "NaN") == 0)
                        {
                            // don't analyze the new ConstantWrapper -- we don't want it to take part in the
                            // duplicate constant combination logic should it be turned on.
                            node.Parent.ReplaceChild(node, new ConstantWrapper(double.NaN, PrimitiveType.Number, node.Context, m_parser));
                        }
                        else if (string.CompareOrdinal(node.Name, "Infinity") == 0)
                        {
                            // don't analyze the new ConstantWrapper -- we don't want it to take part in the
                            // duplicate constant combination logic should it be turned on.
                            node.Parent.ReplaceChild(node, new ConstantWrapper(double.PositiveInfinity, PrimitiveType.Number, node.Context, m_parser));
                        }
                    }
                }
            }
        }

        public override void Visit(Member node)
        {
            if (node != null)
            {
                // if we don't even have any resource strings, then there's nothing
                // we need to do and we can just perform the base operation
                var resourceList = m_parser.Settings.ResourceStrings;
                if (resourceList.Count > 0)
                {
                    // if we haven't created the match visitor yet, do so now
                    if (m_matchVisitor == null)
                    {
                        m_matchVisitor = new MatchPropertiesVisitor();
                    }

                    // walk the list BACKWARDS so that later resource strings supercede previous ones
                    for (var ndx = resourceList.Count - 1; ndx >= 0; --ndx)
                    {
                        var resourceStrings = resourceList[ndx];

                        // see if the resource string name matches the root
                        if (m_matchVisitor.Match(node.Root, resourceStrings.Name))
                        {
                            // it is -- we're going to replace this with a string value.
                            // if this member name is a string on the object, we'll replacve it with
                            // the literal. Otherwise we'll replace it with an empty string.
                            // see if the string resource contains this value
                            ConstantWrapper stringLiteral = new ConstantWrapper(
                                resourceStrings[node.Name] ?? string.Empty,
                                PrimitiveType.String,
                                node.Context,
                                m_parser
                                );

                            node.Parent.ReplaceChild(node, stringLiteral);

                            // analyze the literal
                            stringLiteral.Accept(this);
                            return;
                        }
                    }
                }

                // if we are replacing property names and we have something to replace
                if (m_parser.Settings.HasRenamePairs && m_parser.Settings.ManualRenamesProperties
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.PropertyRenaming))
                {
                    // see if this name is a target for replacement
                    string newName = m_parser.Settings.GetNewName(node.Name);
                    if (!string.IsNullOrEmpty(newName))
                    {
                        // it is -- set the name to the new name
                        node.Name = newName;
                    }
                }

                // check the name of the member for reserved words that aren't allowed
                if (JSScanner.IsKeyword(node.Name, ScopeStack.Peek().UseStrict))
                {
                    node.NameContext.HandleError(JSError.KeywordUsedAsIdentifier, true);
                }

                // recurse
                base.Visit(node);
            }
        }

        public override void Visit(ObjectLiteral node)
        {
            if (node != null)
            {
                // recurse
                base.Visit(node);

                if (ScopeStack.Peek().UseStrict)
                {
                    // now strict-mode checks
                    // go through all property names and make sure there are no duplicates.
                    // use a map to remember which ones we already have.
                    var nameMap = new Dictionary<string, string>(node.Count);
                    for (var ndx = 0; ndx < node.Keys.Count; ++ndx)
                    {
                        // get the name and type of this property
                        var propertyName = node.Keys[ndx].ToString();
                        var propertyType = GetPropertyType(node.Values[ndx] as FunctionObject);

                        // key name is the name plus the type. Can't just use the name because 
                        // get and set will both have the same name (but different types)
                        var keyName = propertyName + propertyType;

                        string mappedType;
                        if (propertyType == "data")
                        {
                            // can't have another data, get, or set
                            if (nameMap.TryGetValue(keyName, out mappedType)
                                || nameMap.TryGetValue(propertyName + "get", out mappedType)
                                || nameMap.TryGetValue(propertyName + "set", out mappedType))
                            {
                                // throw the error
                                node.Keys[ndx].Context.HandleError(JSError.StrictModeDuplicateProperty, true);

                                // if the mapped type isn't data, then we can add this data name/type to the map
                                // because that means the first tryget failed and we don't have a data already
                                if (mappedType != propertyType)
                                {
                                    nameMap.Add(keyName, propertyType);
                                }
                            }
                            else
                            {
                                // not in the map at all. Add it now.
                                nameMap.Add(keyName, propertyType);
                            }
                        }
                        else
                        {
                            // get can have a set, but can't have a data or another get
                            // set can have a get, but can't have a data or another set
                            if (nameMap.TryGetValue(keyName, out mappedType)
                                || nameMap.TryGetValue(propertyName + "data", out mappedType))
                            {
                                // throw the error
                                node.Keys[ndx].Context.HandleError(JSError.StrictModeDuplicateProperty, true);

                                // if the mapped type isn't data, then we can add this data name/type to the map
                                if (mappedType != propertyType)
                                {
                                    nameMap.Add(keyName, propertyType);
                                }
                            }
                            else
                            {
                                // not in the map at all - add it now
                                nameMap.Add(keyName, propertyType);
                            }
                        }
                    }
                }
            }
        }

        private static string GetPropertyType(FunctionObject funcObj)
        {
            // should never be a function declaration....
            return funcObj == null || funcObj.FunctionType == FunctionType.Expression
                ? "data"
                : funcObj.FunctionType == FunctionType.Getter ? "get" : "set";
        }

        public override void Visit(ObjectLiteralField node)
        {
            if (node != null)
            {
                if (node.PrimitiveType == PrimitiveType.String
                    && m_parser.Settings.HasRenamePairs && m_parser.Settings.ManualRenamesProperties
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.PropertyRenaming))
                {
                    string newName = m_parser.Settings.GetNewName(node.Value.ToString());
                    if (!string.IsNullOrEmpty(newName))
                    {
                        node.Value = newName;
                    }
                }

                // don't call the base -- we don't want to add the literal to
                // the combination logic, which is what the ConstantWrapper (base class) does
                //base.Visit(node);
            }
        }

        public override void Visit(RegExpLiteral node)
        {
            if (node != null)
            {
                // verify the syntax
                try
                {
                    // just try instantiating a Regex object with this string.
                    // if it's invalid, it will throw an exception.
                    // we don't need to pass the flags -- we're just interested in the pattern
                    Regex re = new Regex(node.Pattern, RegexOptions.ECMAScript);

                    // basically we have this test here so the re variable is referenced
                    // and FxCop won't throw an error. There really aren't any cases where
                    // the constructor will return null (other than out-of-memory)
                    if (re == null)
                    {
                        node.Context.HandleError(JSError.RegExpSyntax, true);
                    }
                }
                catch (System.ArgumentException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    node.Context.HandleError(JSError.RegExpSyntax, true);
                }
                // don't bother calling the base -- there are no children
            }
        }

        public override void Visit(ReturnNode node)
        {
            if (node != null)
            {
                // first we want to make sure that we are indeed within a function scope.
                // it makes no sense to have a return outside of a function
                ActivationObject scope = ScopeStack.Peek();
                while (scope != null && !(scope is FunctionScope))
                {
                    scope = scope.Parent;
                }

                if (scope == null)
                {
                    node.Context.HandleError(JSError.BadReturn);
                }

                // now just do the default analyze
                base.Visit(node);
            }
        }

        public override void Visit(Switch node)
        {
            if (node != null)
            {
                base.Visit(node);

                // we only want to remove stuff if we are hypercrunching
                if (m_parser.Settings.RemoveUnneededCode)
                {
                    // because we are looking at breaks, we need to know if this
                    // switch statement is labeled
                    string thisLabel = string.Empty;
                    LabeledStatement label = node.Parent as LabeledStatement;
                    if (label != null)
                    {
                        thisLabel = label.Label;
                    }

                    // loop through all the cases, looking for the default.
                    // then, if it's empty (or just doesn't do anything), we can
                    // get rid of it altogether
                    int defaultCase = -1;
                    bool eliminateDefault = false;
                    for (int ndx = 0; ndx < node.Cases.Count; ++ndx)
                    {
                        // it should always be a switch case, but just in case...
                        SwitchCase switchCase = node.Cases[ndx] as SwitchCase;
                        if (switchCase != null)
                        {
                            if (switchCase.IsDefault)
                            {
                                // save the index for later
                                defaultCase = ndx;

                                // set the flag to true unless we can prove that we need it.
                                // we'll prove we need it by finding the statement block executed by
                                // this case and showing that it's neither empty nor containing
                                // just a single break statement.
                                eliminateDefault = true;
                            }

                            // if the default case is empty, then we need to keep going
                            // until we find the very next non-empty case
                            if (eliminateDefault && switchCase.Statements.Count > 0)
                            {
                                // this is the set of statements executed during default processing.
                                // if it does nothing -- one break statement -- then we can get rid
                                // of the default case. Otherwise we need to leave it in.
                                if (switchCase.Statements.Count == 1)
                                {
                                    // see if it's a break
                                    Break lastBreak = switchCase.Statements[0] as Break;

                                    // if the last statement is not a break,
                                    // OR it has a label and it's not this switch statement...
                                    if (lastBreak == null
                                      || (lastBreak.Label != null && lastBreak.Label != thisLabel))
                                    {
                                        // set the flag back to false to indicate that we need to keep it.
                                        eliminateDefault = false;
                                    }
                                }
                                else
                                {
                                    // set the flag back to false to indicate that we need to keep it.
                                    eliminateDefault = false;
                                }

                                // break out of the loop
                                break;
                            }
                        }
                    }

                    // if we get here and the flag is still true, then either the default case is
                    // empty, or it contains only a single break statement. Either way, we can get 
                    // rid of it.
                    if (eliminateDefault && defaultCase >= 0
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveEmptyDefaultCase))
                    {
                        // remove it and reset the position index
                        node.Cases.RemoveAt(defaultCase);
                        defaultCase = -1;
                    }

                    // if we have no default handling, then we know we can get rid
                    // of any cases that don't do anything either.
                    if (defaultCase == -1
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveEmptyCaseWhenNoDefault))
                    {
                        // when we delete a case statement, we set this flag to true.
                        // when we hit a non-empty case statement, we set the flag to false.
                        // if we hit an empty case statement when this flag is true, we can delete this case, too.
                        bool emptyStatements = true;
                        Break deletedBreak = null;

                        // walk the tree backwards because we don't know how many we will
                        // be deleting, and if we go backwards, we won't have to adjust the 
                        // index as we go.
                        for (int ndx = node.Cases.Count - 1; ndx >= 0; --ndx)
                        {
                            // should always be a switch case
                            SwitchCase switchCase = node.Cases[ndx] as SwitchCase;
                            if (switchCase != null)
                            {
                                // if the block is empty and the last block was empty, we can delete this case.
                                // OR if there is only one statement and it's a break, we can delete it, too.
                                if (switchCase.Statements.Count == 0 && emptyStatements)
                                {
                                    // remove this case statement because it falls through to a deleted case
                                    node.Cases.RemoveAt(ndx);
                                }
                                else
                                {
                                    // onlyBreak will be set to null if this block is not a single-statement break block
                                    Break onlyBreak = (switchCase.Statements.Count == 1 ? switchCase.Statements[0] as Break : null);
                                    if (onlyBreak != null)
                                    {
                                        // we'll only delete this case if the break either doesn't have a label
                                        // OR the label matches the switch statement
                                        if (onlyBreak.Label == null || onlyBreak.Label == thisLabel)
                                        {
                                            // if this is a block with only a break, then we need to keep a hold of the break
                                            // statement in case we need it later
                                            deletedBreak = onlyBreak;

                                            // remove this case statement
                                            node.Cases.RemoveAt(ndx);
                                            // make sure the flag is set so we delete any other empty
                                            // cases that fell through to this empty case block
                                            emptyStatements = true;
                                        }
                                        else
                                        {
                                            // the break statement has a label and it's not the switch statement.
                                            // we're going to keep this block
                                            emptyStatements = false;
                                            deletedBreak = null;
                                        }
                                    }
                                    else
                                    {
                                        // either this is a non-empty block, or it's an empty case that falls through
                                        // to a non-empty block. if we have been deleting case statements and this
                                        // is not an empty block....
                                        if (emptyStatements && switchCase.Statements.Count > 0 && deletedBreak != null)
                                        {
                                            // we'll need to append the deleted break statement if it doesn't already have
                                            // a flow-changing statement: break, continue, return, or throw
                                            AstNode lastStatement = switchCase.Statements[switchCase.Statements.Count - 1];
                                            if (!(lastStatement is Break) && !(lastStatement is ContinueNode)
                                              && !(lastStatement is ReturnNode) && !(lastStatement is ThrowNode))
                                            {
                                                switchCase.Statements.Append(deletedBreak);
                                            }
                                        }

                                        // make sure the deletedBreak flag is reset
                                        deletedBreak = null;

                                        // reset the flag
                                        emptyStatements = false;
                                    }
                                }
                            }
                        }
                    }

                    // if the last case's statement list ends in a break, 
                    // we can get rid of the break statement
                    if (node.Cases.Count > 0
                        && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveBreakFromLastCaseBlock))
                    {
                        SwitchCase lastCase = node.Cases[node.Cases.Count - 1] as SwitchCase;
                        if (lastCase != null)
                        {
                            // get the block of statements making up the last case block
                            Block lastBlock = lastCase.Statements;
                            // if the last statement is not a break, then lastBreak will be null
                            Break lastBreak = (lastBlock.Count > 0 ? lastBlock[lastBlock.Count - 1] as Break : null);
                            // if lastBreak is not null and it either has no label, or the label matches this switch statement...
                            if (lastBreak != null
                              && (lastBreak.Label == null || lastBreak.Label == thisLabel))
                            {
                                // remove the break statement
                                lastBlock.RemoveLast();
                            }
                        }
                    }
                }
            }
        }

        public override void Visit(TryNode node)
        {
            if (node != null)
            {
                // get the field -- it should have been generated when the scope was analyzed
                if (node.CatchBlock != null && !string.IsNullOrEmpty(node.CatchVarName))
                {
                    node.SetCatchVariable(node.CatchBlock.BlockScope[node.CatchVarName]);
                }

                // anaylze the blocks
                base.Visit(node);

                // if the try block is empty, then set it to null
                if (node.TryBlock != null && node.TryBlock.Count == 0)
                {
                    node.ReplaceChild(node.TryBlock, null);
                }

                // eliminate an empty finally block UNLESS there is no catch block.
                if (node.FinallyBlock != null && node.FinallyBlock.Count == 0 && node.CatchBlock != null
                    && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveEmptyFinally))
                {
                    node.ReplaceChild(node.FinallyBlock, null);
                }

                // check strict-mode restrictions
                if (ScopeStack.Peek().UseStrict && !string.IsNullOrEmpty(node.CatchVarName))
                {
                    // catch variable cannot be named "eval" or "arguments"
                    if (string.CompareOrdinal(node.CatchVarName, "eval") == 0
                        || string.CompareOrdinal(node.CatchVarName, "arguments") == 0)
                    {
                        node.CatchVarContext.HandleError(JSError.StrictModeVariableName, true);
                    }
                }
            }
        }

        public override void Visit(UnaryOperator node)
        {
            if (node != null)
            {
                base.Visit(node);

                // strict mode has some restrictions
                if (node.OperatorToken == JSToken.Delete)
                {
                    if (ScopeStack.Peek().UseStrict)
                    {
                        // operand of a delete operator cannot be a variable name, argument name, or function name
                        // which means it can't be a lookup
                        if (node.Operand is Lookup)
                        {
                            node.Context.HandleError(JSError.StrictModeInvalidDelete, true);
                        }
                    }
                }
                else if (node.OperatorToken == JSToken.Increment || node.OperatorToken == JSToken.Decrement)
                {
                    // strict mode has some restrictions we want to check now
                    if (ScopeStack.Peek().UseStrict)
                    {
                        // the operator cannot be the eval function or arguments object.
                        // that means the operator is a lookup, and the field for that lookup
                        // is the arguments object or the predefined "eval" object.
                        // could probably just check the names, since we can't create local variables
                        // with those names anyways.
                        var lookup = node.Operand as Lookup;
                        if (lookup != null
                            && (lookup.VariableField == null
                            || lookup.VariableField.FieldType == FieldType.Arguments
                            || (lookup.VariableField.FieldType == FieldType.Predefined && string.CompareOrdinal(lookup.Name, "eval") == 0)))
                        {
                            node.Operand.Context.HandleError(JSError.StrictModeInvalidPreOrPost, true);
                        }
                    }
                }
                else
                {

                    // if the operand is a numeric literal
                    ConstantWrapper constantWrapper = node.Operand as ConstantWrapper;
                    if (constantWrapper != null && constantWrapper.IsNumericLiteral)
                    {
                        // get the value of the constant. We've already screened it for numeric, so
                        // we don't have to worry about catching any errors
                        double doubleValue = constantWrapper.ToNumber();

                        // if this is a unary minus...
                        if (node.OperatorToken == JSToken.Minus
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.ApplyUnaryMinusToNumericLiteral))
                        {
                            // negate the value
                            constantWrapper.Value = -doubleValue;

                            // replace us with the negated constant
                            if (node.Parent.ReplaceChild(node, constantWrapper))
                            {
                                // the context for the minus will include the number (its operand),
                                // but the constant will just be the number. Update the context on
                                // the constant to be a copy of the context on the operator
                                constantWrapper.Context = node.Context.Clone();
                            }
                        }
                        else if (node.OperatorToken == JSToken.Plus
                            && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveUnaryPlusOnNumericLiteral))
                        {
                            // +NEG is still negative, +POS is still positive, and +0 is still 0.
                            // so just get rid of the unary operator altogether
                            if (node.Parent.ReplaceChild(node, constantWrapper))
                            {
                                // the context for the unary will include the number (its operand),
                                // but the constant will just be the number. Update the context on
                                // the constant to be a copy of the context on the operator
                                constantWrapper.Context = node.Context.Clone();
                            }
                        }
                    }
                }
            }
        }

        public override void Visit(Var node)
        {
            if (node != null)
            {
                // first we want to weed out duplicates that don't have initializers
                // var a=1, a=2 is okay, but var a, a=2 and var a=2, a should both be just var a=2, 
                // and var a, a should just be var a
                if (m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveDuplicateVar))
                {
                    // first we want to weed out duplicates that don't have initializers
                    // var a=1, a=2 is okay, but var a, a=2 and var a=2, a should both be just var a=2, 
                    // and var a, a should just be var a
                    int ndx = 0;
                    while (ndx < node.Count)
                    {
                        string thisName = node[ndx].Identifier;

                        // handle differently if we have an initializer or not
                        if (node[ndx].Initializer != null)
                        {
                            // the current vardecl has an initializer, so we want to delete any other
                            // vardecls of the same name in the rest of the list with no initializer
                            // and move on to the next item afterwards
                            DeleteNoInits(node, ++ndx, thisName);
                        }
                        else
                        {
                            // this vardecl has no initializer, so we can delete it if there is ANY
                            // other vardecl with the same name (whether or not it has an initializer)
                            if (VarDeclExists(node, ndx + 1, thisName))
                            {
                                node.RemoveAt(ndx);

                                // don't increment the index; we just deleted the current item,
                                // so the next item just slid into this position
                            }
                            else
                            {
                                // nope -- it's the only one. Move on to the next
                                ++ndx;
                            }
                        }
                    }
                }

                // recurse the analyze
                base.Visit(node);
            }
        }

        public override void Visit(VariableDeclaration node)
        {
            if (node != null)
            {
                base.Visit(node);

                // check the name of the variable for reserved words that aren't allowed
                if (JSScanner.IsKeyword(node.Identifier, ScopeStack.Peek().UseStrict))
                {
                    node.Context.HandleError(JSError.KeywordUsedAsIdentifier, true);
                }
                else if (ScopeStack.Peek().UseStrict 
                    && (string.CompareOrdinal(node.Identifier, "eval") == 0
                    || string.CompareOrdinal(node.Identifier, "arguments") == 0))
                {
                    // strict mode cannot declare variables named "eval" or "arguments"
                    node.IdentifierContext.HandleError(JSError.StrictModeVariableName, true);
                }

                // if this is a special-case vardecl (var foo/*@cc_on=EXPR@*/), set the flag indicating
                // we encountered a @cc_on statement if we found one
                if (node.IsCCSpecialCase && m_parser.Settings.IsModificationAllowed(TreeModifications.RemoveUnnecessaryCCOnStatements))
                {
                    node.UseCCOn = !m_encounteredCCOn;
                    m_encounteredCCOn = true;
                }
            }
        }

        public override void Visit(WhileNode node)
        {
            if (node != null)
            {
                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements)
                     && node.Body != null
                     && node.Body.IsDebuggerStatement)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // recurse
                base.Visit(node);

                // if the body is now empty, make it null
                if (node.Body != null && node.Body.Count == 0)
                {
                    node.ReplaceChild(node.Body, null);
                }
            }
        }

        public override void Visit(WithNode node)
        {
            if (node != null)
            {
                // throw a warning discouraging the use of this statement
                if (ScopeStack.Peek().UseStrict)
                {
                    // with-statements not allowed in strict code at all
                    node.Context.HandleError(JSError.StrictModeNoWith, true);
                }
                else
                {
                    // not strict, but still not recommended
                    node.Context.HandleError(JSError.WithNotRecommended, false);
                }

                // hold onto the with-scope in case we need to do something with it
                BlockScope withScope = (node.Body == null ? null : node.Body.BlockScope);

                if (m_parser.Settings.StripDebugStatements
                     && m_parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements)
                     && node.Body != null
                     && node.Body.IsDebuggerStatement)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // recurse
                base.Visit(node);

                // we'd have to know what the object (obj) evaluates to before we
                // can figure out what to add to the scope -- not possible without actually
                // running the code. This could throw a whole bunch of 'undefined' errors.
                if (node.Body != null && node.Body.Count == 0)
                {
                    node.ReplaceChild(node.Body, null);
                }

                // we got rid of the block -- tidy up the no-longer-needed scope
                if (node.Body == null && withScope != null)
                {
                    // because the scope is empty, we now know it (it does nothing)
                    withScope.IsKnownAtCompileTime = true;
                }
            }
        }

        private string GuessAtName(AstNode node)
        {
            var parent = node.Parent;
            if (parent != null)
            {
                if (parent is AstNodeList)
                {
                    // if the parent is an ASTList, then we're really interested
                    // in our parent's parent (probably a call)
                    parent = parent.Parent;
                }
                CallNode call = parent as CallNode;
                if (call != null && call.IsConstructor)
                {
                    // if this function expression is the object of a new, then we want the parent
                    parent = parent.Parent;
                }

                string guess = parent.GetFunctionGuess(node);
                if (guess != null && guess.Length > 0)
                {
                    if (guess.StartsWith("\"", StringComparison.Ordinal)
                      && guess.EndsWith("\"", StringComparison.Ordinal))
                    {
                        // don't need to wrap it in quotes -- it already is
                        return guess;
                    }
                    // wrap the guessed name in quotes
                    return "\"{0}\"".FormatInvariant(guess);
                }
                else
                {
                    return "anonymous_{0}".FormatInvariant(UniqueNumber);
                }
            }
            return string.Empty;
        }

        private static bool AreAssignmentsInVar(BinaryOperator binaryOp, Var varStatement)
        {
            bool areAssignmentsInVar = false;

            if (binaryOp != null)
            {
                // we only want to pop positive for the simple assign (=). If it's any of the 
                // complex assigns (+=, -=, etc) then we don't want to combine them.
                if (binaryOp.OperatorToken == JSToken.Assign)
                {
                    // see if the left-hand side is a simple lookup
                    Lookup lookup = binaryOp.Operand1 as Lookup;
                    if (lookup != null)
                    {
                        // it is. see if that variable is in the previous var statement
                        areAssignmentsInVar = varStatement.Contains(lookup.Name);
                    }
                }
                else if (binaryOp.OperatorToken == JSToken.Comma)
                {
                    // this is a comma operator, so we will return true only if both
                    // left and right operators are assignments to vars defined in the 
                    // var statement
                    areAssignmentsInVar = AreAssignmentsInVar(binaryOp.Operand1 as BinaryOperator, varStatement)
                        && AreAssignmentsInVar(binaryOp.Operand2 as BinaryOperator, varStatement);
                }
            }

            return areAssignmentsInVar;
        }

        private static void ConvertAssignmentsToVarDecls(BinaryOperator binaryOp, List<VariableDeclaration> varDecls, JSParser parser)
        {
            // we've already checked that the tree only contains simple assignments separate by commas,
            // but just in case we'll check for null anyway
            if (binaryOp != null)
            {
                if (binaryOp.OperatorToken == JSToken.Assign)
                {
                    // we've already cleared this as a simple lookup, but run the check just to be sure
                    Lookup lookup = binaryOp.Operand1 as Lookup;
                    if (lookup != null)
                    {
                        varDecls.Add(new VariableDeclaration(
                            binaryOp.Context.Clone(),
                            parser,
                            lookup.Name,
                            lookup.Context.Clone(),
                            binaryOp.Operand2,
                            0,
                            true));
                    }
                }
                else if (binaryOp.OperatorToken == JSToken.Comma)
                {
                    // recurse both operands
                    ConvertAssignmentsToVarDecls(binaryOp.Operand1 as BinaryOperator, varDecls, parser);
                    ConvertAssignmentsToVarDecls(binaryOp.Operand2 as BinaryOperator, varDecls, parser);
                }
                // shouldn't ever be anything but these two operators
            }
        }

        private static void StripDebugStatements(Block node)
        {
            // walk the list backwards
            for (int ndx = node.Count - 1; ndx >= 0; --ndx)
            {
                // if this item pops positive...
                if (node[ndx].IsDebuggerStatement)
                {
                    // just remove it
                    node.RemoveAt(ndx);
                }
            }
        }

        private static bool VarDeclExists(Var node, int ndx, string name)
        {
            // only need to look forward from the index passed
            for (; ndx < node.Count; ++ndx)
            {
                // string must be exact match
                if (string.CompareOrdinal(node[ndx].Identifier, name) == 0)
                {
                    // there is at least one -- we can bail
                    return true;
                }
            }
            // if we got here, we didn't find any matches
            return false;
        }

        private static void DeleteNoInits(Var node, int min, string name)
        {
            // walk backwards from the end of the list down to (and including) the minimum index
            for (int ndx = node.Count - 1; ndx >= min; --ndx)
            {
                // if the name matches and there is no initializer...
                if (string.CompareOrdinal(node[ndx].Identifier, name) == 0
                    && node[ndx].Initializer == null)
                {
                    // ...remove it from the list
                    node.RemoveAt(ndx);
                }
            }
        }

        private UnaryOperator CreateVoidNode()
        {
            return new UnaryOperator(null, m_parser, new ConstantWrapper(0.0, PrimitiveType.Number, null, m_parser), JSToken.Void, false);
        }
    }
}
