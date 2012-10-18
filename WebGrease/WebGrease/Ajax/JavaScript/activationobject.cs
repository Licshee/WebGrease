// activationobject.cs
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

namespace Microsoft.Ajax.Utilities
{
    public abstract class ActivationObject
    {
        private bool m_isKnownAtCompileTime;
        public bool IsKnownAtCompileTime
        {
            get { return m_isKnownAtCompileTime; }
            set { m_isKnownAtCompileTime = value; }
        }

        private Dictionary<string, JSVariableField> m_nameTable;
        public Dictionary<string, JSVariableField> NameTable { get { return m_nameTable; } }

        private List<JSVariableField> m_fieldTable;
        public IList<JSVariableField> FieldTable { get { return m_fieldTable; } }

        private List<ActivationObject> m_childScopes;
        public IList<ActivationObject> ChildScopes { get { return m_childScopes; } }

        internal Dictionary<JSVariableField, JSVariableField> Verboten { get; private set; }

        private ActivationObject m_parent;
        public ActivationObject Parent
        {
            get { return m_parent; }
        }

        private bool m_useStrict;//= false;

        private JSParser m_parser;
        protected JSParser Parser
        {
            get { return m_parser; }
        }

        public bool IsInWithScope
        {
            get
            {
                // start with this scope
                ActivationObject scope = this;

                // go up the scope heirarchy until we are either a withscope or null
                while (scope != null && !(scope is WithScope))
                {
                    scope = scope.Parent;
                }

                // if we are not null at this point, then we must be a with-scope
                return scope != null;
            }
        }

        protected ActivationObject(ActivationObject parent, JSParser parser)
        {
            m_parent = parent;
            m_nameTable = new Dictionary<string, JSVariableField>();
            m_fieldTable = new List<JSVariableField>();
            m_childScopes = new List<ActivationObject>();
            Verboten = new Dictionary<JSVariableField, JSVariableField>(32);
            m_isKnownAtCompileTime = true;
            m_parser = parser;

            // if our parent is a scope....
            if (parent != null)
            {
                // add us to the parent's list of child scopes
                parent.m_childScopes.Add(this);

                // if the parent is strict, so are we
                UseStrict = parent.UseStrict;
            }
        }

        public bool UseStrict
        {
            get
            {
                return m_useStrict;
            }
            set
            {
                // can set it to true, but can't set it to false
                if (value)
                {
                    // set our value
                    m_useStrict = value;

                    // and all our child scopes (recursive)
                    foreach (var child in m_childScopes)
                    {
                        child.UseStrict = value;
                    }
                }
            }
        }

