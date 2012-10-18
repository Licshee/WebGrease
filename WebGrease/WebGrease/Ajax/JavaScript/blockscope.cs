// blockscope.cs
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

using System.Reflection;

namespace Microsoft.Ajax.Utilities
{
    public abstract class BlockScope : ActivationObject
    {
        private Context m_context;// = null;
        public Context Context
        {
            get { return m_context; }
        }

        // not instantiated directly, only through derived classes
        protected BlockScope(ActivationObject parent, Context context, JSParser parser)
            : base(parent, parser)
        {
            m_context = (context == null ? new Context(parser) : context.Clone());
        }

        public override JSVariableField this[string name]
        {
            get
            {
                // check this name table
                JSVariableField variableField = base[name];
                if (variableField == null)
                {
                    // we need to keep checking until we hit a non-block scope?
                }
                return variableField;
            }
        }

        public override JSVariableField DeclareField(string name, object value, FieldAttributes attributes)
        {
            JSVariableField variableField;
            if (!NameTable.TryGetValue(name, out variableField))
            {
                // find the owning scope where variables are defined
                ActivationObject owningScope = Parent;
                while (owningScope is BlockScope)
                {
                    owningScope = owningScope.Parent;
                }
                // create the variable in that scope
                variableField = owningScope.DeclareField(name, value, attributes);
                // and create an inner-reference in our scope
                variableField = CreateInnerField(variableField);
            }
            return variableField;
        }
    }
}