namespace WebGrease
{
    using System.IO;

    using WebGrease.Configuration;

    public class NullCacheSection : ICacheSection
    {
        public void VaryByFile(string absoluteFilePath)
        {
        }

        public void VaryBySettings(object settings, bool nonpublic = false)
        {
        }

        public void EndSection()
        {
        }

        public bool IsValid()
        {
            return false;
        }

        public void AddResultFile(string filePath, string fileCategory, string relativePath = null, string id = null)
        {
        }

        public void RestoreFiles(string category, string targetPath = null, bool overwrite = false)
        {
        }

        public void RestoreFile(string category, string absolutePath, bool overwrite = false)
        {
        }

        public void AddSourceDependency(string file)
        {
            
        }

        public void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
        }

        public void AddSourceDependency(InputSpec inputSpec)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Interface for Null object")]
        public ICacheSection Parent { get; private set; }

        public bool SourceDependenciesHaveChanged()
        {
            return true;
        }
    }
}