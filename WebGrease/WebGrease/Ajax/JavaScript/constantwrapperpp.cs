// constantwrapperpp.cs
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

using System.Text;

namespace Microsoft.Ajax.Utilities
{
    public class ConstantWrapperPP : Expression
    {
        private string m_varName;
        public string VarName { get { return m_varName; } }

        public bool ForceComments { get; private set; }

        public ConstantWrapperPP(string varName, bool forceComments, Context context, JSParser parser)
            : base(context, parser)
        {
            m_varName = varName;
            ForceComments = forceComments;
        }

        public override void Accept(IVisitor visitor)
        {
            if (visitor != null)
            {
                visitor.Visit(this);
            }
        }
    }
}
