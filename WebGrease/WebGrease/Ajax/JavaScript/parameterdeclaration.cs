// parameterdeclaration.cs
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

namespace Microsoft.Ajax.Utilities
{
    public sealed class ParameterDeclaration
    {
        public JSVariableField Field { get; private set; }

        public string Name
        {
            get
            {
                return (Field != null ? Field.ToString() : m_name);
            }
        }

        public string OriginalName
        {
            get { return m_name; }
        }
        private string m_name;

        public Context Context { get { return m_context; } }
        private Context m_context;

        public ParameterDeclaration(Context context, JSParser parser, string identifier, int position)
        {
            m_name = identifier;
            m_context = (context != null ? context : new Context(parser));

            FunctionScope functionScope = parser != null ? parser.ScopeStack.Peek() as FunctionScope : null;
            if (functionScope != null)
            {
                if (!functionScope.NameTable.ContainsKey(m_name))
                {
                    Field = functionScope.AddNewArgumentField(m_name);
                    Field.OriginalContext = m_context;
                    Field.Position = position;
                }
            }
            else
            {
                // parameters should only be under a function scope
                m_context.HandleError(
                  JSError.DuplicateName,
                  m_name,
                  true
                  );
            }
        }
    }
}
