using Client;

Console.Write("Введите адресс счётного узла: ");

var address = Console.ReadLine().Split(':');

var server = new GaussSlaeSolver(int.Parse(address[1]), address[0]);

Console.WriteLine("Waiting connection...");

if (server.Listen())
{
    Console.WriteLine("Connected");
    while (server.Process()) ;
}
else
{
    Console.WriteLine("Connection error!");
}

Console.ReadLine();
