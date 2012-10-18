// withscope.cs
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
    public sealed class WithScope : BlockScope
    {
        public WithScope(ActivationObject parent, Context context, JSParser parser)
            : base(parent, context, parser)
        {
            // with statements are unknown by default
            //IsKnownAtCompileTime = false;
        }

        public override JSVariableField FindReference(string name)
        {
            // call the base class, which will walk up the chain to find wherever the name lives
            JSVariableField variableField = base.FindReference(name);
            if (variableField != null)
            {
                // it exists, but is it in our with scope?
                if (this[name] == null)
                {
                    // it's not. We need to create an inner field pointing to this outer
                    // field so we can make sure the name doesn't get crunched
                    variableField = CreateInnerField(variableField);
                }
            }
            else
            {
                // because this is a with scope, if something isn't defined anywhere, we
                // don't want to throw an error because we very well may be referencing
                // a property on the object of the with scope.
                // So add a global field to our scope and return that instead, and use the
                // RTSpecialName flag to indicate that we don't really know what this thing could be.
                JSVariableField globalField = Parser.GlobalScope.AddField(
                    new JSVariableField(FieldType.Global, name, FieldAttributes.RTSpecialName, null)
                    );
                variableField = CreateInnerField(globalField);
            }
            return variableField;
        }

        public override JSVariableField CreateInnerField(JSVariableField outerField)
        {
            // blindly create an inner reference field for with scopes, no matter what it
            // is. globals and predefined values can be hijacked by object properties in
            // this scope.
            JSVariableField innerField = CreateField(outerField);
            AddField(innerField);
            return innerField;
        }

        public override JSVariableField CreateField(JSVariableField outerField)
        {
            return new JSVariableField(FieldType.WithField, outerField);
        }

        public override JSVariableField CreateField(string name, object value, FieldAttributes attributes)
        {
            return new JSVariableField(FieldType.WithField, name, attributes, null);
        }
    }
}
