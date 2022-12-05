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

namespace Node
{
    public class GaussSlaeSolver : GaussMethodSolverParallel
    {
        private SafeUdpSocket _client;
        private IPEndPoint _serverIEP;

        public GaussSlaeSolver(int port, string ip)
        {
            _client = new SafeUdpSocket(ip, port);
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
                var index = 0;


                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i < matrixSize; i++)
                {
                    _client.Receive(_serverIEP);

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

                        index = 0;

                        _client.Send(complItetations, _serverIEP, ref index);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.Send(rows[j], _serverIEP, ref index);
                        }

                        _client.Send(vector, _serverIEP, ref index);

                        Console.WriteLine("\nForward phase complited");

                        _client.Close();
   
                        return;
                    }
                    else
                    {
                        _client.Send(1, _serverIEP);
                    }

                    var mainRowIndex = FindMainElement(rows, i, rowsComplited);

                    _client.Receive(_serverIEP);

                    _client.Send(vector[mainRowIndex], _serverIEP);

                    _client.Send(rows[mainRowIndex], _serverIEP, ref index);

                    if (_client.Receive(_serverIEP).Data[0] == 1)
                    {
                        var mainVector = _client.Receive(_serverIEP).Data[0];
                        var mainRow = _client.ReceiveArray(_serverIEP);
                        
                        _client.Send(0, _serverIEP);

                        ExecuteForwardPhaseIteration(rows, mainRow, vector, mainVector, i, rowsComplited);
                    }
                    else
                    {
                        _client.Send(0, _serverIEP);

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
