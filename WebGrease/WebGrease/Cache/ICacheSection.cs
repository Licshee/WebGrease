namespace WebGrease
{
    using System.IO;

    using WebGrease.Configuration;

    public interface ICacheSection
    {
        void VaryByFile(string absoluteFilePath);

        void VaryBySettings(object settings, bool nonpublic = false);

        void EndSection();

        bool IsValid();

        void AddResultFile(string filePath, string fileCategory, string relativePath = null, string id = null);

        void RestoreFiles(string category, string targetPath = null, bool overwrite = false);

        void RestoreFile(string category, string absolutePath, bool overwrite = false);


        void AddSourceDependency(string file);

        void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        void AddSourceDependency(InputSpec inputSpec);

        ICacheSection Parent { get; }

        bool SourceDependenciesHaveChanged();
    }
}