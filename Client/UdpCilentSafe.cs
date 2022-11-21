using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class UdpCilentSafe
    {
        private const int _packageSize = 512;
        private readonly int _serverPort;
        private readonly string _serverIp;
        private readonly Socket _client;

        public UdpCilentSafe(int serverPort, string serverIp)
        {
            _serverPort = serverPort;
            _serverIp = serverIp;
            _client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _client.ReceiveBufferSize = 10000000;
            _client.SendBufferSize = 10000000;
        }

        public bool Connect()
        {
            try
            {
                _client.Connect(_serverIp, _serverPort);
                _client.Send(BitConverter.GetBytes(0));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void Close()
        {
            _client.Shutdown(SocketShutdown.Both);
        }

        public int GetIntResponce()
        {
            var data = new byte[sizeof(int)];
            
            _client.Receive(data, sizeof(int), SocketFlags.None);

            return BitConverter.ToInt32(data);
        }

        public double GetDoubleResponce()
        {
            var data = new byte[sizeof(double)];
            
            _client.Receive(data);

            return BitConverter.ToDouble(data);
        }

        public double[][] GetMatrixResponceSafe(int rowsCount, int rowSize)
        {
            var rows = new double[rowsCount][];

            for (var i = 0; i < rowsCount; i++)
            {
                rows[i] = GetArrayResponceSafe(rowSize);
            }

            return rows;
        }

        public double[] GetArrayResponceSafe(int arraySize)
        {
            var data = new byte[sizeof(double) * arraySize];
            var array = new double[arraySize];

            var packageCount = GetIntResponce();
            var packagesSize = 0;

            for (var i = 0; i < packageCount; i++)
            {
                var packageSize = GetIntResponce();

                do
                {
                    if (_client.Poll(50000, SelectMode.SelectRead))
                    {
                        _client.Receive(data, packagesSize, packageSize, SocketFlags.None);

                        SendIntRequest(0);
                        break;
                    }
                    else
                    {
                        SendIntRequest(1);
                    }
                } while (true);

                packagesSize += packageSize;
            }

            
            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            return array;
        }

        public void SendArrayRequestSafe(double[] array)
        {
            var data = new byte[array.Length * sizeof(double)];

            Buffer.BlockCopy(array, 0, data, 0, data.Length);

            var packageCount = CalculatePackageCount(array.Length * sizeof(double));

            SendIntRequest(packageCount);

            for (var i = 0; i < packageCount; i++)
            {
                var packageSize = _packageSize;

                if (i == packageCount - 1)
                {
                    packageSize = array.Length * sizeof(double) - _packageSize * i ;
                }

                SendIntRequest(packageSize);

                do
                {
                    if (_client.Send(data, _packageSize * i, packageSize, SocketFlags.None) != packageSize)
                    {
                        Thread.Sleep(10000);
                    }
                } while (GetIntResponce() != 0);
            }
        }


        public void SendDoubleRequest(double number)
        {
            _client.Send(BitConverter.GetBytes(number));
        }

        public void SendIntRequest(int number)
        {
            _client.Send(BitConverter.GetBytes(number));
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
    }
}
