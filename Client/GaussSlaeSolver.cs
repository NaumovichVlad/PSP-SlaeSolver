using Core.Methods.Parallel;
using Core.Sockets.Udp;
using System.Net;

namespace Client
{
    public class GaussSlaeSolver : GaussMethodSolverParallel
    {
        private readonly UdpSocket _client;

        private int _receivePackageIndex;
        private int _sendPackageIndex;
        private IPEndPoint _serverIEP;

        public GaussSlaeSolver(int port, string ip)
        {
            _client = new UdpSocket(ip, port);
            _receivePackageIndex = 0;
            _sendPackageIndex = 0;
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

                    _client.Send(0, _serverIEP, 0);

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

        public bool Process()
        {
            try
            {
                var sizes = _client.ReceiveArray(_serverIEP, ref _receivePackageIndex, ref _sendPackageIndex);
                var rowsCount = (int)sizes[0];

                Console.WriteLine($"Expected rows count: {rowsCount}");

                var matrixSize = (int)sizes[1];

                var rows = _client.ReceiveMatrix(rowsCount, _serverIEP, ref _receivePackageIndex, ref _sendPackageIndex);

                Console.WriteLine($"Number of rows received: {rows.Length}");

                var vector = _client.ReceiveArray(_serverIEP, ref _receivePackageIndex, ref _sendPackageIndex);
                var rowsComplited = 0;

                Console.WriteLine($"Number of vector elements received: {vector.Length}");

                Console.WriteLine("\nForward phase started");

                for (var i = 0; i <= matrixSize; i++)
                {
                    if (i == matrixSize)
                    {
                        Console.WriteLine("\nWaiting for the start of sending results...");

                        _client.Receive(_serverIEP, ref _receivePackageIndex);

                        Console.WriteLine("\nSending started");

                        _client.Send(rowsCount, _serverIEP, _sendPackageIndex++);

                        _client.Receive(_serverIEP, ref _receivePackageIndex);

                        for (var j = 0; j < rowsCount; j++)
                        {
                            _client.Send(rows[j], _serverIEP, ref _sendPackageIndex, ref _receivePackageIndex);
                        }

                        _client.Receive(_serverIEP, ref _receivePackageIndex);
                        _client.Send(vector, _serverIEP, ref _sendPackageIndex, ref _receivePackageIndex);

                        Console.WriteLine("\nForward phase complited");

                        return 1 == _client.Receive(_serverIEP, ref _receivePackageIndex).Data[0];
                    }
                    if (_client.Receive(_serverIEP, ref _receivePackageIndex).Data[0] == 0)
                    {
                        _client.Send(rows[rowsComplited], _serverIEP, ref _sendPackageIndex, ref _receivePackageIndex);
                        _client.Send(vector[rowsComplited], _serverIEP, _sendPackageIndex++);

                        rowsComplited++;

                        ExecuteForwardPhaseIteration(rows, rows[rowsComplited - 1], vector, vector[rowsComplited - 1], i, rowsComplited);
                    }
                    else
                    {
                        var mainVector = _client.Receive(_serverIEP, ref _receivePackageIndex).Data[0];
                        _client.Send(0, _serverIEP, _sendPackageIndex++);
                        var mainRow = _client.ReceiveArray(_serverIEP, ref _receivePackageIndex, ref _sendPackageIndex);

                        if (rowsComplited != rowsCount)
                        {
                            ExecuteForwardPhaseIteration(rows, mainRow, vector, mainVector, i, rowsComplited);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }
    }
}
