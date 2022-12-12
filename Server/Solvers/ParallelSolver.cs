using Core.Methods.Linear;
using Core.Methods.Parallel;
using Core.Sockets.Udp;
using DataAccess.Managers;
using Server.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Solvers
{
    public class ParallelSolver : GaussMethodSolverParallel
    { 
        private readonly UdpSocket _server;
        private List<IPEndPoint> _activeClients;

        private readonly IFileManager _fileManager;
        private readonly ITimeLogger _timeLogger;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress, IFileManager fileManager, ITimeLogger timeLogger)
        {
            _server = new UdpSocket(ipAddress, port);
            _activeClients = new List<IPEndPoint>();
            _fileManager = fileManager;
            _timeLogger = timeLogger;
        }

        public void StartServer(string nodesSilePath)
        {
            var addresses = _fileManager.GetNodesAddresses(nodesSilePath);

            foreach (var addr in addresses)
            {
                var address = addr.Split(':');
                var nodeAddress = new IPEndPoint(IPAddress.Parse(address[0]), int.Parse(address[1]));

                _server.Send(-1, nodeAddress);
                _server.Receive(nodeAddress);

                _activeClients.Add(nodeAddress);
                Notify.Invoke(_activeClients.Count, addr);
            }

        }

        public double[] Solve(string pathA, string pathB, string pathRes)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            var matrix = _fileManager.ReadMatrix(pathA);
            var vector = _fileManager.ReadVector(pathB);

            stopwatch.Stop();

            _timeLogger.ReadingTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Reset();
            stopwatch.Start();
            
            SendRows(matrix, vector);

            for (var i = 0; i < matrix.Length; i++)
            {
                GetForwardPhase(i, matrix.Length);
            }

            var result = GetBackPhase(matrix, vector);

            stopwatch.Stop();

            foreach (var client in _activeClients)
            {
                _server.Send(1, client);
            }

            _timeLogger.SolvingTime = stopwatch.ElapsedMilliseconds;

            return result;
        }

        private void SendRows(double[][] matrix, double[] vector)
        {
            Parallel.For(0, _activeClients.Count, i =>
            {
                var rowsCount = CalculateRowsCountForClient(matrix.Length, _activeClients.Count, i);
                var vectorRequest = new double[rowsCount];
                var iterator = 0;
                var index = 0;

                _server.Send(new double[2] { rowsCount, vector.Length }, _activeClients[i], ref index);

                for (var j = i; j < vector.Length; j += _activeClients.Count)
                {
                    _server.Send(matrix[j], _activeClients[i], ref index);
                    vectorRequest[iterator++] = vector[j];
                }

                _server.Send(vectorRequest, _activeClients[i], ref index);

            });
        }

        private void GetForwardPhase(int iteration, int matrixSize)
        {
            var index = 0;

            var row = new double[matrixSize];
            var vector = 0.0;

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i == (iteration % _activeClients.Count()))
                {
                    _server.Send(0, _activeClients[i]);

                    row = _server.ReceiveArray(_activeClients[i]);
                    vector = _server.Receive(_activeClients[i]).Data[0];
                }
                else
                {
                    _server.Send(1, _activeClients[i]);
                }
            }

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i != (iteration % _activeClients.Count()))
                {
                    _server.Send(vector, _activeClients[i]);
                    _server.Receive(_activeClients[i]);
                    _server.Send(row, _activeClients[i], ref index);
                }
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {

            for (var i = 0; i < _activeClients.Count; i++)
            {
                _server.Send(0, _activeClients[i]);

                var rowsCount = _server.Receive(_activeClients[i]).Data[0];
                _server.Send(0, _activeClients[i]);
                var arrays = _server.ReceiveMatrix((int) rowsCount, _activeClients[i]);
                _server.Send(0, _activeClients[i]);
                var vectorList = _server.ReceiveArray(_activeClients[i]);
                var iteration = i; 

                for (var j = 0; j < rowsCount; j++)
                {
                    matrix[iteration] = arrays[j];
                    vector[iteration] = vectorList[j];
                    iteration += _activeClients.Count;
                }
            }

            return ExecuteBackPhaseIteration(matrix, vector);
        }

        private int CalculateRowsCountForClient(int matrixSize, int clientsCount, int clientIndex)
        {
            var rowsCount = matrixSize / clientsCount;

            if (matrixSize % clientsCount > clientIndex)
            {
                rowsCount++;
            }

            return rowsCount;
        }

        public string GetTimeLog()
        {
            return _timeLogger.GetLog();
        }
    }
}
