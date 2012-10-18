// CssSettings.cs
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

namespace Microsoft.Ajax.Utilities
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Enumeration for the type of CSS that will be parsed
    /// </summary>
    public enum CssType
    {
        /// <summary>
        /// Default setting: expecting a full CSS stylesheet
        /// </summary>
        FullStyleSheet = 0,

        /// <summary>
        /// Expecting just a declaration list, for instance: the value of an HTML style attribute
        /// </summary>
        DeclarationList,
    }

    /// <summary>
    /// Settings Object for CSS Minifier
    /// </summary>
    public class CssSettings : CommonSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CssSettings"/> class with default settings.
        /// </summary>
        public CssSettings()
        {
            ColorNames = CssColor.Strict;
            CommentMode = CssComment.Important;
            MinifyExpressions = true;
            CssType = CssType.FullStyleSheet;
        }

        public CssSettings Clone()
        {
            // create the new settings object and copy all the properties from
            // the current settings
            var newSettings = new CssSettings()
            {
                AllowEmbeddedAspNetBlocks = this.AllowEmbeddedAspNetBlocks,
                ColorNames = this.ColorNames,
                CommentMode = this.CommentMode,
                IgnoreAllErrors = this.IgnoreAllErrors,
                IgnoreErrorList = this.IgnoreErrorList,
                IndentSize = this.IndentSize,
                KillSwitch = this.KillSwitch,
                LineBreakThreshold = this.LineBreakThreshold,
                MinifyExpressions = this.MinifyExpressions,
                OutputMode = this.OutputMode,
                PreprocessorDefineList = this.PreprocessorDefineList,
                TermSemicolons = this.TermSemicolons,
                CssType = this.CssType,
                BlocksStartOnSameLine = this.BlocksStartOnSameLine,
            };

            // add the resource strings (if any)
            newSettings.AddResourceStrings(this.ResourceStrings);

            return newSettings;
        }

        /// <summary>
        /// Gets or sets ColorNames setting. Default is Strict.
        /// </summary>
        public CssColor ColorNames
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets CommentMode setting. Default is Important.
        /// </summary>
        public CssComment CommentMode
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to minify the javascript within expression functions. Deault is true.
        /// </summary>
        public bool MinifyExpressions
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating how to treat the input source. Default is FullStyleSheet.
        /// </summary>
        public CssType CssType 
        { 
            get; 
            set; 
        }
    }
}
