using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Solvers
{
    public class ParallelSolver
    {
        private readonly int _port;
        private readonly IPAddress _ipAddress;
        private readonly Socket _socket;
        private readonly List<IPEndPoint> _clients;

        public delegate void ClientConnection(int count, string address);
        public event ClientConnection Notify;

        public int ClientNum { get; private set; }

        public ParallelSolver(int port, string ipAddress)
        {
            _port = port;
            _ipAddress = IPAddress.Parse(ipAddress);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _clients = new List<IPEndPoint>();
        }

        public void StartServer()
        {
            Task listeningTask = new Task(Listen);
            listeningTask.Start();
        }

        private void Listen()
        {
            try
            {
                var localIp = new IPEndPoint(IPAddress.Any, _port);

                _socket.Bind(localIp);

                while (true)
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

                    for (int i = 0; i < _clients.Count; i++)
                    {
                        if (_clients[i].Address.ToString() == remoteFullIp.Address.ToString())
                        {
                            addClient = false;
                            break;
                        }
                    }

                    if (addClient == true)
                    {
                        _clients.Add(remoteFullIp);
                        Notify.Invoke(_clients.Count, remoteFullIp.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }


        public void Solve(string pathA, string pathB, string pathRes)
        {
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
