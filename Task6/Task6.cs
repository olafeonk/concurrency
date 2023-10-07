using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPL
{
    public class AsyncScan : IPScanner
    {
        public async Task Scan(IPAddress[] ipAddrs, int[] ports)
        {
            var pings = ipAddrs.Select(
                async ipAddr => await PingAddr(ipAddr, ports)
            );

            await Task.WhenAll(pings);
        }

        private async Task PingAddr(IPAddress ipAddr, int[] ports)
        {
            var pingAddr = await AsyncPingAddr(ipAddr);
            if (pingAddr.Status != IPStatus.Success)
                return;

            await Task.WhenAll(
                ports.Select(port => AsyncCheck(ipAddr, port)
                ));
        }

        private static async Task<PingReply> AsyncPingAddr(IPAddress ipAddr, int timeout = 3000)
        {
            var ping = new Ping();

            Console.WriteLine($"Pinging {ipAddr}");
            var asyncPing = await ping.SendPingAsync(ipAddr, timeout);
            Console.WriteLine($"Pinged {ipAddr}: {asyncPing.Status}");

            return asyncPing;
        }

        private static async Task<PortStatus> AsyncCheck(IPAddress ipAddr, int port, int timeout = 3000)
        {
            var tcpClient = new TcpClient();

            Console.WriteLine($"Checking {ipAddr}:{port}");
            var status = await tcpClient.ConnectAsync(ipAddr, port, timeout);
            Console.WriteLine($"Checked {ipAddr}:{port} - {status}");

            return status;
        }
    }
}