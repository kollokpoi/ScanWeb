using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PacketDotNet;
using SharpPcap;

namespace scanSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ipAddressBase = "192.168.1.";
            int startRange = 1;
            int endRange = 100;

            for (int i = startRange; i <= endRange; i++)
            {
                string ipAddress = ipAddressBase + i.ToString();
                string macAddress = GetMacAddress(ipAddress);
            
                if (!string.IsNullOrEmpty(macAddress))
                {
                    Console.WriteLine($"Устройство с IP-адресом {ipAddress} и MAC-адресом {macAddress} обнаружено.");
                }
            }

            WakeFunction("00:E0:4F:15:60:9A");
        }

        public class WOLClass : UdpClient
        {
            public WOLClass() : base()
            { }
            //this is needed to send broadcast packet
            public void SetClientToBrodcastMode()
            {
                if (this.Active)
                    this.Client.SetSocketOption(SocketOptionLevel.Socket,
                                              SocketOptionName.Broadcast, 0);
            }
        }

        private static void WakeFunction(string MAC_ADDRESS)
        {
            WOLClass client = new WOLClass();
            client.Connect(new
               IPAddress(0xffffffff),  //255.255.255.255  i.e broadcast
               0x2fff); // port=12287 let's use this one 
            client.SetClientToBrodcastMode();
            //set sending bites
            int counter = 0;
            //buffer to be send
            byte[] bytes = new byte[1024];   // more than enough :-)
                                             //first 6 bytes should be 0xFF
            for (int y = 0; y < 6; y++)
                bytes[counter++] = 0xFF;
            //now repeate MAC 16 times
            for (int y = 0; y < 16; y++)
            {
                int i = 0;
                for (int z = 0; z < 6; z++)
                {
                    string subs = MAC_ADDRESS.Substring(i, 2);

                    bytes[counter++] =
                        byte.Parse(MAC_ADDRESS.Substring(i, 2), NumberStyles.HexNumber);
                    i += 3;
                }
            }

            //now send wake up packet
            int reterned_value = client.Send(bytes, 1024);
        }


        public static string GetMacAddress(string ipAddress)
        {
            try
            {
                var arp = new ArpUtility();
                string macAddress = arp.GetMacAddress(ipAddress);
                return macAddress;
            }
            catch (Exception)
            {
                // Обработка ошибок при получении MAC-адреса
                return null;
            }
        }
        public class ArpUtility
        {
            public string GetMacAddress(string ipAddress)
            {
                IPAddress target = IPAddress.Parse(ipAddress);
                byte[] macAddr = new byte[6];
                int macAddrLen = macAddr.Length;
                uint destIp = BitConverter.ToUInt32(target.GetAddressBytes(), 0);

                if (SendARP(destIp, 0, macAddr, ref macAddrLen) == 0)
                {
                    string macAddress = string.Join(":", macAddr
                        .Take(macAddrLen)
                        .Select(b => b.ToString("X2")));
                    return macAddress;
                }

                return null;
            }

            [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
            public static extern int SendARP(uint DestIP, uint SrcIP, byte[] pMacAddr, ref int PhyAddrLen);
        }
    }
}
