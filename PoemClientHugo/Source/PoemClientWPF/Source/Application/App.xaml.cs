using PoemClient.Tools;
using System;
using System.Windows;

namespace PoemClientWPF
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(new CustomAssemblyResolver().AssemblyResolveHandler);
        }
        
    }
}
