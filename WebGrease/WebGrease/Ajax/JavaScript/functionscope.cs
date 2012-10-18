// functionscope.cs
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
using System.Reflection;

namespace Microsoft.Ajax.Utilities
{
    public sealed class FunctionScope : ActivationObject
    {
        public FunctionObject FunctionObject { get; set; }

        private Dictionary<ActivationObject, ActivationObject> m_refScopes;

        internal FunctionScope(ActivationObject parent, bool isExpression, JSParser parser)
            : base(parent, parser)
        {
            if (isExpression)
            {
                // parent scopes automatically reference enclosed function expressions
                AddReference(Parent);
            }
        }

        internal override void AnalyzeScope()
        {
            if (FunctionObject != null)
            {
                // default processing
                base.AnalyzeScope();
            }
        }

        internal JSVariableField AddNewArgumentField(String name)
        {
            var result = new JSVariableField(FieldType.Argument, name, 0, Missing.Value);
            AddField(result);
            return result;
        }

        internal JSVariableField AddArgumentsField()
        {
            var arguments = new JSVariableField(FieldType.Arguments, "arguments", 0, null);
            AddField(arguments);
            return arguments;
        }

        internal bool IsArgumentTrimmable(JSVariableField argumentField)
        {
            return FunctionObject.IsArgumentTrimmable(argumentField);
        }

        public override JSVariableField FindReference(string name)
        {
            JSVariableField variableField = this[name];
            if (variableField == null)
            {
                // didn't find a field in this scope.
                // special to function scopes: check to see if this is the arguments object
                if (string.Compare(name, "arguments", StringComparison.Ordinal) == 0)
                {
                    // this is a reference to the arguments object, so add the 
                    // arguments field to the scope and return it
                    variableField = AddArgumentsField();
                }
                else
                {
                    // recurse up the parent chain
                    variableField = Parent.FindReference(name);
                }
            }
            return variableField;
        }

        public override JSVariableField CreateField(string name, object value, FieldAttributes attributes)
        {
            return new JSVariableField(FieldType.Local, name, attributes, value);
        }

        public override JSVariableField CreateField(JSVariableField outerField)
        {
            return new JSVariableField(FieldType.Local, outerField);
        }

        internal void AddReference(ActivationObject scope)
        {
            // make sure the hash is created
            if (m_refScopes == null)
            {
                m_refScopes = new Dictionary<ActivationObject, ActivationObject>();
            }
            // we don't want to include block scopes or with scopes -- they are really
            // contained within their parents
            while (scope != null && scope is BlockScope)
            {
                scope = scope.Parent;
            }
            if (scope != null && !m_refScopes.ContainsKey(scope))
            {
                // add the scope to the hash
                m_refScopes.Add(scope, scope);
            }
        }

        public bool IsReferenced(IDictionary<ActivationObject, ActivationObject> visited)
        {
            // first off, if the parent scope of this scope is a global scope, 
            // then we're a global function and referenced by default.
            if (Parent is GlobalScope)
            {
                return true;
            }

            // if we were passed null, then create a new hash table for us to pass on
            if (visited == null)
            {
                visited = new Dictionary<ActivationObject, ActivationObject>();
            }

            // add our scope to the visited hash
            if (!visited.ContainsKey(this))
            {
                visited.Add(this, this);
            }

            // now we walk the hash of referencing scopes and try to find one that that is
            if (m_refScopes != null)
            {
                foreach (ActivationObject referencingScope in m_refScopes.Keys)
                {
                    // skip any that we've already been to
                    if (!visited.ContainsKey(referencingScope))
                    {
                        // if we are referenced by the global scope, then we are referenced
                        if (referencingScope is GlobalScope)
                        {
                            return true;
                        }

                        // if this is a function scope, traverse through it
                        FunctionScope functionScope = referencingScope as FunctionScope;
                        if (functionScope != null && functionScope.IsReferenced(visited))
                        {
                            return true;
                        }
                    }
                }
            }

            // if we get here, then we didn't find any referencing scopes
            // that were referenced
            return false;
        }
    }
}
