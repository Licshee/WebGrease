// ScriptSharpSourceMap.cs
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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace Microsoft.Ajax.Utilities
{
    public sealed class ScriptSharpSourceMap : ISourceMap
    {
        private readonly XmlWriter m_writer;
        private string m_currentPackage;
        private Dictionary<string, int> m_sourceFileIndexMap = new Dictionary<string, int>();
        private int currentIndex;

        public string Name
        {
            get { return "ScriptSharp"; }
        }

        public ScriptSharpSourceMap(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            m_writer = writer;
            m_writer.WriteStartDocument();
            m_writer.WriteStartElement("map");
            JavaScriptSymbol.WriteHeadersTo(m_writer);
            m_writer.WriteStartElement("scriptFiles");
        }

        public void StartPackage(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("path cannot be null or empty", "sourcePath");
            }

            m_currentPackage = sourcePath;
            m_writer.WriteStartElement("scriptFile");
            m_writer.WriteAttributeString("path", sourcePath);
        }

        public void EndPackage()
        {
            if (m_currentPackage == null)
            {
                return;
            }

            // Compute and print the output script checksum and close the scriptFile element
            // the checksum can be used to determine whether the symbols map file is still valid
            // or if the script has been tempered with
            using (FileStream stream = new FileStream(m_currentPackage, FileMode.Open))
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] checksum = md5.ComputeHash(stream);

                    m_writer.WriteStartElement("checksum");
                    m_writer.WriteAttributeString("value", BitConverter.ToString(checksum));
                    m_writer.WriteEndElement(); //checksum
                    m_writer.WriteEndElement(); //scriptFile
                }
            }

            m_currentPackage = null;
        }

        public object StartSymbol(AstNode astNode, int startLine, int startColumn)
        {
            if (astNode != null 
                && !astNode.Context.Document.IsGenerated)
            {
                return JavaScriptSymbol.StartNew(astNode, startLine, startColumn, GetSourceFileIndex(astNode.Context.Document.FileContext));
            }

            return null;
        }

        public void EndSymbol(object symbol, int endLine, int endColumn, string parentContext)
        {
            if (symbol == null)
            {
                return;
            }

            var javaScriptSymbol = (JavaScriptSymbol)symbol;
            javaScriptSymbol.End(endLine, endColumn, parentContext);
            javaScriptSymbol.WriteTo(m_writer);
        }

        public void Dispose()
        {
            EndPackage();

            m_writer.WriteEndElement(); //scriptFiles
            m_writer.WriteStartElement("sourceFiles");

            foreach (KeyValuePair<string, int> kvp in m_sourceFileIndexMap)
            {
                m_writer.WriteStartElement("sourceFile");
                m_writer.WriteAttributeString("id", kvp.Value.ToStringInvariant());
                m_writer.WriteAttributeString("path", kvp.Key);
                m_writer.WriteEndElement(); //file
            }

            m_writer.WriteEndElement(); //sourceFiles
            m_writer.WriteEndElement(); //map
            m_writer.WriteEndDocument();
            m_writer.Close();
        }

        private int GetSourceFileIndex(string fileName)
        {
            int index;
            if (!m_sourceFileIndexMap.TryGetValue(fileName, out index))
            {
                index = ++currentIndex;
                m_sourceFileIndexMap.Add(fileName, index);
            }

            return index;
        }
    }
}
