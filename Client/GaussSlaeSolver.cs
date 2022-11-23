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
        private readonly SafeUdpSocket _client;
        private readonly IPEndPoint _serverIEP;

        public GaussSlaeSolver(int serverPort, string serverIp)
        {
            _client = new SafeUdpSocket();
            _serverIEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        }

        public bool Connect()
        {
            try
            {
                _client.Send(0, _serverIEP, -1);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public void Process()
        {
            try
            {
                var rowsCount = (int) _client.Receive(_serverIEP).Data[0];

                Console.WriteLine($"Expected rows count: {rowsCount}");

                var matrixSize = (int) _client.Receive(_serverIEP).Data[0];
                var rows = _client.ReceiveMatrix(rowsCount, _serverIEP);

                Console.WriteLine($"Number of rows received: {rows.Length}");

                var complItetations = new double[rowsCount];
                var vector = _client.ReceiveArray(_serverIEP);
                var rowsComplited = 0;

                
                
                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i < matrixSize; i++)
                {
                    if (rowsComplited == rowsCount || i == matrixSize - 1)
                    {
                        if (rowsComplited != rowsCount)
                        {
                            complItetations[rowsCount - 1] = matrixSize - 1;
                        }

                        _client.Send(0, _serverIEP);

                        Console.WriteLine("\nWaiting for the start of sending results...");

                        _client.Receive(_serverIEP);

                        Console.WriteLine("\nSending started");

                        _client.Send(complItetations, _serverIEP);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.Send(rows[j], _serverIEP);
                        }

                        _client.Send(vector, _serverIEP);

                        Console.WriteLine("\nForward phase complited");

                        _client.Close();
   
                        return;
                    }
                    else
                    {
                        _client.Send(1, _serverIEP);
                    }

                    var mainRowIndex = FindMainElement(rows, i, rowsComplited);

                    _client.Send(vector[mainRowIndex], _serverIEP);

                    _client.Send(rows[mainRowIndex], _serverIEP);

                    if (_client.Receive(_serverIEP).Data[0] == 1)
                    {
                        var mainRow = _client.ReceiveArray(_serverIEP);
                        var mainVector = _client.Receive(_serverIEP).Data[0];

                        ExecuteForwardPhaseIteration(rows, mainRow, vector, mainVector, i, rowsComplited);
                    }
                    else
                    {
                        SwapRows(rows, vector, mainRowIndex, rowsComplited);

                        complItetations[rowsComplited] = i;

                        rowsComplited++;

                        ExecuteForwardPhaseIteration(rows, rows[rowsComplited - 1], vector, vector[rowsComplited - 1], i, rowsComplited);   
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        
    }
}
