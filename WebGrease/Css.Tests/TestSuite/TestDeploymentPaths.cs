// -----------------------------------------------------------------------
// <copyright file="TestDeploymentPaths.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Encapsulates the well known paths
// </summary>
// -----------------------------------------------------------------------

namespace Css.Tests.TestSuite
{
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Encapsulates the well known paths
    /// </summary>
    public static class TestDeploymentPaths
    {
        /// <summary>Initializes static members of the <see cref="TestDeploymentPaths"/> class.</summary>
        static TestDeploymentPaths()
        {
            TestDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        /// <summary>Gets the mock assets root.</summary>
        public static string TestDirectory { get; private set; }
    }
}
