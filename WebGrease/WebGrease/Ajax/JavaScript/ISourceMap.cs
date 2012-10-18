// ISourceMap.cs
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
    public interface ISourceMap : IDisposable
    {
        void StartPackage(string sourcePath);
        void EndPackage();
        object StartSymbol(AstNode astNode, int startLine, int startColumn);
        void EndSymbol(object symbol, int endLine, int endColumn, string parentContext);
        string Name { get; }
    }
}
