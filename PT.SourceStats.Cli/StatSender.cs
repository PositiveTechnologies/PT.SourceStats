using System;
using System.Threading.Tasks;

namespace PT.SourceStats.Cli
{
    public class StatSender
    {
        public async Task SendStat(string stat, string server)
        {
            var clinet = new HttpsClientWithCert();
            var result = await clinet.SendData(new Uri(server), stat);
        }
    }
}
