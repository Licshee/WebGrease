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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "backwards-compatibility for public class name")]
    public sealed class JScriptException
    {
        #region private fields

        private Context m_context;

        #endregion

        #region public properties

        public string FileContext { get; private set; }

        public bool CanRecover { get; set; }

        public bool IsError { get; set; }

        public string Value { get; set; }

        public JSError ErrorCode { get; private set; }

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

        public string Message
        {
            get
            {
                string code = ErrorCode.ToString();
                if (Value != null)
                {
                    return (ErrorCode == JSError.DuplicateName)
                        ? JScript.ResourceManager.GetString(code, JScript.Culture).FormatInvariant(Value)
                        : Value;
                }

                // special case some errors with contextual information
                return JScript.ResourceManager.GetString(code, JScript.Culture).FormatInvariant(
                    m_context.IfNotNull(c => c.HasCode) ? string.Empty : m_context.Code);
            }
        }

        public int Severity
        {
            get
            {
                return GetSeverity(ErrorCode);
            }
        }

        #endregion

        #region constructors

        internal JScriptException(JSError errorNumber, Context context)
        {
            Value = null;
            m_context = (context == null ? null : context.Clone());
            FileContext = (context == null ? null : context.Document.FileContext);
            ErrorCode = errorNumber;
            CanRecover = true;
        }

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

                case JSError.ArrayLiteralTrailingComma:
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "backwards-compatibility for public class name")]
    public sealed class UndefinedReferenceException
    {
        private Context m_context;

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

        public override string ToString()
        {
            return m_name;
        }
    }

    public class UndefinedReferenceEventArgs : EventArgs
    {
        public UndefinedReferenceException Exception { get; private set; }

        public UndefinedReferenceEventArgs(UndefinedReferenceException exception)
        {
            Exception = exception;
        }
    }
}
