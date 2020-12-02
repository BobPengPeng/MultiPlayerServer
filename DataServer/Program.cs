using System;
using Grpc.Core;
using DataAndNetwork;

namespace DataServer
{
    class Program
    {
        static void Main(string[] args)
        {
            const int Port = 30052;
            Server server = new Server
            {
                Services = {NetWork.BindService(new MyServerImpl())},
                Ports = { new ServerPort("0.0.0.0", Port, ServerCredentials.Insecure)}
            };
            server.Start();

            Console.WriteLine("RouteGuide server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
            
            server.ShutdownAsync().Wait();
        }
    }
}
