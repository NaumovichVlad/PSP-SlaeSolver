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

        public ParallelSolver(IFileManager fileManager, int waitClientsNum, ITimeLogger timeLogger)
        {
            _server = new SafeUdpSocket();
            _activeClients = new List<IPEndPoint>();
            _fileManager = fileManager;
            _listeningTask = new Thread(Listen);
            _waitingClients = new List<IPEndPoint>();
            _waitClientsNum = waitClientsNum;
            _timeLogger = timeLogger;
        }

        public void StartServer(int port, string ipAddress)
        {
            _server.Create(ipAddress, port);
            _listeningTask.Start();
        }

        private void Listen()
        {
            try
            {
                while (_activeClients.Count < _waitClientsNum)
                {
                    Statuses status;
                    var data = new byte[256];
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    if (_server.TryAccept(ref remoteIp))
                    {
                        IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                        bool addClient = true;

                        for (int i = 0; i < _activeClients.Count; i++)
                        {
                            if (_activeClients[i].Address.ToString() == remoteFullIp.Address.ToString())
                            {
                                addClient = false;
                                break;
                            }
                        }

                        if (addClient == true)
                        {
                            _activeClients.Add(remoteFullIp);
                            _server.SendStatus(Statuses.OK, remoteFullIp);
                            Notify.Invoke(_activeClients.Count, remoteFullIp.ToString());
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

                _server.SendInt(rowsCount, _activeClients[i]);
                _server.SendInt(vector.Length, _activeClients[i]);


                for (var j = i; j < vector.Length; j += _activeClients.Count)
                {

                    _server.SendArray(matrix[j], _activeClients[i]);

                    vectorRequest[iterator] = vector[j];
                    iterator++;
                }

                _server.SendArray(vectorRequest, _activeClients[i]);

            });
        }

        private void GetForwardPhase(int iteration, int matrixSize)
        {
            for (var i = 0; i < _activeClients.Count; i++)
            {
                _server.SendStatus(Statuses.OK, _activeClients[i]);

                if (_server.ReceiveStatus(_activeClients[i]) == Statuses.OK)
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
                _server.SendStatus(Statuses.OK, _activeClients[i]);

                rows[i] = _server.ReceiveArray(matrixSize, _activeClients[i]);
                vector[i] = _server.ReceiveDouble(_activeClients[i]);
            }

            var mainElementIndex = FindMainElement(rows, iteration);

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i != mainElementIndex)
                {
                    _server.SendStatus(Statuses.NO, _activeClients[i]);

                    _server.SendArray(rows[mainElementIndex], _activeClients[i]);

                    _server.SendDouble(vector[mainElementIndex], _activeClients[i]);

                }
                else
                {
                    _server.SendStatus(Statuses.OK, _activeClients[i]);
                }
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {

            for (var i = 0; i < _waitingClients.Count; i++)
            {
                _server.SendStatus(Statuses.OK, _waitingClients[i]);

                var rowsCount = _server.ReceiveInt(_waitingClients[i]);

                var pathes = _server.ReceiveArray(rowsCount, _waitingClients[i]);

                for (var j = 0; j < rowsCount; j++)
                {
                    matrix[(int)pathes[j]] = _server.ReceiveArray(matrix.Length, _waitingClients[i]);
                }

                var vectorList = _server.ReceiveArray(rowsCount, _waitingClients[i]);

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
