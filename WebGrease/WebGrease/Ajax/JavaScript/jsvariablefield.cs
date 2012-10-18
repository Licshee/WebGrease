// jsvariablefield.cs
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
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Ajax.Utilities
{
    /// <summary>
    /// Field type enumeration
    /// </summary>
    public enum FieldType
    {
        Local,
        Predefined,
        Global,
        Arguments,
        Argument,
        WithField,
        NamedFunctionExpression,
    }

    public class JSVariableField
    {
        // never update this context object. It is shared
        public Context OriginalContext { get; set; }

        public string Name { get; private set; }
        public FieldType FieldType { get; private set; }
        public bool IsFunction { get; internal set; }

        public JSVariableField NamedFunctionExpression { get; set; }
        public bool IsAmbiguous { get; set; }
        public bool IsPlaceholder { get; set; }
        public int RefCount { get; private set; }
        public JSVariableField OuterField { get; set; }
        public Object FieldValue { get; set; }
        public FieldAttributes Attributes { get; set; }
        public int Position { get; set; }
        public bool InitializationOnly { get; set; }

        public bool IsLiteral
        {
            get
            {
                return ((Attributes & FieldAttributes.Literal) != 0);
            }
        }

        private bool m_canCrunch;// = false;
        public bool CanCrunch
        {
            get { return m_canCrunch; }
            set 
            { 
                m_canCrunch = value;

                // if there is an outer field, we only want to propagate
                // our crunch setting if we are setting it to false. We never
                // want to set an outer field to true because we might have already
                // determined that we can't crunch it.
                if (OuterField != null && !value)
                {
                    OuterField.CanCrunch = false;
                }
            }
        }

        private bool m_isDeclared; //= false;
        public bool IsDeclared
        {
            get { return m_isDeclared; }
            set 
            { 
                m_isDeclared = value;
                if (OuterField != null)
                {
                    OuterField.IsDeclared = value;
                }
            }
        }

        private bool m_isGenerated;
        public bool IsGenerated
        {
            get
            {
                // if we are pointing to an outer field, return ITS flag, not ours
                return OuterField != null ? OuterField.IsGenerated : m_isGenerated;
            }
            set
            {
                // always set our flag, just in case
                m_isGenerated = value;

                // if we are pointing to an outer field, set it's flag as well
                if (OuterField != null)
                {
                    OuterField.IsGenerated = value;
                }
            }
        }

        // we'll set this after analyzing all the variables in the
        // script in order to shrink it down even further
        private string m_crunchedName;// = null;
        public string CrunchedName
        {
            get
            {
                // return the outer field's crunched name if there is one,
                // otherwise return ours
                return (OuterField != null
                    ? OuterField.CrunchedName
                    : m_crunchedName);
            }
            set
            {
                // only set this if we CAN
                if (m_canCrunch)
                {
                    // if this is an outer reference, pass this on to the outer field
                    if (OuterField != null)
                    {
                        OuterField.CrunchedName = value;
                    }
                    else
                    {
                        m_crunchedName = value;
                    }
                }
            }
        }

        // we'll set this to true if the variable is referenced in a lookup
        public bool IsReferenced
        {
            get
            {
                // if the refcount is zero, we know we're not referenced.
                // if the count is greater than zero and we're a function definition,
                // then we need to do a little more work
                FunctionObject funcObj = FieldValue as FunctionObject;
                if (funcObj != null)
                {
                    // ask the function object if it's referenced. 
                    // Pass the field refcount because it would be useful for func declarations
                    return funcObj.IsReferenced(RefCount);
                }
                return RefCount > 0;
            }
        }

        public JSVariableField(FieldType fieldType, string name, FieldAttributes fieldAttributes, object value)
        {
            Name = name;
            Attributes = fieldAttributes;
            FieldValue = value;
            SetFieldsBasedOnType(fieldType);
        }

        internal JSVariableField(FieldType fieldType, JSVariableField outerField)
        {
            // set values based on the outer field
            Debug.Assert(outerField != null, "Parameter outerField cannot be null");
            OuterField = outerField;
            Name = outerField.Name;
            Attributes = outerField.Attributes;
            FieldValue = outerField.FieldValue;
            IsGenerated = outerField.IsGenerated;

            // and set some other fields on our object based on the type we are
            SetFieldsBasedOnType(fieldType);
        }

        private void SetFieldsBasedOnType(FieldType fieldType)
        {
            FieldType = fieldType;
            switch (FieldType)
            {
                case FieldType.Argument:
                    IsDeclared = true;
                    CanCrunch = true;
                    break;

                case FieldType.Arguments:
                    CanCrunch = false;
                    break;

                case FieldType.Global:
                    CanCrunch = false;
                    break;

                case FieldType.Local:
                    CanCrunch = true;
                    break;

                case FieldType.NamedFunctionExpression:
                    CanCrunch = OuterField == null ? true : OuterField.CanCrunch;
                    IsFunction = true;
                    break;

                case FieldType.Predefined:
                    CanCrunch = false;
                    break;

                case FieldType.WithField:
                    CanCrunch = false;
                    break;

                default:
                    // shouldn't get here
                    throw new ArgumentException("Invalid field type", "fieldType");
            }
        }

        public void AddReference(ActivationObject scope)
        {
            // if we have an outer field, add the reference to it
            if (OuterField != null)
            {
                OuterField.AddReference(scope);
            }

            ++RefCount;
            if (FieldValue is FunctionObject)
            {
                // add the reference to the scope
                ((FunctionObject)FieldValue).FunctionScope.AddReference(scope);
            }

            // no longer a placeholder if we are referenced
            if (IsPlaceholder)
            {
                IsPlaceholder = false;
            }
        }

        public void Detach()
        {
            OuterField = null;
        }

        public override string ToString()
        {
            string crunch = CrunchedName;
            return string.IsNullOrEmpty(crunch) ? Name : crunch;
        }


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// returns true if the fields point to the same ultimate reference object.
        /// Needs to walk up the outer-reference chain for each field in order to
        /// find the ultimate reference
        /// </summary>
        /// <param name="otherField"></param>
        /// <returns></returns>
        public bool IsSameField(JSVariableField otherField)
        {
            // shortcuts -- if they are already the same object, we're done;
            // and if the other field is null, then we are NOT the same object.
            if (this == otherField)
            {
                return true;
            }
            else if (otherField == null)
            {
                return false;
            }

            // get the ultimate field for this field
            var thisOuter = OuterField != null ? OuterField : this;
            while (thisOuter.OuterField != null)
            {
                thisOuter = thisOuter.OuterField;
            }

            // get the ultimate field for the other field
            var otherOuter = otherField.OuterField != null ? otherField.OuterField : otherField;
            while (otherOuter.OuterField != null)
            {
                otherOuter = otherOuter.OuterField;
            }

            // now that we have the same outer fields, check to see if they are the same
            return thisOuter == otherOuter;
        }
    }
}