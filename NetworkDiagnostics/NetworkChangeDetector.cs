using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetworkDiagnostics
{
    internal static class NetworkChangeDetector
    {
        internal static void LogNetworkInfoWithoutDebounce()
        {
            var localV4 = ProbeLocalAddress(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53));
            var localV6 = ProbeLocalAddress(new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), 53));

            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Select(x => new NetInterface
            {
                Name = x.Name,
                Description = x.Description,
                NetworkInterfaceType = x.NetworkInterfaceType,
                OperationalStatus = x.OperationalStatus,
                Speed = x.Speed,
                Addresses = x.GetIPProperties().UnicastAddresses.Select(x => x.Address).ToArray()
            }).ToArray();

            var info = new NetInfo
            {
                LocalIpv4 = MaskAddressForLogging(localV4),
                LocalIpv6 = MaskAddressForLogging(localV6),
                Interfaces = interfaces,
                IsIpv6Enabled = localV6 is not null,
                IsWifiEnabled = interfaces.Any(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            };

            for (var i = 0; i < interfaces.Length; i++)
            {
                var ni = interfaces[i];
                if (ni.Addresses.Contains(localV4))
                {
                    ni.Flags |= NetInterfaceFlags.DefaultIpv4Interface;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        info.IsWifiUsed = true;
                }
                if (ni.Addresses.Contains(localV6))
                {
                    ni.Flags |= NetInterfaceFlags.DefaultIpv6Interface;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        info.IsWifiUsed = true;
                }
            }

            // Data collection completed, masking ips before logging

            for (var i = 0; i < interfaces.Length; i++)
            {
                var ni = interfaces[i];
                ni.Addresses = ni.Addresses.Select(x => MaskAddressForLogging(x)!).ToArray();
            }

            // Log

            var json = JsonSerializer.Serialize(info, new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new IPAddressToStringJsonConverter()
                }
            });

            Console.WriteLine(json);
        }

        internal static IPAddress? MaskAddressForLogging(IPAddress? address)
        {
            switch ((address?.AddressFamily) ?? AddressFamily.Unknown)
            {
                case AddressFamily.InterNetwork:
                    {
                        var bytes = address!.GetAddressBytes();
                        bytes[3] = 0;
                        return new IPAddress(bytes);
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        var bytes = address!.GetAddressBytes();
                        if (address.IsIPv4MappedToIPv6)
                        {
                            bytes[15] = 0;
                        }
                        else
                        {
                            bytes[8] = 0;
                            bytes[9] = 0;
                            bytes[10] = 0;
                            bytes[11] = 0;
                            bytes[12] = 0;
                            bytes[13] = 0;
                            bytes[14] = 0;
                            bytes[15] = 0;
                        }
                        return new IPAddress(bytes);
                    }
                default:
                    return address;
            }
        }

        private static IPAddress? ProbeLocalAddress(IPEndPoint remote)
        {
            try
            {
                using var socket = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remote);
                return (socket.LocalEndPoint as IPEndPoint)?.Address;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public class NetInfo
        {
            /// <summary>
            /// Does wireless network interface exist
            /// </summary>
            public bool IsWifiEnabled { get; set; }

            /// <summary>
            /// Is wireless network being used
            /// </summary>
            public bool IsWifiUsed { get; set; }

            public bool IsIpv6Enabled { get; set; }

            public IPAddress? LocalIpv4 { get; set; }
            public IPAddress? LocalIpv6 { get; set; }

            public NetInterface[] Interfaces { get; set; } = Array.Empty<NetInterface>();
        }

        public class NetInterface
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public NetInterfaceFlags Flags { get; set; }

            public NetworkInterfaceType NetworkInterfaceType { get; set; }
            public OperationalStatus OperationalStatus { get; set; }
            public long Speed { get; set; }

            public IPAddress[] Addresses { get; set; } = Array.Empty<IPAddress>();
        }

        [Flags]
        public enum NetInterfaceFlags
        {
            None = 0,
            DefaultIpv4Interface = 1 << 0,
            DefaultIpv6Interface = 1 << 1,
        }
    }
}
