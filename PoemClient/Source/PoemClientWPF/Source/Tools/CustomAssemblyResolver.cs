using System;
using System.IO;
using System.Reflection;

namespace PoemClient.Tools
{
    class CustomAssemblyResolver : MarshalByRefObject
    {
        public Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries");
            string assemblyPath = Path.Combine(directoryPath, new AssemblyName(args.Name).Name + ".dll");

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            return null;
        }
    }
}
