using System.Windows;

namespace PoemClientWPF.Tools
{
    class DebugMode
    {
        private static DebugMode instance = null;
        private readonly bool isDebugMode;

        private DebugMode(bool isDebug)
        {
            this.isDebugMode = isDebug;
        }

        public static DebugMode GetInstance(bool isDebug)
        {
            if (instance == null)
                instance = new DebugMode(isDebug);
            return instance;
        }

        public void DebugMessage(string message)
        {
            if (isDebugMode)
            {
                MessageBox.Show(
                      message,
                      "Debug Message",
                      MessageBoxButton.OK,
                      MessageBoxImage.Information
                  );
            }
        }
    }
}
