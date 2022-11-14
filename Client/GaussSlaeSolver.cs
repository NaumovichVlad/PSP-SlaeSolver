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
        private readonly UdpCilentSafe _client;

        public GaussSlaeSolver(int serverPort, string serverIp)
        {
            _client = new UdpCilentSafe(serverPort, serverIp);
        }

        public void Process()
        {
            if (_client.Connect())
            {
                Console.WriteLine("Client connected");
            }
            else
            {
                Console.WriteLine("Connection error");
                return;
            }
            try
            {
                var rowsCount = _client.GetIntResponce();
                var matrixSize = _client.GetIntResponce();
                var rows = _client.GetMatrixResponceSafe(rowsCount, matrixSize);
                var complItetations = new double[rowsCount];
                var vector = _client.GetArrayResponceSafe(rowsCount);
                var rowsComplited = 0;

                Console.WriteLine($"Expected rows count: {rowsCount}");
                Console.WriteLine($"Number of rows received: {rows.Length}");
                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i < matrixSize; i++)
                {
                    _client.GetIntResponce();

                    if (rowsComplited == rowsCount || i == matrixSize - 1)
                    {
                        if (rowsComplited != rowsCount)
                        {
                            complItetations[rowsCount - 1] = matrixSize - 1;
                        }

                        _client.SendIntRequest(0);

                        Console.WriteLine("\nWaiting for the start of sending results...");

                        _client.GetIntResponce();

                        _client.SendIntRequest(rowsCount);

                        _client.SendArrayRequestSafe(complItetations);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.SendArrayRequestSafe(rows[j]);
                        }

                        _client.SendArrayRequestSafe(vector);

                        Console.WriteLine("\nForward phase complited");

                        _client.Close();
   
                        return;
                    }
                    else
                    {
                        _client.SendIntRequest(1);
                        _client.GetIntResponce();
                    }

                    var mainRowIndex = FindMainElement(rows, i, rowsComplited);

                    _client.SendArrayRequestSafe(rows[mainRowIndex]);

                    _client.SendDoubleRequest(vector[mainRowIndex]);


                    if (_client.GetIntResponce() == 1)
                    {
                        var mainRow = _client.GetArrayResponceSafe(matrixSize);
                        var mainVector = _client.GetDoubleResponce();

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
