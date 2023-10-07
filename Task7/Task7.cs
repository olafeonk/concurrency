using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPL
{
    public class AsyncScanner : IPScanner
    {
        public async Task Scan(IPAddress[] ipAddrs, int[] ports)
        {
            foreach (var ipAddr in ipAddrs)
            {
                var pingReply = await PingAddr(ipAddr);
                if (pingReply.Status != IPStatus.Success)
                    continue;

                foreach (var port in ports)
                    await CheckPort(pingReply.Address, port);
            }
        }

        private async Task<PingReply> PingAddr(IPAddress ipAddr, int timeout = 3000)
        {
            using var ping = new Ping();

            Console.WriteLine($"Pinging {ipAddr}");
            var statusTask = await ping.SendPingAsync(ipAddr, timeout);
            Console.WriteLine($"Pinged {ipAddr}: {statusTask}");

            return statusTask;
        }

        private static async Task CheckPort(IPAddress ipAddr, int port, int timeout = 3000)
        {
            using var tcpClient = new TcpClient();

            Console.WriteLine($"Checking {ipAddr}:{port}");
            var portStatus = await tcpClient.ConnectAsync(ipAddr, port, timeout);
            Console.WriteLine($"Checked {ipAddr}:{port} - {portStatus}");
        }
    }
}
