using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TagTool.Commands;

namespace TagTool.Scripting.CSharp
{
    public class AssemblyResolver
    {
        public AssemblyName FindAssembly(DirectoryInfo sourceDirectory, string assemblyNameOrPath)
        {
            if (assemblyNameOrPath.Contains(".dll"))
            {
                string fullPath = assemblyNameOrPath.StartsWith("./")
                    ? Path.Combine(sourceDirectory.FullName, assemblyNameOrPath)
                    : Path.Combine(DirectoryPaths.Base, assemblyNameOrPath);

                if (!File.Exists(fullPath))
                    return null;

                return AssemblyName.GetAssemblyName(fullPath);
            }

            var assemblyName = new AssemblyName(assemblyNameOrPath);
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName())
                .Where(an => IsAssemblyNameCompatible(assemblyName, an))
                .OrderByDescending(a => a.Version)
                .FirstOrDefault();

            return loadedAssembly ?? FindAssemblyInGAC(assemblyName);
        }

        private AssemblyName FindAssemblyInGAC(AssemblyName assemblyName)
        {
            var searchPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "assembly", Environment.Is64BitProcess ? "GAC_64" : "GAC_32"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "assembly", "GAC_MSIL")
            };

            foreach (var path in searchPaths)
            {
                var found = SearchAssemblyCache(new DirectoryInfo(path), assemblyName)
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

                if (found != null)
                    return found;
            }

            return null;
        }

        private IEnumerable<AssemblyName> SearchAssemblyCache(DirectoryInfo cacheDirectory, AssemblyName assemblyName)
        {
            var assemblyDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, assemblyName.Name));
            if (!assemblyDirectory.Exists)
                yield break;

            foreach (var directory in assemblyDirectory.GetDirectories())
            {
                foreach (var file in directory.GetFiles("*.dll", SearchOption.AllDirectories))
                {
                    var an = AssemblyName.GetAssemblyName(file.FullName);
                    if (IsAssemblyNameCompatible(assemblyName, an))
                        yield return an;
                }
            }
        }

        private bool IsAssemblyNameCompatible(AssemblyName target, AssemblyName source)
        {
            if (target.Name != source.Name)
                return false;

            if (target.Version != null && source.Version != target.Version)
                return false;

            if (target.CultureInfo != null && !Equals(target.CultureInfo, CultureInfo.InvariantCulture) && target.CultureName != source.CultureName)
                return false;

            if (!target.GetPublicKeyToken()?.SequenceEqual(source.GetPublicKeyToken()) ?? false)
                return false;

            return true;
        }
    }
}
