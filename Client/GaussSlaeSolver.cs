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
        private readonly EndPoint _serverEp;

        public GaussSlaeSolver(int serverPort, string serverIp)
        {
            _client = new SafeUdpSocket();
            _serverEp = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        }

        public bool Connect()
        {
            try
            {
                _client.SendStatus(Statuses.OK, _serverEp);
                if (_client.ReceiveStatus(_serverEp) == Statuses.OK)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        public void Process()
        {
            try
            {
                var rowsCount = _client.ReceiveInt(_serverEp);

                Console.WriteLine($"Expected rows count: {rowsCount}");

                var matrixSize = _client.ReceiveInt(_serverEp);
                var rows = _client.ReceiveMatrix(rowsCount, matrixSize, _serverEp);

                Console.WriteLine($"Number of rows received: {rows.Length}");

                var complItetations = new double[rowsCount];
                var vector = _client.ReceiveArray(rowsCount, _serverEp);
                var rowsComplited = 0;

                
                
                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i < matrixSize; i++)
                {
                    _client.ReceiveStatus(_serverEp);

                    if (rowsComplited == rowsCount || i == matrixSize - 1)
                    {
                        if (rowsComplited != rowsCount)
                        {
                            complItetations[rowsCount - 1] = matrixSize - 1;
                        }

                        _client.SendStatus(Statuses.OK, _serverEp);

                        Console.WriteLine("\nWaiting for the start of sending results...");

                        _client.ReceiveStatus(_serverEp);

                        _client.SendInt(rowsCount, _serverEp);

                        _client.SendArray(complItetations, _serverEp);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.SendArray(rows[j], _serverEp);
                        }

                        _client.SendArray(vector, _serverEp);

                        Console.WriteLine("\nForward phase complited");

                        _client.Close();
   
                        return;
                    }
                    else
                    {
                        _client.SendStatus(Statuses.NO, _serverEp);
                        _client.ReceiveStatus(_serverEp);
                    }

                    var mainRowIndex = FindMainElement(rows, i, rowsComplited);

                    _client.SendArray(rows[mainRowIndex], _serverEp);

                    _client.SendDouble(vector[mainRowIndex], _serverEp);


                    if (_client.ReceiveStatus(_serverEp) == Statuses.NO)
                    {
                        var mainRow = _client.ReceiveArray(matrixSize, _serverEp);
                        var mainVector = _client.ReceiveDouble(_serverEp);

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
