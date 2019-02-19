using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using RSocket;
using RSocket.RPC;
using RSocket.Transports;

namespace RSocketRPCSample
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Google.Protobuf;
	using Google.Protobuf.WellKnownTypes;
	using RSocket.Collections.Generic;

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
			await service.FireAndForget(Value.ForString($"{nameof(service.FireAndForget)}: Calling service..."));

			//Make a service method call returning a single value.
			var result = await service.RequestResponse(Value.ForString($"{nameof(service.RequestResponse)}: Calling service..."));
			Console.WriteLine($"Sample Result: {result.StringValue}");

			//Make a service call and asynchronously iterate the returning values.
			var stream = service.RequestStream(Value.ForString($"{nameof(service.RequestStream)}: Calling service..."));
			await ForEach(stream, value => Console.WriteLine($"Stream Result: {value.StringValue}"), () => Console.WriteLine("Stream Done"));


			//Make a service call taking asynchronous values and asynchronously iterate the returning values.
			var values = new AsyncValues<Google.Protobuf.WellKnownTypes.Value>(
				Task.FromResult(from value in new[] { Value.ForString($"Initial Value"), } select value),
				Task.FromResult(from value in new[] { 2, 3, 4 } select Value.ForString($"Value {value}")));
			//	await ForEach(values, value => Console.WriteLine($"Values Result: {value}"), () => Console.WriteLine("Values Done"));

			var channel = service.RequestChannel(values);
			await ForEach(channel, value => Console.WriteLine($"Channel Result: {value.StringValue}"), () => Console.WriteLine("Channel Done"));


			//Wait for a keypress to end session.
			Console.WriteLine($"Press any key to continue...");
			Console.ReadKey();
		}


		/// <summary>Helper method to show Asynchronous Enumeration in C# 7</summary>
		static public async Task ForEach<T>(RSocket.Collections.Generic.IAsyncEnumerable<T> enumerable, Action<T> action, Action final = default)
		{
			var enumerator = enumerable.GetAsyncEnumerator();
			try
			{
				while (await enumerator.MoveNextAsync()) { action(enumerator.Current); }
				final?.Invoke();
			}
			finally { await enumerator.DisposeAsync(); }
		}


		/// <summary>An asynchronously enumerable set of values.</summary>
		public class AsyncValues<T> : RSocket.Collections.Generic.IAsyncEnumerable<T>
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
	}
}
