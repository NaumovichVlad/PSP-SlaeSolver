using Core.Sockets.Udp;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core.Sockets.Queue
{
    public class SentPackagesQueue
    {
        private readonly PriorityQueue<Package, int> _mainQueue;
        private readonly PriorityQueue<Package, int> _secondaryQueue;

        public SentPackagesQueue()
        {
            _mainQueue = new PriorityQueue<Package, int>();
            _secondaryQueue = new PriorityQueue<Package, int>();
        }

        public Package ReceivePackage(UdpClient socket, IPEndPoint expectedRecieveAddress, ref int packageIndex)
        {
            Package? package = GetPackage(packageIndex);

            if (package == null)
            {
                do
                {
                    IPEndPoint receiveAddress = null;
                    var data = socket.Receive(ref receiveAddress);

                    package = JsonConvert.DeserializeObject<Package>(Encoding.ASCII.GetString(data));

                    if (package.Id == packageIndex && expectedRecieveAddress.Port == receiveAddress.Port)
                    {
                        packageIndex++;

                        return package;
                    }
                    else
                    {
                        _mainQueue.Enqueue(package, 0);
                    }
                } while (true);
            }
            else
            {
                packageIndex++;

                return package;
            }
        }

        private Package GetPackage(int packageIndex)
        {
            Package? package = null;
            var priority = 0;

            while (_mainQueue.Count > 0)
            {
                _mainQueue.TryDequeue(out package, out priority);

                if (package.Id == packageIndex)
                {
                    return package;
                }
                else
                {
                    _secondaryQueue.Enqueue(package, priority--);
                }
            }

            _mainQueue.EnqueueRange(_secondaryQueue.UnorderedItems);

            return null;
        }
    }
}
