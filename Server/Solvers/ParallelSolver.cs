using Core.Methods.Linear;
using Core.Methods.Parallel;
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
        private readonly int _port;
        private readonly IPAddress _ipAddress;
        private readonly Socket _server;
        private readonly List<IPEndPoint> _activeClients;
        private readonly List<IPEndPoint> _waitingClients;
        private readonly int _waitClientsNum;

        private readonly IFileManager _fileManager;
        private readonly ITimeLogger _timeLogger;
        private readonly Thread _listeningTask;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress, IFileManager fileManager, int waitClientsNum, ITimeLogger timeLogger)
        {
            _port = port;
            _ipAddress = IPAddress.Parse(ipAddress);
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
                var localIp = new IPEndPoint(_ipAddress, _port);

                _server.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _server.Bind(localIp);

                while (_activeClients.Count < _waitClientsNum)
                {
                    var builder = new StringBuilder();
                    var bytes = 0;
                    var data = new byte[256];
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = _server.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (_server.Available > 0);

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
                        Notify.Invoke(_activeClients.Count, remoteFullIp.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Close();
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

                SendIntRequest(rowsCount, _activeClients[i]);
                SendIntRequest(vector.Length, _activeClients[i]);


                for (var j = i; j < vector.Length; j += _activeClients.Count)
                {

                    SendArrayRequestSafe(matrix[j], _activeClients[i]);

                    vectorRequest[iterator] = vector[j];
                    iterator++;
                }

                SendArrayRequestSafe(vectorRequest, _activeClients[i]);

            });
        }

        private void GetForwardPhase(int iteration, int matrixSize)
        {
            for (var i = 0; i < _activeClients.Count; i++)
            {
                SendIntRequest(0, _activeClients[i]);

                if (GetIntResponce(_activeClients[i]) == 0)
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
                SendIntRequest(0, _activeClients[i]);

                rows[i] = GetArrayResponceSafe(matrixSize, _activeClients[i]);
                vector[i] = GetDoubleResponce(_activeClients[i]);
            }

            var mainElementIndex = FindMainElement(rows, iteration);

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i != mainElementIndex)
                {
                    SendIntRequest(1, _activeClients[i]);

                    SendArrayRequestSafe(rows[mainElementIndex], _activeClients[i]);

                    SendDoubleRequest(vector[mainElementIndex], _activeClients[i]);

                }
                else
                {
                    SendIntRequest(0, _activeClients[i]);
                }
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {

            for (var i = 0; i < _waitingClients.Count; i++)
            {
                SendIntRequest(0, _waitingClients[i]);

                var rowsCount = GetIntResponce(_waitingClients[i]);

                var pathes = GetArrayResponceSafe(rowsCount, _waitingClients[i]);

                for (var j = 0; j < rowsCount; j++)
                {
                    matrix[(int)pathes[j]] = GetArrayResponceSafe(matrix.Length, _waitingClients[i]);
                }

                var vectorList = GetArrayResponceSafe(rowsCount, _waitingClients[i]);

                for (var j = 0; j < vectorList.Length; j++)
                {
                    vector[(int)pathes[j]] = vectorList[j];
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

        public int GetIntResponce(EndPoint client)
        {
            var data = new byte[sizeof(int)];

            _server.ReceiveFrom(data, ref client);

            return BitConverter.ToInt32(data);
        }

        public double GetDoubleResponce(EndPoint client)
        {
            var data = new byte[sizeof(double)];

            _server.ReceiveFrom(data, ref client);

            return BitConverter.ToDouble(data);
        }

        public double[][] GetMatrixResponceSafe(int rowsCount, int rowSize, EndPoint client)
        {
            var rows = new double[rowsCount][];

            for (var i = 0; i < rowsCount; i++)
            {
                rows[i] = GetArrayResponceSafe(rowSize, client);
            }

            return rows;
        }

        public double[] GetArrayResponceSafe(int arraySize, EndPoint client)
        {
            var data = new byte[sizeof(double) * arraySize];
            var array = new double[arraySize];

            while (_server.ReceiveFrom(data, ref client) != sizeof(double) * arraySize)
            {
                SendIntRequest(1, client);
            }

            SendIntRequest(0, client);

            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            return array;
        }

        public void SendArrayRequestSafe(double[] array, EndPoint client)
        {
            var data = new byte[array.Length * sizeof(double)];

            Buffer.BlockCopy(array, 0, data, 0, data.Length);

            do
            {
                _server.SendTo(data, client);

            } while (GetIntResponce(client) != 0);
        }

        public void SendDoubleRequest(double number, EndPoint client)
        {
            _server.SendTo(BitConverter.GetBytes(number), client);
        }

        public void SendIntRequest(int number, EndPoint client)
        {
            _server.SendTo(BitConverter.GetBytes(number), client);
        }

        private void Close()
        {
            if (_server != null)
            {
                _server.Shutdown(SocketShutdown.Both);
                _server.Close();
            }
        }

        public string GetTimeLog()
        {
            return _timeLogger.GetLog();
        }
    }
}
