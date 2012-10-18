// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HashUtility.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   HashUtility class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Common
{
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>HashUtility class.</summary>
    internal static class HashUtility
    {
        /// <summary>
        /// The hasher for creating the checksum.
        /// </summary>
        private static readonly MD5 hasher = MD5.Create();

        /// <summary>Gets hash string in 'X2' format for input file</summary>
        /// <param name="file">Input file to generate a hash string for</param>
        /// <returns>Returns hash string in 'X2' format for input file</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "By design")]
        internal static string GetHashStringForFile(string file)
        {
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                var hash = hasher.ComputeHash(fileStream);

                var hexString = new StringBuilder(hash.Length);
                for (var i = 0; i < hash.Length; i++)
                {
                    hexString.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }

                return hexString.ToString().ToLower(CultureInfo.InvariantCulture);
            }
        }
    }
}
