// JavaScriptSymbol.cs
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

using System.Globalization;
using System.Xml;

namespace Microsoft.Ajax.Utilities
{
    public class JavaScriptSymbol
    {
        private const string SymbolDataFormat = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}";
        private int m_startLine;
        private int m_endLine;
        private int m_startColumn;
        private int m_endColumn;
        private Context m_sourceContext;
        private int m_sourceFileId;
        private string m_symbolType;
        private string m_parentFunction;

        private JavaScriptSymbol()
        {
        }

        public static JavaScriptSymbol StartNew(AstNode node, int startLine, int startColumn, int sourceFileId)
        {
            return new JavaScriptSymbol
            {
                m_startLine = startLine,
                m_startColumn = startColumn,
                m_sourceContext = node != null ? node.Context : null,
                m_symbolType = node != null ? node.GetType().Name : "[UNKNOWN]",
                m_sourceFileId = sourceFileId,
            };
        }

        public void End(int endLine, int endColumn, string parentFunction)
        {
            m_endLine = endLine;
            m_endColumn = endColumn;
            m_parentFunction = parentFunction;
        }

        public static void WriteHeadersTo(XmlWriter writer)
        {
            if (writer != null)
            {
                writer.WriteStartElement("headers");
                writer.WriteString(SymbolDataFormat.FormatInvariant(
                    "DstStartLine",
                    "DstStartColumn",
                    "DstEndLine",
                    "DstEndColumn",
                    "SrcStartPosition",
                    "SrcEndPosition",
                    "SrcStartLine",
                    "SrcStartColumn",
                    "SrcEndLine",
                    "SrcEndColumn",
                    "SrcFileId",
                    "SymbolType",
                    "ParentFunction"));

                writer.WriteEndElement(); //headers
            }
        }

        public void WriteTo(XmlWriter writer)
        {
            if (writer != null)
            {
                writer.WriteStartElement("s");
                writer.WriteString(SymbolDataFormat.FormatInvariant(
                    m_startLine,
                    m_startColumn,
                    m_endLine,
                    m_endColumn,
                    m_sourceContext.StartPosition,
                    m_sourceContext.EndPosition,
                    m_sourceContext.StartLineNumber,
                    m_sourceContext.StartColumn,
                    m_sourceContext.EndLineNumber,
                    m_sourceContext.EndColumn,
                    m_sourceFileId,
                    m_symbolType,
                    m_parentFunction));

                writer.WriteEndElement(); //s
            }
        }
    }
}
