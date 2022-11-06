using System.Net;
using System.Net.Sockets;
using System.Text;

UdpClient sender = new UdpClient();
try
{
    while (true)
    {
        string message = Console.ReadLine();
        byte[] data = Encoding.Unicode.GetBytes(message);
        sender.Send(data, data.Length, "127.0.0.1", 8080); // отправка
        Console.WriteLine("Sended");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    sender.Close();
}
