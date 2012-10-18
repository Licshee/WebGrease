// catchscope.cs
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

using System.Collections;
using System.Reflection;

namespace Microsoft.Ajax.Utilities
{

    public sealed class CatchScope : BlockScope
    {
        private string m_name;

        public JSVariableField CatchField { get; private set; }

        internal CatchScope(ActivationObject parent, Context argContext, JSParser parser)
            : base(parent, argContext, parser)
        {
            // get the name of the catch variable
            m_name = Context.Code;

            // add it to the catch-scope's name table
            CatchField = new JSVariableField(FieldType.Argument, m_name, 0, null);
            CatchField.OriginalContext = argContext;
            NameTable[m_name] = CatchField;
            FieldTable.Add(CatchField);
        }

        public override JSVariableField CreateField(JSVariableField outerField)
        {
            return new JSVariableField(FieldType.Local, outerField);
        }

        public override JSVariableField CreateField(string name, object value, FieldAttributes attributes)
        {
            return new JSVariableField(FieldType.Local, name, attributes, value);
        }

        internal override void AnalyzeScope()
        {
            if (Context != null)
            {
                // get the parent global/function scope where variables defined within our
                // scope are REALLY defined
                ActivationObject definingScope = this;
                do
                {
                    definingScope = definingScope.Parent;
                } while (definingScope is BlockScope);

                // see if there is a variable already defined in that scope with the same name
                JSVariableField outerField = definingScope[m_name];
                if (outerField != null)
                {
                    // there is one defined already!!!
                    // but if it isn't referenced, it's safe to just use it
                    // as the outer field for our catch variable so our names
                    // stay in sync when we rename stuff
                    if (outerField.IsReferenced)
                    {
                        // but the outer field IS referenced somewhere! We have a possible ambiguous
                        // catch variable problem that behaves differently in IE and non-IE browsers.
                        Context.HandleError(JSError.AmbiguousCatchVar);
                    }
                }
                else
                {
                    // there isn't one defined -- add one and hook it to our argument
                    // field as the outer reference so the name doesn't collide with any
                    // other fields in that scope if we are renaming fields
                    outerField = definingScope.CreateField(m_name, null, 0);
                    outerField.IsPlaceholder = true;
                    outerField.OriginalContext = this.Context;
                    definingScope.AddField(outerField);
                }

                // point our inner catch variable to the outer variable
                this[m_name].OuterField = outerField;
            }
            base.AnalyzeScope();
        }

        internal override void HyperCrunch()
        {
            // the block scope is used for catch blocks. 
            // we don't want to introduce possible cross-browser problems, so
            // we need to make sure we don't rename our catch parameter to anything
            // already existing in our parent function scope.
            //
            // so walk through the parents (up to the first function or the global scope)
            // and add all existing crunched variable names to our verboten list.
            ActivationObject parentScope = Parent;
            while (parentScope != null)
            {
                // take all the variable names (crunched if they're crunched)
                // and add them to this block's verboten list if they aren't already.
                if (parentScope.NameTable.Count > 0)
                {
                    foreach (var variableField in parentScope.NameTable.Values)
                    {
                        // add it to our verboten list
                        if (!Verboten.ContainsKey(variableField))
                        {
                            Verboten.Add(variableField, variableField);
                        }
                    }
                }

                // also add everything in the parent's verboten list
                if (parentScope.Verboten.Count > 0)
                {
                    foreach (var variableField in parentScope.Verboten.Keys)
                    {
                        // add it to our verboten list
                        if (!Verboten.ContainsKey(variableField))
                        {
                            Verboten.Add(variableField, variableField);
                        }
                    }
                }

                // stop as soon as we've processed a funciton or global scope
                if (!(parentScope is BlockScope))
                {
                    break;
                }
                // next parent
                parentScope = parentScope.Parent;
            }

            // then just perform as usual
            base.HyperCrunch();
        }
    }
}