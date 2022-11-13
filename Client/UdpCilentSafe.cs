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
        private readonly int _serverPort;
        private readonly string _serverIp;
        private readonly Socket _client;

        public UdpCilentSafe(int serverPort, string serverIp)
        {
            _serverPort = serverPort;
            _serverIp = serverIp;
            _client = new Socket(SocketType.Dgram, ProtocolType.Udp);
        }

        public bool Connect()
        {
            try
            {
                //Сделать получше, а то хуета
                var random = new Random();
                var localIp = new IPEndPoint(IPAddress.Parse("127.0.0." + random.Next(1, 254)), random.Next(5000, 6000));
                //

                _client.Bind(localIp);
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
            
            _client.Receive(data);

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

            while(_client.Receive(data) != sizeof(double) * arraySize)
            {
                SendIntRequest(1);
            }

            SendIntRequest(0);
            
            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            return array;
        }

        public void SendArrayRequestSafe(double[] array)
        {
            var data = new byte[array.Length * sizeof(double)];

            Buffer.BlockCopy(array, 0, data, 0, data.Length);

            do
            {
                _client.Send(data);

            } while (GetIntResponce() != 0);
        }


        public void SendDoubleRequest(double number)
        {
            _client.Send(BitConverter.GetBytes(number));
        }

        public void SendIntRequest(int number)
        {
            _client.Send(BitConverter.GetBytes(number));
        }
    }
}
