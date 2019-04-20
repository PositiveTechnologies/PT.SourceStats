using System;
using System.Threading.Tasks;

namespace PT.SourceStats.Cli
{
    public class StatSender
    {
        public async Task SendStat(string stat, string server)
        {
            var client = new HttpsClientWithCert();
            await client.SendData(new Uri(server), stat);
        }
    }
}
