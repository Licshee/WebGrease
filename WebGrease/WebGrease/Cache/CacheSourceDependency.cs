namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    public class CacheSourceDependency
    {
        public static CacheSourceDependency Create(IWebGreaseContext context, InputSpec inputSpec)
        {
            var csd = new CacheSourceDependency();
            if (Directory.Exists(inputSpec.Path))
            {
                inputSpec.Path.EnsureEndSeperatorChar();
            }

            csd.InputSpecHash = GetInputSpecHash(context, inputSpec);
            inputSpec.Path = inputSpec.Path.MakeRelativeToDirectory(context.Configuration.SourceDirectory);
            csd.InputSpec = inputSpec;

            return csd;
        }

        private static string GetInputSpecHash(IWebGreaseContext context, InputSpec inputSpec)
        {
            return inputSpec
                .GetFiles(context.Configuration.SourceDirectory)
                .ToDictionary(f => f.MakeRelativeToDirectory(context.Configuration.SourceDirectory), context.GetFileHash)
                .ToJson();
        }

        public InputSpec InputSpec { get; private set; }
        public string InputSpecHash { get; private set; }

        public bool HasChanged(IWebGreaseContext context)
        {
            return !InputSpecHash.Equals(GetInputSpecHash(context, InputSpec), StringComparison.Ordinal);
        }
    }
}