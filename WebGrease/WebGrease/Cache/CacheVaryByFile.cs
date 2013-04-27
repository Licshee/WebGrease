namespace WebGrease
{
    using System.Xml.Linq;

    public class CacheVaryByFile
    {
        public string OriginalAbsoluteFilePath { get; private set; }

        public string OriginalRelativeFilePath { get; private set; }

        public string Hash { get; private set; }

        private CacheVaryByFile() { }

        public static CacheVaryByFile FromFile(IWebGreaseContext context, string absoluteFilePath)
        {
            return new CacheVaryByFile
                       {
                           OriginalAbsoluteFilePath = absoluteFilePath,
                           OriginalRelativeFilePath = context.MakeRelative(absoluteFilePath),
                           Hash = context.GetFileHash(absoluteFilePath),
                       };
        }

        public static CacheVaryByFile FromXml(XElement element)
        {
            return new CacheVaryByFile
                       {
                           OriginalAbsoluteFilePath = (string)element.Attribute("OriginalAbsoluteFilePath"),
                           OriginalRelativeFilePath = (string)element.Attribute("OriginalRelativeFilePath"),
                           Hash = (string)element.Attribute("Hash"),
                       };
        }

        public XElement ToXml()
        {
            return new XElement(
                "File",
                new XAttribute("Hash", this.Hash),
                new XAttribute("OriginalAbsoluteFilePath", this.OriginalAbsoluteFilePath),
                new XAttribute("OriginalRelativeFilePath", this.OriginalRelativeFilePath));
        }
    }
}