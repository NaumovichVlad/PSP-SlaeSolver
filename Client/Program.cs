using Client;
using System.Net;
using System.Net.Sockets;
using System.Text;

var server = new GaussSlaeSolver(8080, "127.0.0.1");

if (server.Connect())
{
    Console.WriteLine("Connected to server");
}
else
{
    Console.WriteLine("Connection error");
}
