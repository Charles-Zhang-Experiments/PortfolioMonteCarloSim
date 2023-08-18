using System.IO;
using System.Linq;
using System.Reflection;

namespace RiskTerminal
{
    internal static class EmbeddedResourceHelper
    {
        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(RiskTerminal)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }
}
