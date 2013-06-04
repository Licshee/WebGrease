// globalscope.cs
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
using System.Collections.ObjectModel;
using System.Reflection;

namespace Microsoft.Ajax.Utilities
{
    public sealed class GlobalScope : ActivationObject
    {
        private HashSet<string> m_globalProperties;
        private HashSet<string> m_globalFunctions;
        private HashSet<string> m_assumedGlobals;
        private HashSet<UndefinedReferenceException> m_undefined;

        public ICollection<UndefinedReferenceException> UndefinedReferences { get { return m_undefined; } }

        internal GlobalScope(CodeSettings settings)
            : base(null, settings)
        {
            // define the Global object's properties, and methods
            m_globalProperties = new HashSet<string>(new[] { 
                "Infinity", "NaN", "undefined", "window", "Image", "JSON", "Math", "XMLHttpRequest", "DOMParser",
                "applicationCache", "clientInformation", "clipboardData", "closed", "console", "document", "event", "external", "frameElement", "frames", "history", "length", "localStorage", "location", "name", "navigator", "opener", "parent", "screen", "self", "sessionStorage", "status", "top"});

            m_globalFunctions = new HashSet<string>(new[] {
                "decodeURI", "decodeURIComponent", "encodeURI", "encodeURIComponent", "escape", "eval", "importScripts", "isNaN", "isFinite", "parseFloat", "parseInt", "unescape", "ActiveXObject", "Array", "Boolean", "Date", "Error", "EvalError", "EventSource", "File", "FileList", "FileReader", "Function", "GeckoActiveXObject", "HTMLElement", "Number", "Object", "Proxy", "RangeError", "ReferenceError", "RegExp", "SharedWorker", "String", "SyntaxError", "TypeError", "URIError", "WebSocket", "Worker",
                "addEventListener", "alert", "attachEvent", "blur", "clearInterval", "clearTimeout", "close", "confirm", "createPopup", "detachEvent", "dispatchEvent", "execScript", "focus", "getComputedStyle", "getSelection", "moveBy", "moveTo", "navigate", "open", "postMessage", "prompt", "removeEventListener", "resizeBy", "resizeTo", "scroll", "scrollBy", "scrollTo", "setActive", "setInterval", "setTimeout", "showModalDialog", "showModelessDialog" });
        }

        /// <summary>
        /// Set up this scopes lexically- and var-declared fields
        /// </summary>
        public override void DeclareScope()
        {
            // bind lexical declarations
            DefineLexicalDeclarations();

            // bind the variable declarations
            DefineVarDeclarations();
        }

        public void AddUndefinedReference(UndefinedReferenceException exception)
        {
            if (m_undefined == null)
            {
                m_undefined = new HashSet<UndefinedReferenceException>();
            }

            m_undefined.Add(exception);
        }

        internal void SetAssumedGlobals(CodeSettings settings)
        {
            if (settings != null)
            {
                // start off with any known globals
                m_assumedGlobals = settings.KnownGlobalCollection == null ? new HashSet<string>() : new HashSet<string>(settings.KnownGlobalCollection);

                // chek to see if there are any debug lookups
                foreach (var debugLookup in settings.DebugLookupCollection)
                {
                    m_assumedGlobals.Add(debugLookup.SubstringUpToFirst('.'));
                }

                // and the root name of any resource strings is also an assumed global
                foreach (var resourceStrings in settings.ResourceStrings)
                {
                    if (!resourceStrings.Name.IsNullOrWhiteSpace())
                    {
                        m_assumedGlobals.Add(resourceStrings.Name.SubstringUpToFirst('.'));
                    }
                }
            }
            else
            {
                // empty set
                m_assumedGlobals = new HashSet<string>();
            }
        }

        internal override void AnalyzeScope()
        {
            // rename fields if we need to
            ManualRenameFields();

            // recurse 
            foreach (var activationObject in ChildScopes)
            {
                // but not for existing child scopes
                if (!activationObject.Existing)
                {
                    activationObject.AnalyzeScope();
                }
            }
        }

        internal override void AutoRenameFields()
        {
            // don't crunch global values -- they might be referenced in other scripts
            // within the page but outside this module.

            // traverse through our children scopes
            foreach (ActivationObject scope in ChildScopes)
            {
                // don't recurse existing child scopes
                if (!scope.Existing)
                {
                    scope.AutoRenameFields();
                }
            }
        }

        public override JSVariableField this[string name]
        {
            get
            {
                // check the name table
                JSVariableField variableField = base[name];

                // not found so far, check the global properties
                if (variableField == null)
                {
                    variableField = ResolveFromCollection(name, m_globalProperties, FieldType.Predefined, false);
                }

                // not found so far, check the global properties
                if (variableField == null)
                {
                    variableField = ResolveFromCollection(name, m_globalFunctions, FieldType.Predefined, true);
                }

                // if not found so far, check to see if this value is provided in our "assumed" 
                // global list specified on the command line
                if (variableField == null)
                {
                    variableField = ResolveFromCollection(name, m_assumedGlobals, FieldType.Global, false);
                }

                return variableField;
            }
        }

        private JSVariableField ResolveFromCollection(string name, HashSet<string> collection, FieldType fieldType, bool isFunction)
        {
            if (collection.Contains(name))
            {
                var variableField = new JSVariableField(fieldType, name, 0, null);
                variableField.IsFunction = isFunction;
                return AddField(variableField);
            }

            return null;
        }

        public override JSVariableField CreateField(string name, object value, FieldAttributes attributes)
        {
            return new JSVariableField(FieldType.Global, name, attributes, value);
        }

        public override JSVariableField CreateField(JSVariableField outerField)
        {
            // should NEVER try to create an inner field in a global scope
            throw new NotImplementedException();
        }
    }
}
