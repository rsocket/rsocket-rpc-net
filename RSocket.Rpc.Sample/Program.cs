using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using RSocket;
using RSocket.RPC;
using RSocket.Transports;

namespace RSocketRPCSample
{
	using Google.Protobuf;
	using Google.Protobuf.WellKnownTypes;

	class Program
	{
		static async Task Main(string[] args)
		{
			//Create a new Client.
			var client = new RSocketClient(
				new WebSocketTransport("ws://localhost:9092/"));
			//	new SocketTransport("tcp://localhost:9091/"));

			//Bind a Service to this Client.
			var service = new EchoService.EchoServiceClient(client);

			//Connect to a Server and establish communications.
			await client.ConnectAsync();

			//Make a service method call with no return to the server.
			await service.FireAndForget(Value.ForString($"{nameof(EchoService.IEchoService.FireAndForget)}: Calling service..."));

			//Make a service method call returning a single value.
			var result = await service.RequestResponse(Value.ForString($"{nameof(EchoService.IEchoService.RequestResponse)}: Calling service..."));
			Console.WriteLine($"Sample Result: {result.StringValue}");


			var enumerator = service.RequestStream(Value.ForString($"{nameof(EchoService.IEchoService.RequestStream)}: Calling service...")).GetAsyncEnumerator();
			try
			{
				while (await enumerator.MoveNextAsync()) { Console.WriteLine($"Stream Result: {enumerator.Current.StringValue}"); }
				Console.WriteLine("Stream Done");
			}
			finally { await enumerator.DisposeAsync(); }


			//Wait for a keypress to end session.
			Console.WriteLine($"Press any key to continue...");
			Console.ReadKey();
		}
	}
}
