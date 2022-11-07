using Core.Methods.Linear;
using Core.Methods.Parallel;
using DataAccess.Managers;
using System;
using System.Collections.Generic;
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
        private readonly Socket _socket;
        private readonly List<IPEndPoint> _activeClients;
        private readonly List<IPEndPoint> _waitingClients;
        private readonly int _waitClientsNum;

        private readonly IFileManager _fileManager;
        private readonly Thread _listeningTask;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress, IFileManager fileManager, int waitClientsNum)
        {
            _port = port;
            _ipAddress = IPAddress.Parse(ipAddress);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _activeClients = new List<IPEndPoint>();
            _fileManager = fileManager;
            _listeningTask = new Thread(Listen);
            _waitingClients = new List<IPEndPoint>();
            _waitClientsNum = waitClientsNum;
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

                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _socket.Bind(localIp);

                while (_activeClients.Count < _waitClientsNum)
                {
                    var builder = new StringBuilder();
                    var bytes = 0;
                    var data = new byte[256];
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = _socket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (_socket.Available > 0);

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
            var matrix = _fileManager.ReadMatrix(pathA);
            var vector = _fileManager.ReadVector(pathB);
            
            SendRows(matrix, vector);

            for (var i = 0; i < matrix.Length - 1; i++)
            {
                GetForwardPhase(i, matrix.Length);
            }

             return GetBackPhase(matrix, vector);
        }

        private void SendRows(double[][] matrix, double[] vector)
        {
            Parallel.For(0, _activeClients.Count, i =>
            {
                var rowsCount = CalculateRowsCountForClient(matrix.Length, _activeClients.Count, i);
                var request = BitConverter.GetBytes(rowsCount);
                var vectorRequest = new double[rowsCount];
                var iterator = 0;

                _socket.SendTo(request, _activeClients[i]);

                request = BitConverter.GetBytes(vector.Length);

                _socket.SendTo(request, _activeClients[i]);

                for (var j = i; j < vector.Length; j += _activeClients.Count)
                {
                    byte[] matrixMessage = new byte[matrix[j].Length * sizeof(double)];

                    Buffer.BlockCopy(matrix[j], 0, matrixMessage, 0, matrixMessage.Length);

                    _socket.SendTo(matrixMessage, _activeClients[i]);

                    if (GetIntResponce(_activeClients[i]) != 0)
                        throw new Exception();

                    vectorRequest[iterator] = vector[j];
                    iterator++;
                }

                byte[] vectorMessage = new byte[vectorRequest.Length * sizeof(double)];

                Buffer.BlockCopy(vectorRequest, 0, vectorMessage, 0, vectorMessage.Length);
                _socket.SendTo(vectorMessage, _activeClients[i]);

            });
        }

        private void GetForwardPhase(int iteration, int matrixSize)
        {
            

            var rows = new double[_activeClients.Count][];
            var vector = new double[_activeClients.Count];

            for (var i = 0; i < _activeClients.Count; i++)
            {
                var compliteStatusMessage = new byte[sizeof(int)];

                _socket.Receive(compliteStatusMessage);

                if (BitConverter.ToInt32(compliteStatusMessage) == 1)
                {
                    _waitingClients.Add(_activeClients[i]);
                    _activeClients.RemoveAt(i);

                    i--;

                    continue;
                }

                var data = new byte[matrixSize * sizeof(double)];
                var b = new byte[sizeof(double)];
                var array = new double[matrixSize];

                _socket.Receive(b);

                _socket.Receive(data);

                Buffer.BlockCopy(data, 0, array, 0, data.Length);

                rows[i] = array;
                vector[i] = BitConverter.ToDouble(b);
            }

            if (_activeClients.Count == 0)
            {
                return;
            }

            var mainElementIndex = FindMainElement(rows, iteration);

            for (var i = 0; i < _activeClients.Count; i++)
            {
                if (i != mainElementIndex)
                {
                    _socket.SendTo(BitConverter.GetBytes(1), _activeClients[i]);

                    byte[] matrixMessage = new byte[rows[mainElementIndex].Length * sizeof(double)];

                    Buffer.BlockCopy(rows[mainElementIndex], 0, matrixMessage, 0, matrixMessage.Length);

                    _socket.SendTo(matrixMessage, _activeClients[i]);

                    _socket.SendTo(BitConverter.GetBytes(vector[mainElementIndex]), _activeClients[i]);

                }
                else
                {
                    _socket.SendTo(BitConverter.GetBytes(0), _activeClients[i]);
                }
            }
        }

        private double[] GetBackPhase(double[][] matrix, double[] vector)
        {
            var request = BitConverter.GetBytes(0);
            var vectorList = new List<double>();
            var counter = 0;

            for (var i = 0; i < _waitingClients.Count; i++)
            {
                _socket.SendTo(request, _waitingClients[i]);
                var rowsCount = GetIntResponce(_waitingClients[i]);

                for (var j = 0; j < rowsCount; j++)
                {
                    matrix[counter] = GetArrayResponce(matrix.Length);
                    counter++;
                }

                vectorList.AddRange(GetArrayResponce(rowsCount));
            }

            return ExecuteBackPhaseIteration(matrix, vectorList.ToArray());
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

        private double[][] GetRows(int rowsCount, int rowSize)
        {
            var rows = new double[rowsCount][];

            for (var i = 0; i < rowsCount; i++)
            {
                rows[i] = GetArrayResponce(rowSize);
            }

            return rows;
        }

        private double[] GetArrayResponce(int rowSize)
        {
            byte[] data = new byte[rowSize * sizeof(double)];
            _socket.Receive(data);
            var array = new double[data.Length / sizeof(double)];

            Buffer.BlockCopy(data, 0, array, 0, data.Length);

            return array;
        }

        private int GetIntResponce(EndPoint client)
        {
            byte[] data = new byte[sizeof(int)];

            _socket.ReceiveFrom(data, ref client);

            return BitConverter.ToInt32(data);
        }

        private void Close()
        {
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }
    }
}
