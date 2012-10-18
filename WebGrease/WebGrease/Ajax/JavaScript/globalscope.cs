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
        private GlobalObject m_globalObject;
        private GlobalObject m_windowObject;
        private HashSet<string> m_assumedGlobals;
        private HashSet<UndefinedReferenceException> m_undefined;

        public ICollection<UndefinedReferenceException> UndefinedReferences { get { return m_undefined; } }

        internal GlobalScope(JSParser parser)
            : base(null, parser)
        {
            // define the Global object's properties, and methods
            m_globalObject = new GlobalObject(
              new string[] { "Infinity", "NaN", "undefined", "window", "Image", "JSON", "Math", "XMLHttpRequest", "DOMParser" },
              new string[] { "decodeURI", "decodeURIComponent", "encodeURI", "encodeURIComponent", "escape", "eval", "importScripts", "isNaN", "isFinite", "parseFloat", "parseInt", "unescape", "ActiveXObject", "Array", "Boolean", "Date", "Error", "EvalError", "EventSource", "File", "FileList", "FileReader", "Function", "GeckoActiveXObject", "HTMLElement", "Number", "Object", "Proxy", "RangeError", "ReferenceError", "RegExp", "SharedWorker", "String", "SyntaxError", "TypeError", "URIError", "WebSocket", "Worker" }
              );

            // define the Window object's properties, and methods
            m_windowObject = new GlobalObject(
              new string[] { "applicationCache", "clientInformation", "clipboardData", "closed", "console", "document", "event", "external", "frameElement", "frames", "history", "length", "localStorage", "location", "name", "navigator", "opener", "parent", "screen", "self", "sessionStorage", "status", "top" },
              new string[] { "addEventListener", "alert", "attachEvent", "blur", "clearInterval", "clearTimeout", "close", "confirm", "createPopup", "detachEvent", "dispatchEvent", "execScript", "focus", "getComputedStyle", "getSelection", "moveBy", "moveTo", "navigate", "open", "postMessage", "prompt", "removeEventListener", "resizeBy", "resizeTo", "scroll", "scrollBy", "scrollTo", "setActive", "setInterval", "setTimeout", "showModalDialog", "showModelessDialog" }
              );
        }

        public void AddUndefinedReference(UndefinedReferenceException exception)
        {
            if (m_undefined == null)
            {
                m_undefined = new HashSet<UndefinedReferenceException>();
            }

            m_undefined.Add(exception);
        }

        internal void SetAssumedGlobals(IEnumerable<string> globals, IEnumerable<string> debugLookups)
        {
            // start off with any known globals
            m_assumedGlobals = globals == null ? new HashSet<string>() : new HashSet<string>(globals);

            // chek to see if there are any debug lookups
            if (debugLookups != null)
            {
                foreach (var debugLookup in debugLookups)
                {
                    m_assumedGlobals.Add(debugLookup.SubstringUpToFirst('.'));
                }
            }
        }

        internal override void AnalyzeScope()
        {
            // rename fields if we need to
            RenameFields();

            // it's okay for the global scope to have unused vars, so don't bother checking
            // the fields, but recurse the function scopes anyway
            foreach (ActivationObject activationObject in ChildScopes)
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

        internal override void ReserveFields()
        {
            // don't do anything but traverse through our children
            foreach (ActivationObject scope in ChildScopes)
            {
                scope.ReserveFields();
            }
        }

        internal override void HyperCrunch()
        {
            // don't crunch global values -- they might be referenced in other scripts
            // within the page but outside this module.

            // traverse through our children scopes
            foreach (ActivationObject scope in ChildScopes)
            {
                scope.HyperCrunch();
            }
        }

        public override JSVariableField this[string name]
        {
            get
            {
                // check the name table
                JSVariableField variableField = base[name];
                if (variableField == null)
                {
                    // not found so far, check the window object
                    variableField = m_windowObject.GetField(name);
                }
                if (variableField == null)
                {
                    // not found so far, check the global object
                    variableField = m_globalObject.GetField(name);
                }
                if (variableField == null)
                {
                    // see if this value is provided in our "assumed" global list specified on the command line
                    if (m_assumedGlobals.Count > 0)
                    {
                        foreach (string globalName in m_assumedGlobals)
                        {
                            if (string.Compare(name, globalName.Trim(), StringComparison.Ordinal) == 0)
                            {
                                variableField = CreateField(name, null, 0);
                                break;
                            }
                        }
                    }
                }
                return variableField;
            }
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

        public override JSVariableField GetLocalField(String name)
        {
            // there are no local fields in the global scope
            return null;
        }
    }
}
