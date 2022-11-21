using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Sockets.Udp
{
    public class SafeUdpSocket
    {
        private readonly Socket _socket;
        private const int _packageSize = 512;

        public SafeUdpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        public void Create(string ip, int port)
        {
            var localIp = new IPEndPoint(IPAddress.Parse(ip), port);

            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socket.Bind(localIp);
        }

        public int ReceiveInt(EndPoint client)
        {
            var data = new byte[sizeof(int)];

            _socket.ReceiveFrom(data, ref client);

            return BitConverter.ToInt32(data);
        }

        public double ReceiveDouble(EndPoint client)
        {
            var data = new byte[sizeof(double)];

            _socket.ReceiveFrom(data, ref client);

            return BitConverter.ToDouble(data);
        }

        public double[][] ReceiveMatrix(int rowsCount, int rowSize, EndPoint client)
        {
            var rows = new double[rowsCount][];

            for (var i = 0; i < rowsCount; i++)
            {
                rows[i] = ReceiveArray(rowSize, client);
            }

            return rows;
        }

        public double[] ReceiveArray(int arraySize, EndPoint client)
        {
            var data = new byte[sizeof(double) * arraySize];
            var array = new double[arraySize];

            var packageCount = ReceiveInt(client);
            var packagesSize = 0;

            for (var i = 0; i < packageCount; i++)
            {
                var packageSize = ReceiveInt(client);

                while (true)
                {
                    if (_socket.Poll(10000, SelectMode.SelectRead))
                    {
                        _socket.ReceiveFrom(data, packagesSize, packageSize, SocketFlags.None, ref client);
                        SendStatus(Statuses.OK, client);
                        break;
                    }
                    else
                    {
                        SendStatus(Statuses.NO, client);
                    }
                }

                packagesSize += packageSize;
            }

            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            return array;
        }

        public void SendArray(double[] array, EndPoint client)
        {
            var data = new byte[array.Length * sizeof(double)];

            Buffer.BlockCopy(array, 0, data, 0, data.Length);

            var packageCount = CalculatePackageCount(array.Length * sizeof(double));

            SendInt(packageCount, client);

            for (var i = 0; i < packageCount; i++)
            {
                var packageSize = _packageSize;

                if (i == packageCount - 1)
                {
                    packageSize = array.Length * sizeof(double) - _packageSize * i;
                }

                SendInt(packageSize, client);

                do
                {
                    if (_socket.SendTo(data, _packageSize * i, packageSize, SocketFlags.None, client) != packageSize)
                    {
                        Thread.Sleep(10000);
                    }
                } while (ReceiveStatus(client) != Statuses.OK);
            }
        }

        public void SendDouble(double number, EndPoint client)
        {
            _socket.SendTo(BitConverter.GetBytes(number), client);
        }

        public void SendInt(int number, EndPoint client)
        {
            _socket.SendTo(BitConverter.GetBytes(number), client);
        }

        public void Close()
        {
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }

        private int CalculatePackageCount(int dataSize)
        {
            var packageCount = dataSize / _packageSize;

            if (dataSize % _packageSize != 0)
            {
                packageCount++;
            }

            return packageCount;
        }

        public void SendStatus(Statuses status, EndPoint client)
        {
            _socket.SendTo(Encoding.ASCII.GetBytes(status.ToString()), client);
        }

        public Statuses ReceiveStatus(EndPoint client)
        {
            object? result;
            var status = string.Empty;

            do
            {
                var data = new byte[2];

                _socket.ReceiveFrom(data, ref client);

                status = Encoding.ASCII.GetString(data);

            } while (!Enum.TryParse(typeof(Statuses), status, out result));

            return (Statuses) result;
        }
        public bool TryAccept(ref EndPoint client)
        {
            object? result;
            var data = new byte[2];

            _socket.ReceiveFrom(data, ref client);

            var status = Encoding.ASCII.GetString(data);

            if (Enum.TryParse(typeof(Statuses), status, out result))
            {
                return (Statuses)result == Statuses.OK;

            }
            else
            {
                return false;
            }
        }
    }
}
