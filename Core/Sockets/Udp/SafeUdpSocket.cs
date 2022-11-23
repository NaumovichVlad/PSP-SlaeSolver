using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Sockets.Udp
{
    public class SafeUdpSocket
    {
        private readonly UdpClient _client;
        private const int _packageSize = 500;

        public UdpClient Client => _client;

        public SafeUdpSocket(string ip, int port)
        {

            _client = new UdpClient();
            _client.Client.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Ttl = 240;
            _client.Client.ReceiveBufferSize = 100000000;
            _client.Client.SendBufferSize = 100000000;
        }

        public SafeUdpSocket()
        {
            _client = new UdpClient();
        }

        public Package Receive(IPEndPoint client)
        {
            var data = _client.Receive(ref client);

            var package = JsonConvert.DeserializeObject<Package>(Encoding.ASCII.GetString(data));

            return package;
        }

        public double[][] ReceiveMatrix(int rowsCount, IPEndPoint clientIEP)
        {
            var packages = new List<Package>();

            for (var i = 0; i < rowsCount; i++)
            {
                packages.AddRange(ReceivePackages(clientIEP));
            }

            return JoinPackages(packages, rowsCount);
        }


        public double[] ReceiveArray(IPEndPoint clientIEP)
        {
            return JoinPackages(ReceivePackages(clientIEP));
        }

        public List<Package> ReceivePackages(IPEndPoint clientIEP)
        {
            var packageCount = (int)Receive(clientIEP).Data[0];
            var packages = new List<Package>();

            for (var i = 0; i < packageCount; i++)
            {
                packages.Add(Receive(clientIEP));
            }

            return packages;
        }

        private double[] JoinPackages(List<Package> packages)
        {
            var i = 0;
            var length = packages.Sum(p => p.Data.Length);
            var array = new double[length];

            packages.OrderBy(p => p.Id);

            foreach (var package in packages)
            {
                for (var j = 0; j < package.Data.Length; j++)
                {
                    array[i++] = package.Data[j];
                }
            }

            return array;
        }

        private double[][] JoinPackages(List<Package> packages, int rowCount)
        {
            var i = 0;
            var length = packages.Sum(p => p.Data.Length);
            var rowLength = length / rowCount;
            var array = new double[rowCount][];

            packages.OrderBy(p => p.Id);

            foreach (var package in packages)
            {
                for (var j = 0; j < package.Data.Length; j++)
                {
                    if (i % rowLength == 0)
                    {
                        array[i / rowLength] = new double[rowLength];
                    }

                    array[i / rowLength][i % rowLength] = package.Data[j];
                    i++;
                }
            }

            return array;
        }

        public void Send(double[] array, IPEndPoint clientIEP, int firstPackageNum = 0)
        {
            var packageCount = CalculatePackageCount(array.Length);

            Send(packageCount, clientIEP);

            for (var i = 0; i < packageCount; i++)
            {
                int packageSize;
                var package = new Package() { Id = firstPackageNum };

                if (i == packageCount - 1)
                {
                    packageSize = array.Length - _packageSize * i;
                }
                else
                {
                    packageSize = _packageSize;
                }

                package.Data = new double[packageSize];
                Array.Copy(array, _packageSize * i, package.Data, 0, packageSize);

                _client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(package)), clientIEP);
                Thread.Sleep(1);
            }
        }

        public void Send(double number, IPEndPoint clientIEP, int packageNum = 0)
        {
            var package = new Package { Id = packageNum, Data = new double[1] { number } };
            var data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(package));

            _client.Send(data, clientIEP);
        }

        public void Close()
        {
            if (_client != null)
            {
                _client.Close();
            }
        }

        private int CalculatePackageCount(int dataArraySize)
        {
            var packageCount = dataArraySize / _packageSize;

            if (dataArraySize % _packageSize != 0)
            {
                packageCount++;
            }

            return packageCount;
        }

        public bool TryAccept(ref IPEndPoint clientIEP)
        {
            try
            {
                var package = JsonConvert.DeserializeObject<Package>(Encoding.ASCII.GetString(_client.Receive(ref clientIEP)));


                return package.Id == -1;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
