using Core.Methods.Parallel;
using Core.Sockets.Udp;
using DataAccess.Managers;
using Server.Loggers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Server.Solvers
{
    public class ParallelSolver : GaussMethodSolverParallel
    {
        private readonly UdpSocket _server;
        private List<Node> _nodes;


        private readonly IFileManager _fileManager;
        private readonly ITimeLogger _timeLogger;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress, IFileManager fileManager, ITimeLogger timeLogger)
        {
            _server = new UdpSocket(ipAddress, port);
            _nodes = new List<Node>();
            _fileManager = fileManager;
            _timeLogger = timeLogger;
        }

        public void StartServer(string nodesSilePath)
        {
            var addresses = _fileManager.GetNodesAddresses(nodesSilePath);
            var index = 0;

            foreach (var addr in addresses)
            {
                var address = addr.Split(':');
                var nodeAddress = new IPEndPoint(IPAddress.Parse(address[0]), int.Parse(address[1]));

                _server.Send(-1, nodeAddress, 0);
                _server.Receive(nodeAddress, ref index);

                _nodes.Add(new Node { IEP = nodeAddress, ReceivedPackegesCount = 0, SentPackgesCount = 0 });
                Notify.Invoke(_nodes.Count, addr);
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

            foreach (var node in _nodes)
            {
                _server.Send(1, node.IEP, node.SentPackgesCount++);
            }

            _timeLogger.SolvingTime = stopwatch.ElapsedMilliseconds;

            return result;
        }

        private void SendRows(double[][] matrix, double[] vector)
        {
            Parallel.For(0, _nodes.Count, i =>
            {
                var rowsCount = CalculateRowsCountForClient(matrix.Length, _nodes.Count, i);
                var vectorRequest = new double[rowsCount];
                var iterator = 0;
                var sendIndex = _nodes[i].SentPackgesCount;
                var receiveIndex = _nodes[i].ReceivedPackegesCount;

                _server.Send(new double[2] { rowsCount, vector.Length }, _nodes[i].IEP, ref sendIndex, ref receiveIndex);

                for (var j = i; j < vector.Length; j += _nodes.Count)
                {
                    _server.Send(matrix[j], _nodes[i].IEP, ref sendIndex, ref receiveIndex);
                    vectorRequest[iterator++] = vector[j];
                }

                _server.Send(vectorRequest, _nodes[i].IEP, ref sendIndex, ref receiveIndex);

                _nodes[i].SentPackgesCount = sendIndex;
                _nodes[i].ReceivedPackegesCount = receiveIndex;

            });
        }

        private void GetForwardPhase(int iteration, int matrixSize)
        {
            var row = new double[matrixSize];
            var vector = 0.0;

            for (var i = 0; i < _nodes.Count; i++)
            {
                if (i == (iteration % _nodes.Count()))
                {
                    var receiveIndex = _nodes[i].ReceivedPackegesCount;
                    var sendIndex = _nodes[i].SentPackgesCount;

                    _server.Send(0, _nodes[i].IEP, sendIndex++);

                    row = _server.ReceiveArray(_nodes[i].IEP, ref receiveIndex, ref sendIndex);
                    vector = _server.Receive(_nodes[i].IEP, ref receiveIndex).Data[0];

                    _nodes[i].SentPackgesCount = sendIndex;
                    _nodes[i].ReceivedPackegesCount = receiveIndex;
                }
                else
                {
                    _server.Send(1, _nodes[i].IEP, _nodes[i].SentPackgesCount++);
                }
            }

            for (var i = 0; i < _nodes.Count; i++)
            {
                if (i != (iteration % _nodes.Count()))
                {
                    var receiveIndex = _nodes[i].ReceivedPackegesCount;
                    var sendIndex = _nodes[i].SentPackgesCount;

                    _server.Send(vector, _nodes[i].IEP, sendIndex++);
                    _server.Receive(_nodes[i].IEP, ref receiveIndex);
                    _server.Send(row, _nodes[i].IEP, ref sendIndex, ref receiveIndex);

                    _nodes[i].SentPackgesCount = sendIndex;
                    _nodes[i].ReceivedPackegesCount = receiveIndex;
                }
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {

            for (var i = 0; i < _nodes.Count; i++)
            {
                var receiveIndex = _nodes[i].ReceivedPackegesCount;
                var sendIndex = _nodes[i].SentPackgesCount;

                _server.Send(0, _nodes[i].IEP, sendIndex++);

                var rowsCount = _server.Receive(_nodes[i].IEP, ref receiveIndex).Data[0];
                _server.Send(0, _nodes[i].IEP, sendIndex++);

                var arrays = _server.ReceiveMatrix((int)rowsCount, _nodes[i].IEP, ref receiveIndex, ref sendIndex);
                _server.Send(0, _nodes[i].IEP, sendIndex++);

                var vectorList = _server.ReceiveArray(_nodes[i].IEP, ref receiveIndex, ref sendIndex);
                var iteration = i;

                for (var j = 0; j < rowsCount; j++)
                {
                    matrix[iteration] = arrays[j];
                    vector[iteration] = vectorList[j];
                    iteration += _nodes.Count;
                }

                _nodes[i].SentPackgesCount = sendIndex;
                _nodes[i].ReceivedPackegesCount = receiveIndex;
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

        private class Node
        {
            public IPEndPoint IEP { get; set; }
            public int SentPackgesCount { get; set; }
            public int ReceivedPackegesCount { get; set; }

        }
    }
}
