using Core.Methods.Linear;
using Core.Methods.Parallel;
using Core.Sockets.Udp;
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
        private UdpSocket _client;
        private IPEndPoint _serverIEP;

        public GaussSlaeSolver(int port, string ip)
        {
            _client = new UdpSocket(ip, port);
        }

        public bool Listen()
        {
            try
            {
                var data = new byte[256];
                var remoteIp = new IPEndPoint(IPAddress.Any, 0);

                if (_client.TryAccept(ref remoteIp))
                {
                    _serverIEP = remoteIp;

                    _client.Send(0, _serverIEP);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _client.Close();
            }

            return false;
        }

        public bool Process()
        {
            try
            {
                var sizes = _client.ReceiveArray(_serverIEP);
                var rowsCount = (int) sizes[0];

                Console.WriteLine($"Expected rows count: {rowsCount}");

                var matrixSize = (int) sizes[1];

                var rows = _client.ReceiveMatrix(rowsCount, _serverIEP);

                Console.WriteLine($"Number of rows received: {rows.Length}");

                var vector = _client.ReceiveArray(_serverIEP);
                var rowsComplited = 0;
                var index = 0;


                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i <= matrixSize; i++)
                {
                    if (i == matrixSize)
                    {
                        Console.WriteLine("\nWaiting for the start of sending results...");

                        _client.Receive(_serverIEP);

                        Console.WriteLine("\nSending started");

                        index = 0;

                        _client.Send(rowsCount, _serverIEP);

                        _client.Receive(_serverIEP);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.Send(rows[j], _serverIEP, ref index);
                        }

                        _client.Receive(_serverIEP);

                        _client.Send(vector, _serverIEP, ref index);

                        Console.WriteLine("\nForward phase complited");

                        return 1 == _client.Receive(_serverIEP).Data[0];
                    }
                    if (_client.Receive(_serverIEP).Data[0] == 0)
                    {
                        
                        _client.Send(rows[rowsComplited], _serverIEP, ref index);
                        _client.Send(vector[rowsComplited], _serverIEP);

                        rowsComplited++;

                        ExecuteForwardPhaseIteration(rows, rows[rowsComplited - 1], vector, vector[rowsComplited - 1], i, rowsComplited);
                    }
                    else
                    {
                        var mainVector = _client.Receive(_serverIEP).Data[0];
                        _client.Send(0, _serverIEP);
                        var mainRow = _client.ReceiveArray(_serverIEP);

                        if (rowsComplited != rowsCount)
                        {
                            ExecuteForwardPhaseIteration(rows, mainRow, vector, mainVector, i, rowsComplited);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        
    }
}
