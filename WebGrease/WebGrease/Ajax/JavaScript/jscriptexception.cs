// jscriptexception.cs
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
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Ajax.Utilities
{

    //-------------------------------------------------------------------------------------------------------
    // JScriptException
    //
    //  An error in JScript goes to a COM+ host/program in the form of a JScriptException. However a 
    //  JScriptException is not always thrown. In fact a JScriptException is also a IVsaError and thus it can be
    //  passed to the host through IVsaSite.OnCompilerError(IVsaError error).
    //  When a JScriptException is not a wrapper for some other object (usually either a COM+ exception or 
    //  any value thrown in a JScript throw statement) it takes a JSError value.
    //  The JSError enum is defined in JSError.cs. When introducing a new type of error perform
    //  the following four steps:
    //  1- Add the error in the JSError enum (JSError.cs)
    //  2- Update JScript.resx with the US English error message
    //  3- Update Severity.
    //-------------------------------------------------------------------------------------------------------
#if !SILVERLIGHT
    [Serializable]
#endif
    public class JScriptException : Exception
    {
        #region private fields

#if !SILVERLIGHT
        [NonSerialized]
#endif
        private Context m_context;

        private string m_valueObject;
        private bool m_isError;
        private JSError m_errorCode;
        private bool m_canRecover = true;
        private string m_fileContext;

        #endregion

        #region public properties

        public string FileContext
        {
            get
            {
                return m_fileContext;
            }
        }

        public bool CanRecover
        {
            get { return m_canRecover; }
            set { m_canRecover = value; }
        }

        public bool IsError
        {
            get { return m_isError; }
            set { m_isError = value; }
        }

        public string Value
        {
            get { return m_valueObject; }
            set { m_valueObject = value; }
        }

        public int StartColumn
        {
            get
            {
                return Column;
            }
        }

        public int Line
        {
            get
            {
                if (m_context != null)
                {
                    return m_context.StartLineNumber;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int Column
        {
            get
            {
                if (m_context != null)
                {
                    // one-based column number
                    return m_context.StartColumn + 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int EndLine
        {
            get
            {
                if (m_context != null)
                {
                    return m_context.EndLineNumber;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int EndColumn
        {
            get
            {
                if (m_context != null)
                {
                    if (m_context.EndColumn >= m_context.StartColumn)
                    {
                        // normal condition - one-based
                        return m_context.EndColumn + 1;
                    }
                    else
                    {
                        // end column before start column -- just set end to be the end of the line
                        return LineText.Length;
                    }
                }
                else
                    return 0;
            }
        }

        public string FullSource
        {
            get
            {
                return (m_context == null ? string.Empty : m_context.Document.Source);
            }
        }

        public String LineText
        {
            get
            {
                string lineText = string.Empty;
                if (m_context != null)
                {
                    int lineStart = m_context.StartLinePosition;
                    string source = m_context.Document.Source;

                    if (lineStart < source.Length)
                    {
                        int ndxLF = source.IndexOf('\n', lineStart);
                        if (ndxLF < lineStart)
                        {
                            // no line endings for the rest of the source
                            lineText = source.Substring(lineStart);
                        }
                        else if (ndxLF == lineStart || (ndxLF == lineStart + 1 && source[lineStart] == '\r'))
                        {
                            // blank line
                        }
                        else if (source[ndxLF - 1] == '\r')
                        {
                            // up to CRLF
                            lineText = source.Substring(lineStart, ndxLF - lineStart - 1);
                        }
                        else
                        {
                            // up to LF
                            lineText = source.Substring(lineStart, ndxLF - lineStart);
                        }
                    }
                }
                return lineText;
            }
        }

        public string ErrorSegment
        {
            get
            {
                string source = m_context.Document.Source;
                // just pull out the string that's between start position and end position
                if (m_context.StartPosition >= source.Length)
                {
                    return string.Empty;
                }
                else
                {
                    int length = m_context.EndPosition - m_context.StartPosition;
                    if (m_context.StartPosition + length <= source.Length)
                    {
                        return source.Substring(m_context.StartPosition, length).Trim();
                    }
                    else
                    {
                        return source.Substring(m_context.StartPosition).Trim();
                    }
                }
            }
        }

        public override String Message
        {
            get
            {
                string code = m_errorCode.ToString();
                if (m_valueObject != null)
                {
                    return (m_errorCode == JSError.DuplicateName)
                        ? JScript.ResourceManager.GetString(code, JScript.Culture).FormatInvariant(m_valueObject)
                        : m_valueObject;
                }

                // special case some errors with contextual information
                return JScript.ResourceManager.GetString(code, JScript.Culture).FormatInvariant(
                    m_context.IfNotNull(c => c.HasCode) ? string.Empty : m_context.Code);
            }
        }

        public JSError ErrorCode
        {
            get { return m_errorCode; }
        }

        public int Severity
        {
            get
            {
                return GetSeverity(m_errorCode);
            }
        }

        #endregion

        #region constructors

        public JScriptException() : this(JSError.UncaughtException, null) { }
        public JScriptException(string message) : this(message, null) { }
        public JScriptException(string message, Exception innerException)
            : base(message, innerException)
        {
            m_valueObject = message;
            m_context = null;
            m_fileContext = null;
            m_errorCode = JSError.UncaughtException;
            SetHResult();
        }

        internal JScriptException(JSError errorNumber, Context context)
        {
            m_valueObject = null;
            m_context = (context == null ? null : context.Clone());
            m_fileContext = (context == null ? null : context.Document.FileContext);
            m_errorCode = errorNumber;
            SetHResult();
        }

        #if !SILVERLIGHT
        protected JScriptException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentException(JScript.InternalCompilerError);
            }

            m_valueObject = info.GetString("Value");
            m_isError = info.GetBoolean("IsError");
            m_errorCode = (JSError)info.GetInt32("JSError");
            m_canRecover = info.GetBoolean("CanRecover");
            m_fileContext = info.GetString("FileContext");
        }
        #endif

        #endregion

        #region public methods

        #if !SILVERLIGHT
        [SecurityCritical] 
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");
            base.GetObjectData(info, context);
            info.AddValue("Value", m_valueObject);
            info.AddValue("IsError", m_isError);
            info.AddValue("JSError", (int)m_errorCode);
            info.AddValue("CanRecover", m_canRecover);
            info.AddValue("FileContext", m_fileContext);
        }
        #endif

        #endregion

        #region public static methods

        /// <summary>
        /// Return the default severity for a given JSError value
        /// guide: 0 == there will be a run-time error if this code executes
        ///        1 == the programmer probably did not intend to do this
        ///        2 == this can lead to cross-browser of future problems.
        ///        3 == this can lead to performance problems
        ///        4 == this is just not right
        /// </summary>
        /// <param name="errorCode">error code</param>
        /// <returns>severity</returns>
        public static int GetSeverity(JSError errorCode)
        {
            switch (errorCode)
            {
                case JSError.AmbiguousCatchVar:
                case JSError.AmbiguousNamedFunctionExpression:
                case JSError.NumericOverflow:
                case JSError.StrictComparisonIsAlwaysTrueOrFalse:
                    return 1;

                case JSError.DuplicateCatch:
                case JSError.DuplicateConstantDeclaration:
                case JSError.DuplicateLexicalDeclaration:
                case JSError.KeywordUsedAsIdentifier:
                case JSError.MisplacedFunctionDeclaration:
                case JSError.ObjectLiteralKeyword:
                    return 2;

                case JSError.ArgumentNotReferenced:
                case JSError.DuplicateName:
                case JSError.FunctionNotReferenced:
                case JSError.UndeclaredFunction:
                case JSError.UndeclaredVariable:
                case JSError.VariableDefinedNotReferenced:
                    return 3;

                case JSError.StatementBlockExpected:
                case JSError.SuspectAssignment:
                case JSError.SuspectSemicolon:
                case JSError.SuspectEquality:
                case JSError.WithNotRecommended:
                case JSError.ObjectConstructorTakesNoArguments:
                case JSError.NumericMaximum:
                case JSError.NumericMinimum:
                case JSError.OctalLiteralsDeprecated:
                case JSError.FunctionNameMustBeIdentifier:
                case JSError.SemicolonInsertion:
                    return 4;

                default:
                    // all others
                    return 0;
            }
        }

        #endregion

        #region private methods

        private void SetHResult()
        {
            this.HResult = //unchecked((int)(0x800A0000 + (int)m_errorCode));
                -2146828288 + (int)m_errorCode;
        }

        #endregion
    }

    public class JScriptExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// The JavaScript error information being fired
        /// </summary>
        public ContextError Error { get; private set; }

        /// <summary>
        /// JScriptException object. Don't use this; might go away in future versions. Use Error property instead.
        /// </summary>
        public JScriptException Exception { get; private set; }

        public JScriptExceptionEventArgs(JScriptException exception, ContextError error)
        {
            Error = error;
            Exception = exception;
        }
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    public class UndefinedReferenceException : Exception
    {
#if !SILVERLIGHT
        [NonSerialized]
#endif
        private Context m_context;

#if !SILVERLIGHT
        [NonSerialized]
#endif
        private Lookup m_lookup;
        public AstNode LookupNode
        {
            get { return m_lookup; }
        }

        private string m_name;
        private ReferenceType m_type;

        public string Name
        {
            get { return m_name; }
        }

        public ReferenceType ReferenceType
        {
            get { return m_type; }
        }

        public int Column
        {
            get
            {
                if (m_context != null)
                {
                    // one-based
                    return m_context.StartColumn + 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int Line
        {
            get
            {
                if (m_context != null)
                {
                    return m_context.StartLineNumber;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal UndefinedReferenceException(Lookup lookup, Context context)
        {
            m_lookup = lookup;
            m_name = lookup.Name;
            m_type = lookup.RefType;
            m_context = context;
        }

        public UndefinedReferenceException() : base() { }
        public UndefinedReferenceException(string message) : base(message) { }
        public UndefinedReferenceException(string message, Exception innerException) : base(message, innerException) { }

#if !SILVERLIGHT
        protected UndefinedReferenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            m_name = info.GetString("name");
            m_type = (ReferenceType)Enum.Parse(typeof(ReferenceType), info.GetString("type"));
        }

        [SecurityCritical] 
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("name", m_name);
            info.AddValue("type", m_type.ToString());
        }
#endif

        public override string ToString()
        {
            return m_name;
        }
    }

    public class UndefinedReferenceEventArgs : EventArgs
    {
        private UndefinedReferenceException m_exception;
        public UndefinedReferenceException Exception { get { return m_exception; } }

        public UndefinedReferenceEventArgs(UndefinedReferenceException exception)
        {
            m_exception = exception;
        }
    }
}
