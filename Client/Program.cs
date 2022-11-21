using Client;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.Write("Введите Ip сервера: ");
var server = new GaussSlaeSolver(8080, Console.ReadLine());

server.Process();
Console.ReadLine();
