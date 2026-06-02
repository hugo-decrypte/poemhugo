using System;
using System.Collections.Generic;

namespace PoemClientWPF.Tools
{
    class CommandLineArgs {
        public readonly Dictionary<string, string> argv = new Dictionary<string, string>();

        public CommandLineArgs() {
            string[] arguments = Environment.GetCommandLineArgs();

            if (arguments.Length < 3)
            {
                return;
            }
            try
            {
                for (int i = 1; i < arguments.Length; i += 2)
                {
                    argv.Add(arguments[i], arguments[i + 1]);
                }
            }
            catch (Exception exception)
            {
                _ = exception;
            }
        }

        public string GetArgValue(string key)
        {
            if (this.argv.ContainsKey(key) == true)
            {
                return this.argv[key];
            }
            return null;
        }

        public void PopArgValue(string key)
        {
            try
            {
                this.argv.Remove(key);
            }
            catch (Exception exception)
            {
                _ = exception;
            }
        }
    }
}