        internal virtual void AnalyzeScope()
        {
            // check for unused local fields or arguments
            foreach (JSVariableField variableField in m_nameTable.Values)
            {
                if ((variableField.FieldType == FieldType.Local || variableField.FieldType == FieldType.Argument)
                    && !variableField.IsReferenced 
                    && variableField.OriginalContext != null)
                {
                    var funcObject = variableField.FieldValue as FunctionObject;
                    if (funcObject != null)
                    {
                        // if there's no function name, do nothing
                        if (funcObject.Name != null)
                        {
                            // if the function name isn't a simple identifier, then leave it there and mark it as
                            // not renamable because it's probably one of those darn IE-extension event handlers or something.
                            if (JSScanner.IsValidIdentifier(funcObject.Name))
                            {
                                // unreferenced function declaration.
                                // hide it from the output if our settings say we can
                                if (IsKnownAtCompileTime
                                    && funcObject.Parser.Settings.MinifyCode
                                    && funcObject.Parser.Settings.RemoveUnneededCode)
                                {
                                    funcObject.HideFromOutput = true;
                                }

                                // and fire an error
                                Context ctx = ((FunctionObject)variableField.FieldValue).IdContext;
                                if (ctx == null) { ctx = variableField.OriginalContext; }
                                ctx.HandleError(JSError.FunctionNotReferenced, false);
                            }
                            else
                            {
                                // not a valid identifier name for this function. Don't rename it.
                                variableField.CanCrunch = false;
                            }
                        }
                    }
                    else if (!variableField.IsGenerated)
                    {
                        if (variableField.FieldType == FieldType.Argument)
                        {
                            // we only want to throw this error if it's possible to remove it
                            // from the argument list. And that will only happen if there are
                            // no REFERENCED arguments after this one in the formal parameter list.
                            // Assertion: because this is an argument, this should be a function scope,
                            // let's walk up to the first function scope we find, just in case.
                            FunctionScope functionScope = this as FunctionScope;
                            if (functionScope == null)
                            {
                                ActivationObject scope = this.Parent;
                                while (scope != null)
                                {
                                    functionScope = scope as FunctionScope;
                                    if (scope != null)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (functionScope == null || functionScope.IsArgumentTrimmable(variableField))
                            {
                                variableField.OriginalContext.HandleError(
                                  JSError.ArgumentNotReferenced,
                                  false
                                  );
                            }
                        }
                        else if (variableField.OuterField == null || !variableField.OuterField.IsReferenced)
                        {
                            variableField.OriginalContext.HandleError(
                              JSError.VariableDefinedNotReferenced,
                              false
                              );
                        }
                    }
                }
            }

            // rename fields if we need to
            RenameFields();

            // recurse 
            foreach (ActivationObject activationObject in m_childScopes)
            {
                try
                {
                    Parser.ScopeStack.Push(activationObject);
                    activationObject.AnalyzeScope();
                }
                finally
                {
                    Parser.ScopeStack.Pop();
                }
            }
        }

        protected void RenameFields()
        {
            // if the local-renaming kill switch is on, we won't be renaming ANYTHING, so we'll have nothing to do.
            if (Parser.Settings.IsModificationAllowed(TreeModifications.LocalRenaming))
            {
                // if the parser settings has a list of rename pairs, we will want to go through and rename
                // any matches
                if (Parser.Settings.HasRenamePairs)
                {
                    // go through the list of fields in this scope. Anything defined in the script that
                    // is in the parser rename map should be renamed and the auto-rename flag reset so
                    // we don't change it later.
                    foreach (var varField in m_nameTable.Values)
                    {
                        // don't rename outer fields (only actual fields), 
                        // and we're only concerned with global or local variables --
                        // those which are defined by the script (not predefined, not the arguments object)
                        if (varField.OuterField == null 
                            && (varField.FieldType != FieldType.Arguments && varField.FieldType != FieldType.Predefined))
                        {
                            // see if the name is in the parser's rename map
                            string newName = Parser.Settings.GetNewName(varField.Name);
                            if (!string.IsNullOrEmpty(newName))
                            {
                                // it is! Change the name of the field, but make sure we reset the CanCrunch flag
                                // or setting the "crunched" name won't work.
                                // and don't bother making sure the name doesn't collide with anything else that
                                // already exists -- if it does, that's the developer's fault.
                                // TODO: should we at least throw a warning?
                                varField.CanCrunch = true;
                                varField.CrunchedName = newName;

                                // and make sure we don't crunch it later
                                varField.CanCrunch = false;
                            }
                        }
                    }
                }

                // if the parser settings has a list of no-rename names, then we will want to also mark any
                // fields that match and are still slated to rename as uncrunchable so they won't get renamed.
                // if the settings say we're not going to renaming anything automatically (KeepAll), then we 
                // have nothing to do.
                if (Parser.Settings.LocalRenaming != LocalRenaming.KeepAll)
                {
                    foreach (var noRename in Parser.Settings.NoAutoRenameCollection)
                    {
                        // don't rename outer fields (only actual fields), 
                        // and we're only concerned with fields that can still
                        // be automatically renamed. If the field is all that AND is listed in
                        // the collection, set the CanCrunch to false
                        JSVariableField varField;
                        if (m_nameTable.TryGetValue(noRename, out varField)
                            && varField.OuterField == null
                            && varField.CanCrunch)
                        {
                            // no, we don't want to crunch this field
                            varField.CanCrunch = false;
                        }
                    }
                }
            }
        }

        #region crunching methods

        internal virtual void ReserveFields()
        {
            // traverse through our children first to get depth-first
            foreach (ActivationObject scope in m_childScopes)
            {
                scope.ReserveFields();
            }

            // do the actual work now that we've recursed
            DoReserveFields();
        }

        private void DoReserveFields()
        {
            // then reserve all our fields that need reserving
            // check for unused local fields or arguments
            foreach (JSVariableField variableField in m_nameTable.Values)
            {
                if (variableField.CanCrunch)
                {
                    // this variable is a target for renaming.
                    // if this is a named-function-expression name, then we want to use the name of the 
                    // outer field so we don't collide in IE
                    if (variableField.FieldType == FieldType.NamedFunctionExpression)
                    {
                        // make sure the field is in this scope's verboten list so we don't accidentally reuse
                        // an outer scope variable name
                        if (!Verboten.ContainsKey(variableField))
                        {
                            Verboten.Add(variableField, variableField);
                        }

                        // we don't need to reserve up the scope because the named function expression's
                        // "outer" field is always in the very next scope
                    }
                    else if (variableField.OuterField != null)
                    {
                        // if the outer field is not null, then this field (not the name) needs to be 
                        // reserved up the scope chain until the scope where it's defined.
                        // make sure the field is in this scope's verboten list so we don't accidentally reuse
                        // the outer scope's variable name
                        if (!Verboten.ContainsKey(variableField))
                        {
                            Verboten.Add(variableField, variableField);
                        }

                        for (var scope = this; scope != null; scope = scope.Parent)
                        {
                            // get the local field by this name (if any)
                            var scopeField = scope.GetLocalField(variableField.Name);
                            if (scopeField == null)
                            {
                                // it's not referenced in this scope -- if the field isn't in the verboten
                                // list, add it now
                                if (!scope.Verboten.ContainsKey(variableField))
                                {
                                    scope.Verboten.Add(variableField, variableField);
                                }
                            }
                            else if (scopeField.OuterField == null)
                            {
                                // found AN original field -- see if it matches our field's outer field
                                // OR our outer field's outer field, etc
                                if (IsOuterParent(variableField, scopeField))
                                {
                                    // this is the outer field -- we're done
                                    break;
                                }
                                else
                                {
                                    // NOT A MATCH! we have an ambiguous situation here. When we resolved
                                    // the original variable, it resolved to something farther up the chain.
                                    // let's add our variable to the verboten list so we don't get a naming
                                    // collision and keep moving up the chain
                                    if (!scope.Verboten.ContainsKey(variableField))
                                    {
                                        scope.Verboten.Add(variableField, variableField);
                                    }

                                    // one scenario this happens in is if a catch variable is named the same
                                    // name as a variable in a child scope that references an outer variable.
                                    // the catch variable adds a placeholder variable in the parent scope AFTER
                                    // the child reference is resolved to the outer scope.
                                    // throw a warning
                                    var context = variableField.OriginalContext ?? scopeField.OriginalContext;
                                    if (context != null)
                                    {
                                        context.HandleError(JSError.AmbiguousVariable, false);
                                    }
                                }
                            }
                        }
                    }
                    else if (m_parser.Settings.LocalRenaming == LocalRenaming.KeepLocalizationVars
                      && variableField.Name.StartsWith("L_", StringComparison.Ordinal))
                    {
                        // localization variable. don't crunch it.
                        // add it to this scope's verboten list in the extremely off-hand chance
                        // that a crunched variable might be the same pattern
                        if (!Verboten.ContainsKey(variableField))
                        {
                            Verboten.Add(variableField, variableField);
                        }
                    }
                }
                else if (variableField.FieldType == FieldType.Global
                    || variableField.FieldType == FieldType.Predefined)
                {
                    // this variable is NOT a target for renaming, but it's a global object
                    // reserve the name in this scope and all the way up the chain
                    for (var scope = this; scope != null; scope = scope.Parent)
                    {
                        if (!scope.Verboten.ContainsKey(variableField))
                        {
                            scope.Verboten.Add(variableField, variableField);
                        }
                    }
                }
                else
                {
                    // not target for renaming, but not a global. 
                    // (we may have already have a name picked out for it that we want to keep).
                    // add it to the verboten list, too.
                    if (!Verboten.ContainsKey(variableField))
                    {
                        Verboten.Add(variableField, variableField);
                    }

                    // if this is pointing to an outer field, we still need to reserve it
                    // all the way up the scope chain
                    if (variableField.OuterField != null)
                    {
                        for (var scope = this; scope != null; scope = scope.Parent)
                        {
                            // get the local field by this name (if any)
                            var scopeField = scope.GetLocalField(variableField.Name);
                            if (scopeField == null)
                            {
                                // it's not referenced in this scope -- if the field isn't in the verboten
                                // list, add it now
                                if (!scope.Verboten.ContainsKey(variableField))
                                {
                                    scope.Verboten.Add(variableField, variableField);
                                }
                            }
                            else if (scopeField.OuterField == null)
                            {
                                // found the original field -- stop looking
                                break;
                            }
                        }
                    }
                }
            }

            // finally, if this scope is not known at compile time, 
            // AND we know we want to make all affected scopes safe
            // for the eval statement
            // AND we are actually referenced by the enclosing scope, 
            // then our parent scope is also not known at compile time
            if (!m_isKnownAtCompileTime
                && Parser.Settings.EvalTreatment == EvalTreatment.MakeAllSafe)
            {
                ActivationObject parentScope = (ActivationObject)Parent;
                FunctionScope funcScope = this as FunctionScope;
                if (funcScope == null)
                {
                    // we're not a function -- parent is unknown too
                    parentScope.IsKnownAtCompileTime = false;
                }
                else
                {
                    var localField = parentScope.GetLocalField(funcScope.FunctionObject.Name);
                    if (localField == null || localField.IsReferenced)
                    {
                        parentScope.IsKnownAtCompileTime = false;
                    }
                }
            }
        }

        private static bool IsOuterParent(JSVariableField thisField, JSVariableField targetOuterField)
        {
            var isOuterParent = false;
            var outerField = thisField.OuterField;
            while (outerField != null)
            {
                // see if the outer reference matches the target
                if (outerField == targetOuterField)
                {
                    isOuterParent = true;
                    break;
                }

                // go up next level of outer references
                outerField = outerField.OuterField;
            }
            return isOuterParent;
        }

        internal virtual void ValidateGeneratedNames()
        {
            // check all the variables defined within this scope.
            // we're looking for uncrunched generated fields.
            foreach (JSVariableField variableField in m_fieldTable)
            {
                if (variableField.IsGenerated
                    && variableField.CrunchedName == null)
                {
                    // we need to rename this field.
                    // first we need to walk all the child scopes depth-first
                    // looking for references to this field. Once we find a reference,
                    // we then need to add all the other variables referenced in those
                    // scopes and all above them (from here) so we know what names we
                    // can't use.
                    Dictionary<string, string> avoidTable = new Dictionary<string, string>();
                    GenerateAvoidList(avoidTable, variableField.Name);

                    // now that we have our avoid list, create a crunch enumerator from it
                    CrunchEnumerator crunchEnum = new CrunchEnumerator(avoidTable);

                    // and use it to generate a new name
                    variableField.CrunchedName = crunchEnum.NextName();
                }
            }

            // recursively traverse through our children
            foreach (ActivationObject scope in m_childScopes)
            {
                scope.ValidateGeneratedNames();
            }
        }

        private bool GenerateAvoidList(Dictionary<string, string> table, string name)
        {
            // our reference flag is based on what was passed to us
            bool isReferenced = false;

            // depth first, so walk all the children
            foreach (ActivationObject childScope in m_childScopes)
            {
                // if any child returns true, then it or one of its descendents
                // reference this variable. So we reference it, too
                if (childScope.GenerateAvoidList(table, name))
                {
                    // we'll return true because we reference it
                    isReferenced = true;
                }
            }
            if (!isReferenced)
            {
                // none of our children reference the scope, so see if we do
                if (m_nameTable.ContainsKey(name))
                {
                    isReferenced = true;
                }
            }

            if (isReferenced)
            {
                // if we reference the name or are in line to reference the name,
                // we need to add all the variables we reference to the list
                foreach (JSVariableField variableField in m_fieldTable)
                {
                    string fieldName = variableField.ToString();
                    if (!table.ContainsKey(fieldName))
                    {
                        table[fieldName] = fieldName;
                    }
                }
            }
            // return whether or not we are in the reference chain
            return isReferenced;
        }

        internal virtual void HyperCrunch()
        {
            // if we're not known at compile time, then we can't crunch
            // the local variables in this scope, because we can't know if
            // something will reference any of it at runtime.
            // eval is something that will make the scope unknown because we
            // don't know what eval will evaluate to until runtime
            if (m_isKnownAtCompileTime)
            {
                // get an array of all the uncrunched local variables defined in this scope
                JSVariableField[] localFields = GetUncrunchedLocals();
                if (localFields.Length > 0)
                {
                    // create a crunch-name enumerator, taking into account our verboten set
                    var crunchEnum = new CrunchEnumerator(Verboten);
                    for (int ndx = 0; ndx < localFields.Length; ++ndx)
                    {
                        JSVariableField localField = localFields[ndx];

                        // if we are an unambiguous reference to a named function expression and we are not
                        // referenced by anyone else, then we can just skip this variable because the
                        // name will be stripped from the output anyway.
                        // we also always want to crunch "placeholder" fields.
                        if (localField.CanCrunch
                            && (localField.RefCount > 0 || localField.IsDeclared || localField.IsPlaceholder
                            || !(Parser.Settings.RemoveFunctionExpressionNames && Parser.Settings.IsModificationAllowed(TreeModifications.RemoveFunctionExpressionNames))))
                        {
                            localFields[ndx].CrunchedName = crunchEnum.NextName();
                        }
                    }
                }
            }

            // then traverse through our children
            foreach (ActivationObject scope in m_childScopes)
            {
                scope.HyperCrunch();
            }
        }

        internal JSVariableField[] GetUncrunchedLocals()
        {
            // there can't be more uncrunched fields than total fields
            var list = new List<JSVariableField>(m_nameTable.Count);
            foreach (JSVariableField variableField in m_nameTable.Values)
            {
                // if the field is defined in this scope and hasn't been crunched
                // AND can still be crunched
                if (variableField != null && variableField.OuterField == null && variableField.CrunchedName == null
                    && variableField.CanCrunch)
                {
                    // if local renaming is not crunch all, then it must be crunch all but localization
                    // (we don't get called if we aren't crunching anything). 
                    // SO for the first clause:
                    // IF we are crunch all, we're good; but if we aren't crunch all, then we're only good if
                    //    the name doesn't start with "L_".
                    // The second clause is only computed IF we already think we're good to go.
                    // IF we aren't preserving function names, then we're good. BUT if we are, we're
                    // only good to go if this field doesn't represent a function object.
                    if ((m_parser.Settings.LocalRenaming == LocalRenaming.CrunchAll
                        || !variableField.Name.StartsWith("L_", StringComparison.Ordinal))
                        && !(m_parser.Settings.PreserveFunctionNames && variableField.IsFunction))
                    {
                        // don't add to our list if it's a function that's going to be hidden anyway
                        FunctionObject funcObject;
                        if (!variableField.IsFunction
                            || (funcObject = variableField.FieldValue as FunctionObject) == null
                            || !funcObject.HideFromOutput)
                        {
                            list.Add(variableField);
                        }
                    }
                }
            }
            // sort the array by reference count, descending
            list.Sort(ReferenceComparer.Instance);
            
            // return as an array
            return list.ToArray();
        }

        #endregion

        #region field-management methods

        public virtual JSVariableField this[string name]
        {
            get
            {
                JSVariableField variableField;
                // check to see if this name is already defined in this scope
                if (!m_nameTable.TryGetValue(name, out variableField))
                {
                    // not in this scope
                    variableField = null;
                }
                return variableField;
            }
        }

        public virtual JSVariableField FindReference(string name)
        {
            // see if we have it
            JSVariableField variableField = this[name];
            // if we didn't find anything and this scope has a parent
            if (variableField == null && Parent != null)
            {
                // recursively go up the scope chain
                variableField = Parent.FindReference(name);
            }
            return variableField;
        }

        public virtual JSVariableField DeclareField(string name, object value, FieldAttributes attributes)
        {
            JSVariableField variableField;
            if (!m_nameTable.TryGetValue(name, out variableField))
            {
                variableField = CreateField(name, value, attributes);
                AddField(variableField);
            }
            return variableField;
        }

        public abstract JSVariableField CreateField(JSVariableField outerField);
        public abstract JSVariableField CreateField(string name, object value, FieldAttributes attributes);

        public virtual JSVariableField CreateInnerField(JSVariableField outerField)
        {
            JSVariableField innerField;
            if (outerField != null &&
                (outerField.FieldType == FieldType.Global || outerField.FieldType == FieldType.Predefined))
            {
                // if this is a global or predefined field, then just add the field itself
                // to the local scope. We don't want to create a local reference.
                innerField = outerField;
            }
            else
            {
                // create a new inner field to be added to our scope
                innerField = CreateField(outerField);
            }

            // add the field to our scope and return it
            AddField(innerField);
            return innerField;
        }

        internal JSVariableField AddField(JSVariableField variableField)
        {
            m_nameTable[variableField.Name] = variableField;
            m_fieldTable.Add(variableField);
            return variableField;
        }

        public virtual JSVariableField GetLocalField(string name)
        {
            JSVariableField localField;
            return (m_nameTable.TryGetValue(name, out localField)) ? localField : null;
        }

        #endregion

        public JSVariableField[] GetFields()
        {
            return m_fieldTable.ToArray();
        }
    }
}