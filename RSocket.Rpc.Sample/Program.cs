using System;
using System.Threading.Tasks;
using RSocket;
using RSocket.Transports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf.WellKnownTypes;
using System.Buffers;

namespace RSocketRPCSample
{

	class Program
	{
		static async Task Main(string[] args)
		{
			var transport = new LoopbackTransport();
			//	new WebSocketTransport("ws://localhost:9092/"));
			//	new SocketTransport("tcp://localhost:9091/")

			var client = new RSocketClient(transport);
			var service = new EchoService.EchoServiceClient(client);

			var server = new RSocketServer(transport.Beyond);
			var producer = new MyEchoServer(server);

			//TODO ASK: Should these be present in the generated methods to allow one-line construction?
			await server.ConnectAsync();
			await client.ConnectAsync();

			var streamresults = await service.RequestStream(Value.ForString("Test Request"))
					.ToListAsync();     //Collect all of the results. In C#8, this can be an async foreach - nice!
			streamresults.ForEach(result => Console.WriteLine($"{nameof(service.RequestStream)}() => {result.StringValue}"));


			//Wait for a keypress to end session.
			{ Console.WriteLine($"Press any key to continue..."); Console.ReadKey(); }
		}


		//Implementors have two choices, implement IEchoService directly or override the provided abstract base class - shown below:
		class MyEchoServer : EchoService.EchoServiceServer
		{
			public MyEchoServer(RSocket.RSocket socket) : base(socket) { }

			public override IAsyncEnumerable<Value> RequestStream(Value message, ReadOnlySequence<byte> metadata)
			{
				return Yield().ToAsyncEnumerable();

				IEnumerable<Value> Yield()		//In C#8.0, use an async generator.
				{
					yield return Value.ForString(message.StringValue + "and Result 1");
					yield return Value.ForString(message.StringValue + "and Result 2");
					yield return Value.ForString(message.StringValue + "and Result 3");
				}
			}
		}



		static async Task Main1(string[] args)
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
			await service.FireAndForget(Value.ForString($"{nameof(service.FireAndForget)}: Calling service..."));

			//Make a service method call returning a single value.
			var result = await service.RequestResponse(Value.ForString($"{nameof(service.RequestResponse)}: Calling service..."));
			Console.WriteLine($"Sample Result: {result.StringValue}");


			//C# 8.0: Change the target language version to see IAsyncEnumerable iteration using built-in language constructs. (Project -> Properties -> Build -> Advanced -> Language Version)
#if CSHARP8
			//Make a service call and asynchronously iterate the returning values.
			var stream = service.RequestStream(Value.ForString($"{nameof(service.RequestStream)}: Calling service..."));
			await foreach (var value in stream)
			{
				Console.WriteLine($"Stream Result: {value.StringValue}");
			}
			Console.WriteLine("Stream Done");


			//Make a service call taking asynchronous values and asynchronously iterate the returning values.
			var channel = service.RequestChannel(GenerateValues());
			await foreach (var value in channel)
			{
				Console.WriteLine($"Channel Result: {value.StringValue}");
			}
			Console.WriteLine("Channel Done");

#else
			//Make a service call and asynchronously iterate the returning values.
			var stream = service.RequestStream(Value.ForString($"{nameof(service.RequestStream)}: Calling service..."));
			await ForEach(stream, value => Console.WriteLine($"Stream Result: {value.StringValue}"), () => Console.WriteLine("Stream Done"));


			//Make a service call taking asynchronous values and asynchronously iterate the returning values.
			var channel = service.RequestChannel(GenerateValues());
			await ForEach(channel, value => Console.WriteLine($"Channel Result: {value.StringValue}"), () => Console.WriteLine("Channel Done"));

			//Wait for a keypress to end session.
			{ Console.WriteLine($"Press any key to continue..."); Console.ReadKey(); }
#endif
		}


		//.Net Core 3.0: Change the Framework Version to include the genuine IAsyncEnumerable Interface. (Project -> Properties -> Application -> Target Framework)
#if NETCOREAPP3_0

		//Use the state machine compiler to create an awaitable generator.
		static async IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> GenerateValues()
		{
			yield return Value.ForString($"Initial Value");
			await Task.Delay(100);
			foreach (var value in from value in new[] { 2, 3, 4 } select Value.ForString($"Value {value}"))
			{ yield return value; }
		}
#else

		//Use a helper class create an awaitable generator.
		static IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> GenerateValues()
		{
			return new AsyncValues<Google.Protobuf.WellKnownTypes.Value>(
				Task.FromResult(from value in new[] { Value.ForString($"Initial Value"), } select value),
				Task.FromResult(from value in new[] { 2, 3, 4 } select Value.ForString($"Value {value}")));
		}

		/// <summary>An asynchronously enumerable set of values.</summary>
		public class AsyncValues<T> : IAsyncEnumerable<T>
		{
			readonly IEnumerable<Task<IEnumerable<T>>> From;

			public AsyncValues(params Task<IEnumerable<T>>[] from) { From = from; }
			public AsyncValues(IEnumerable<Task<IEnumerable<T>>> from) { From = from; }

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Enumerator(From);

			private class Enumerator : IAsyncEnumerator<T>
			{
				readonly IEnumerator<Task<IEnumerable<T>>> under;
				IEnumerator<T> into;
				public T Current => into.Current;

				public Enumerator(IEnumerable<Task<IEnumerable<T>>> from) { under = from.GetEnumerator(); }

				public async ValueTask<bool> MoveNextAsync()
				{
					while (true)
					{
						if (into == default)
						{
							if (!under.MoveNext()) { return false; }
							into = (await under.Current).GetEnumerator();
						}
						if (into.MoveNext()) { return true; } else { into = default; }
					}
				}

				public ValueTask DisposeAsync() => new ValueTask();
			}
		}
#endif

		/// <summary>Helper method to show Asynchronous Enumeration in C#7. This is not needed in C#8</summary>
		static public async Task ForEach<T>(IAsyncEnumerable<T> enumerable, Action<T> action, Action final = default)
		{
			var enumerator = enumerable.GetAsyncEnumerator();
			try
			{
				while (await enumerator.MoveNextAsync()) { action(enumerator.Current); }
				final?.Invoke();
			}
			finally { await enumerator.DisposeAsync(); }
		}
	}
}
