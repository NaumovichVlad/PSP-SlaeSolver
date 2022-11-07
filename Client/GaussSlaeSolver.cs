using Core.Methods.Linear;
using Core.Methods.Parallel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class GaussSlaeSolver : GaussMethodSolverParallel
    {
        private readonly int _serverPort;
        private readonly string _serverIp;
        private readonly UdpClient _client;

        public GaussSlaeSolver(int serverPort, string serverIp)
        {
            _serverPort = serverPort;
            _serverIp = serverIp;
            _client = new UdpClient();
        }

        public bool Connect()
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes("Ready");

                _client.Connect(_serverIp, _serverPort);
                _client.Send(data);

                Process();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void Process()
        {
            IPEndPoint? serverIp = null;

            try
            {
                var rowsCount = GetIntResponce(serverIp);
                var matrixSize = GetIntResponce(serverIp);
                var rows = GetRows(serverIp, rowsCount);
                var vector = GetArrayResponce(serverIp);
                var rowsComplited = 0;

                Console.WriteLine($"Expected rows count: {rowsCount}");
                Console.WriteLine($"Number of rows received: {rows.Length}");
                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                for (var i = 0; i < matrixSize; i++)
                {
                    if (rowsComplited == rowsCount)
                    {
                        SendIntRequest(serverIp, 1);

                        GetIntResponce(serverIp);

                        SendIntRequest(serverIp, rowsCount);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            SendArrayRequest(serverIp, rows[j]);
                        }

                        SendArrayRequest(serverIp, vector);

                        Console.WriteLine("Forward phase complited");
   
                        return;
                    }
                    else
                    {
                        SendIntRequest(serverIp, 0);
                    }

                    var mainRowIndex = FindMainElement(rows, i, rowsComplited);

                    SendDoubleRequest(serverIp, vector[mainRowIndex]);

                    SendArrayRequest(serverIp, rows[mainRowIndex]);
                    

                    if (GetIntResponce(serverIp) == 1)
                    {
                        var mainRow = GetArrayResponce(serverIp);
                        var mainVector = GetDoubleResponce(serverIp);

                        ExecuteForwardPhaseIteration(rows, mainRow, vector, mainVector, i);
                    }
                    else
                    {
                        SwapRows(rows, vector, mainRowIndex, rowsComplited);
                        ExecuteForwardPhaseIteration(rows, rows[mainRowIndex], vector, vector[mainRowIndex], i, rowsComplited++);
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private int GetIntResponce(IPEndPoint? serverIp)
        {
            byte[] data = _client.Receive(ref serverIp);

            return BitConverter.ToInt32(data);
        }

        private double GetDoubleResponce(IPEndPoint? serverIp)
        {
            byte[] data = _client.Receive(ref serverIp);

            return BitConverter.ToDouble(data);
        }

        private double[][] GetRows(IPEndPoint? serverIp, int rowsCount)
        {
            var rows = new double[rowsCount][];

            for (var i = 0; i < rowsCount; i++)
            {
                rows[i] = GetArrayResponce(serverIp);
            }

            return rows;
        }

        private double[] GetArrayResponce(IPEndPoint? serverIp)
        {
            byte[] data = _client.Receive(ref serverIp);
            var array = new double[data.Length / sizeof(double)];
            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            SendIntRequest(serverIp, 0);

            return array;
        }

        private void SendArrayRequest(IPEndPoint serverIp, double[] array)
        {
            byte[] data = new byte[array.Length * sizeof(double)];

            Buffer.BlockCopy(array, 0, data, 0, data.Length);

            _client.Send(data);
        }

        private void SendDoubleRequest(IPEndPoint serverIp, double number)
        {
            var data = BitConverter.GetBytes(number);

            _client.Send(data);
        }

        private void SendIntRequest(IPEndPoint serverIp, int number)
        {
            var data = BitConverter.GetBytes(number);

            _client.Send(data);
        }
    }
}
