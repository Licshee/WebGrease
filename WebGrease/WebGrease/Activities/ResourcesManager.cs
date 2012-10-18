// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcesManager.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   ResourcesManager class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Resources;

    /// <summary>ResourcesManager class.</summary>
    internal static class ResourcesManager
    {
        /// <summary>
        /// The padding key to scan in the resources file
        /// </summary>
        private const string IgnoreImagesKey = "ImageAssembly.IgnoreImages";

        /// <summary>
        /// The padding key to scan in the resources file
        /// </summary>
        private const string PaddingKey = "ImageAssembly.VerticalSprite.Padding";

        /// <summary>
        /// Images to assemble into the lazy loaded sprite
        /// </summary>
        private const string LazyLoadImagesKey = "ImageAssembly.LazyLoadImages";

        /// <summary>Gets the tokens based on locale or theme key. 
        /// The token files are searched in input paths
        /// and resolved for precedence and a dictionary is returned.</summary>
        /// <param name="resourcesDirectoryPath">Resources folder path.</param>
        /// <param name="localeOrThemeName">Theme or locale key.</param>
        /// <param name="resources">The resources dictionary</param>
        /// <returns>Whether list loaded successfully</returns>
        public static bool TryGetResources(string resourcesDirectoryPath, string localeOrThemeName, out Dictionary<string, string> resources)
        {
            resources = new Dictionary<string, string>();
            string resxFilePath;

            if (HasResources(resourcesDirectoryPath, localeOrThemeName, out resxFilePath))
            {
                using (var resXResourceReader = new ResXResourceReader(resxFilePath))
                {
                    foreach (DictionaryEntry resource in resXResourceReader)
                    {
                        var key = resource.Key as string;
                        var value = resource.Value as string;
                        if (key != null)
                        {
                            resources.Add(key, value);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the lazy load image list from the resources dictionary
        /// </summary>
        /// <param name="localeResources">The dictionary of resources</param>
        /// <param name="hashedImagesLogs">The hashed log file of images</param>
        /// <param name="imageReferencesToLazyLoad">The image references to ignore</param>
        /// <returns>Whether list loaded successfully</returns>
        public static bool TryGetLazyLoadReferencesFromResources(IDictionary<string, string> localeResources, RenamedFilesLogs hashedImagesLogs, out HashSet<string> imageReferencesToLazyLoad)
        {
            string lazyLoadImagesValue;

            if (localeResources != null && localeResources.TryGetValue(LazyLoadImagesKey, out lazyLoadImagesValue))
            {
                if (!string.IsNullOrWhiteSpace(lazyLoadImagesValue))
                {
                    var lazyLoadImagesList = lazyLoadImagesValue.Split(Strings.Semicolon.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    if (lazyLoadImagesList.Count() > 0)
                    {
                        imageReferencesToLazyLoad = new HashSet<string>();

                        var hashedLazyLoadImageSet = new Dictionary<string, List<string>>();

                        // Convert to the hash paths
                        foreach (var lazyLoadImage in lazyLoadImagesList)
                        {
                            // Look up for the hash path in the log file
                            var hashedLazyLoadImage = hashedImagesLogs.FindHashPath(lazyLoadImage);
                            if (!string.IsNullOrWhiteSpace(hashedLazyLoadImage))
                            {
                                if (!hashedLazyLoadImageSet.ContainsKey(hashedLazyLoadImage))
                                {
                                    hashedLazyLoadImageSet.Add(hashedLazyLoadImage, new List<string>());
                                }

                                hashedLazyLoadImageSet[hashedLazyLoadImage].Add(lazyLoadImage);
                            }
                        }

                        foreach (var entry in hashedLazyLoadImageSet)
                        {
                            // Verify if file *and* all dups are in the list
                            // Unfortunately, hashedImagesLogs doesn't have position information.
                            // If it did, we could verify that all dup that have also have a matching position
                            // are in the list which is more optimal.
                            if (hashedImagesLogs.AllInputFileNamesMatch(entry.Key, entry.Value))
                            {
                                // Add to lazy load list hash set
                                imageReferencesToLazyLoad.Add(entry.Key);
                            }
                        }

                        return true;
                    }
                }
            }

            imageReferencesToLazyLoad = null;
            return false;
        }

        /// <summary>Checks if the resx file is present for a theme or a locale</summary>
        /// <param name="resourcesDirectoryPath">Resources folder path.</param>
        /// <param name="localeOrThemeName">Theme or locale key.</param>
        /// <param name="resourcePath">The path of the resx file</param>
        /// <returns>True if the resx file is found</returns>
        public static bool HasResources(string resourcesDirectoryPath, string localeOrThemeName, out string resourcePath)
        {
            resourcePath = Path.Combine(resourcesDirectoryPath, localeOrThemeName + Strings.ResxExtension);
            return File.Exists(resourcePath);
        }

        /// <summary>Gets the ignore image list from the resources dictionary</summary>
        /// <param name="localeResources">The dictionary of resources</param>
        /// <param name="hashedImagesLogs">The hashed log file of images</param>
        /// <param name="imageReferencesToIgnore">The image references to ignore</param>
        /// <returns>The list of images to ignore</returns>
        public static bool TryGetIgnoreReferencesFromResources(IDictionary<string, string> localeResources, RenamedFilesLogs hashedImagesLogs, out HashSet<string> imageReferencesToIgnore)
        {
            string ignoreImagesValue;

            if (localeResources != null && localeResources.TryGetValue(IgnoreImagesKey, out ignoreImagesValue))
            {
                if (!string.IsNullOrWhiteSpace(ignoreImagesValue))
                {
                    var ignoreImagesList = ignoreImagesValue.Split(Strings.Semicolon.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    if (ignoreImagesList.Count() > 0)
                    {
                        imageReferencesToIgnore = new HashSet<string>();

                        // Convert to the hash paths
                        foreach (var ignoreImage in ignoreImagesList)
                        {
                            // Look up for the hash path in the log file
                            var hashedIgnoreImage = hashedImagesLogs.FindHashPath(ignoreImage);

                            // Verify if there is a match found
                            if (!string.IsNullOrWhiteSpace(hashedIgnoreImage))
                            {
                                // Add to ignore list hash set if found
                                imageReferencesToIgnore.Add(hashedIgnoreImage);
                            }
                        }

                        return true;
                    }
                }
            }

            imageReferencesToIgnore = null;
            return false;
        }

        /// <summary>
        /// Gets the padding from the resources dictionary
        /// </summary>
        /// <param name="localeResources">The dictionary of resources</param>
        /// <param name="padding">The padding specified in the resources</param>
        /// <returns>The padding</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification="css is in lowercase.")]
        public static bool TryGetPaddingFromResources(IDictionary<string, string> localeResources, out int padding)
        {
            padding = 0;
            string paddingValue;

            if (localeResources != null && localeResources.TryGetValue(PaddingKey, out paddingValue))
            {
                if (!string.IsNullOrWhiteSpace(paddingValue))
                {
                    // Remove the "px" units if specificed
                    paddingValue.ToLower(CultureInfo.InvariantCulture).TrimEnd(Strings.Px.ToCharArray());

                    int result;
                    if (int.TryParse(paddingValue, out result))
                    {
                        padding = result;
                        return true;
                    }

                    throw new BuildWorkflowException(string.Format(CultureInfo.CurrentUICulture, "Invalid padding value '{0}' found  in the Css resources.", paddingValue));
                }
            }

            return false;
        }
    }
}