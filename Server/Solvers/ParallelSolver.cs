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
        private readonly SafeUdpSocket _server;
        private List<IPEndPoint> _activeClients;
        private List<IPEndPoint> _waitingClients;
        private readonly int _waitClientsNum;

        private readonly IFileManager _fileManager;
        private readonly ITimeLogger _timeLogger;
        private readonly Thread _listeningTask;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress, IFileManager fileManager, int waitClientsNum, ITimeLogger timeLogger)
        {
            _server = new SafeUdpSocket(ipAddress, port);
            _activeClients = new List<IPEndPoint>();
            _fileManager = fileManager;
            _listeningTask = new Thread(Listen);
            _waitingClients = new List<IPEndPoint>();
            _waitClientsNum = waitClientsNum;
            _timeLogger = timeLogger;
        }

        public void StartServer()
        {
            _listeningTask.Start();
        }

        private void Listen()
        {
            try
            {
                while (_activeClients.Count < _waitClientsNum)
                {
                    var data = new byte[256];
                   var remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    if (_server.TryAccept(ref remoteIp))
                    {
                        bool addClient = true;

                        for (int i = 0; i < _activeClients.Count; i++)
                        {
                            if (_activeClients[i].Address.ToString() == remoteIp.Address.ToString())
                            {
                                addClient = false;
                                break;
                            }
                        }

                        if (addClient == true)
                        {
                            _activeClients.Add(remoteIp);
                            Notify.Invoke(_activeClients.Count, remoteIp.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _server.Close();
            }
        }

        public double[] Solve(string pathA, string pathB, string pathRes)
        {
            _listeningTask.Interrupt();

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

                _server.Send(rowsCount, _activeClients[i], 0);
                _server.Send(vector.Length, _activeClients[i], 1);

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

            for (var i = 0; i < _activeClients.Count; i++)
            {
                _server.Send(0, _activeClients[i]);

                var responce = _server.Receive(_activeClients[i]);

                if (responce.Data[0] == 0 && responce.Data.Length == 1)
                {
                    _waitingClients.Add(_activeClients[i]);
                    _activeClients.RemoveAt(i);

                    i--;

                    continue;
                }
            }

            if (_activeClients.Count == 0)
            {
                return;
            }

            var rows = new double[_activeClients.Count][];
            var vector = new double[_activeClients.Count];

            for (var i = 0; i < _activeClients.Count; i++)
            {
                _server.Send(0, _activeClients[i]);

                vector[i] = _server.Receive(_activeClients[i]).Data[0];
                rows[i] = _server.ReceiveArray(_activeClients[i]);
            }

            var mainElementIndex = FindMainElement(rows, iteration);

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i != mainElementIndex)
                {
                    _server.Send(1, _activeClients[i]);
                    _server.Send(vector[mainElementIndex], _activeClients[i]);
                    _server.Send(rows[mainElementIndex], _activeClients[i], ref index);
                    
                }
                else
                {
                    _server.Send(0, _activeClients[i], 0);
                }

                _server.Receive(_activeClients[i]);
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {

            for (var i = 0; i < _waitingClients.Count; i++)
            {
                _server.Send(0, _waitingClients[i]);

                var pathes = _server.ReceiveArray(_waitingClients[i]);

                var arrays = _server.ReceiveMatrix(pathes.Length, _waitingClients[i]);

                var vectorList = _server.ReceiveArray(_waitingClients[i]);

                for (var j = 0; j < pathes.Length; j++)
                {
                    matrix[(int)pathes[j]] = arrays[j];
                }

                for (var j = 0; j < vectorList.Length; j++)
                {
                    vector[(int)pathes[j]] = vectorList[j];
                }
            }

            _waitingClients = new List<IPEndPoint>();

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
