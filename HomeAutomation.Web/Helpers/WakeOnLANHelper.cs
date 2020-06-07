using HomeAutomation.Web.Helpers.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Helpers
{
    public class WakeOnLANHelper : IWakeOnLANHelper
    {
        public async Task WakeAsync(string macAddress, string broadcastIP)
        {
            using var client = new UdpClient();
            var datagram = new byte[102];

            for (var i = 0; i <= 5; i++)
            {
                datagram[i] = 0xff;
            }

            string[] macDigits;
            if (macAddress.Contains("-"))
            {
                macDigits = macAddress.Split('-');
            }
            else
            {
                macDigits = macAddress.Split(':');
            }

            if (macDigits.Length != 6)
            {
                throw new ArgumentException("Incorrect MAC address supplied!");
            }

            var start = 6;
            for (var i = 0; i < 16; i++)
            {
                for (var x = 0; x < 6; x++)
                {
                    datagram[start + i * 6 + x] = (byte)Convert.ToInt32(macDigits[x], 16);
                }
            }

            var broadcastAddress = IPAddress.Parse(broadcastIP);

            await client.SendAsync(datagram, datagram.Length, broadcastAddress.ToString(), 3);
        }
    }
}
