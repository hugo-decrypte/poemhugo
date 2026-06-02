using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools
{
    internal static class Utils
    {
        public static bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    return (reply.Status == IPStatus.Success);
                }
            }
            catch (PingException)
            {
                return false;
            }
        }
    }
}
