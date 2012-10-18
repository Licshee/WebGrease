// globalobject.cs
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

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Ajax.Utilities
{
    internal sealed class GlobalObject
    {
        private Dictionary<string, JSVariableField> m_fieldMap;

        public GlobalObject(string[] properties, string[] methods)
        {
            m_fieldMap = new Dictionary<string, JSVariableField>();

            // itemize all the properties
            if (properties != null)
            {
                foreach (string fieldName in properties)
                {
                    var newField = new JSVariableField(FieldType.Predefined, fieldName, 0, null);
                    m_fieldMap.Add(fieldName, newField);
                }
            }

            // itemize all the methods
            if (methods != null)
            {
                foreach (string fieldName in methods)
                {
                    var newField = new JSVariableField(FieldType.Predefined, fieldName, 0, null);
                    newField.IsFunction = true;
                    m_fieldMap.Add(fieldName, newField);
                }
            }
        }

        public JSVariableField GetField(string name)
        {
            if (m_fieldMap.ContainsKey(name))
            {
                return m_fieldMap[name];
            }
            return null;
        }
    }
}
