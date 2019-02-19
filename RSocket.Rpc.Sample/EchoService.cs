using System;
using System.Buffers;
using System.Threading.Tasks;
using RSocket;
using RSocket.RPC;
using System.Threading;
#if NETCOREAPP3_0
using System.Collections.Generic;
#else
using RSocket.Collections.Generic;
#endif

namespace RSocketRPCSample
{
	[System.Runtime.CompilerServices.CompilerGenerated]
	public static class EchoService
	{
		public const string Service = "io.rsocket.rpc.echo.EchoService";
		public const string Method_fireAndForget = "fireAndForget";
		public const string Method_requestResponse = "requestResponse";
		public const string Method_requestStream = "requestStream";
		public const string Method_requestChannel = "requestChannel";

		[System.Runtime.CompilerServices.CompilerGenerated]
		public interface IEchoService
		{
			Task FireAndForget(Google.Protobuf.WellKnownTypes.Value message, ReadOnlySequence<byte> metadata);

			Task<Google.Protobuf.WellKnownTypes.Value> RequestResponse(Google.Protobuf.WellKnownTypes.Value message,
				ReadOnlySequence<byte> metadata);

			IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> RequestStream(
				Google.Protobuf.WellKnownTypes.Value message, ReadOnlySequence<byte> metadata);

			IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> RequestChannel(
				IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> messages, ReadOnlySequence<byte> metadata);
		}

		[System.Runtime.CompilerServices.CompilerGenerated]
		public class EchoServiceClient : RSocketService, IEchoService
		{
			public EchoServiceClient(RSocketClient client) : base(client)
			{
			}

			public Task FireAndForget(Google.Protobuf.WellKnownTypes.Value message,
				ReadOnlySequence<byte> metadata = default) =>
				__RequestFireAndForget(message, Google.Protobuf.MessageExtensions.ToByteArray, metadata,
					service: EchoService.Service, method: EchoService.Method_fireAndForget);

			public Task<Google.Protobuf.WellKnownTypes.Value> RequestResponse(
				Google.Protobuf.WellKnownTypes.Value message, ReadOnlySequence<byte> metadata = default) =>
				__RequestResponse(message, Google.Protobuf.MessageExtensions.ToByteArray,
					Google.Protobuf.WellKnownTypes.Value.Parser.ParseFrom, metadata, service: EchoService.Service,
					method: EchoService.Method_requestResponse);

			public IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> RequestStream(
				Google.Protobuf.WellKnownTypes.Value message, ReadOnlySequence<byte> metadata = default) =>
				__RequestStream(message, Google.Protobuf.MessageExtensions.ToByteArray,
					Google.Protobuf.WellKnownTypes.Value.Parser.ParseFrom, metadata, service: EchoService.Service,
					method: EchoService.Method_requestStream);

			public IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> RequestChannel(
				IAsyncEnumerable<Google.Protobuf.WellKnownTypes.Value> messages,
				ReadOnlySequence<byte> metadata = default) =>
				__RequestChannel(messages, Google.Protobuf.MessageExtensions.ToByteArray,
					Google.Protobuf.WellKnownTypes.Value.Parser.ParseFrom, metadata, service: EchoService.Service,
					method: EchoService.Method_requestChannel);


			#region Adapters to allow compilation in .NET Core 3.0 to call .NET Standard 2.0 Library using proper IAsyncEnumerable
#if NETCOREAPP3_0
			private async new IAsyncEnumerable<TResult> __RequestStream<TMessage, TResult>(TMessage message, Func<TMessage, byte[]> sourcemapper, Func<byte[], TResult> resultmapper, ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> tracing = default, string service = default, string method = default)
			{ await foreach (var _ in base.__RequestStream(message, sourcemapper, resultmapper, metadata, tracing, service, method)) { yield return _; } }

			private async IAsyncEnumerable<TResult> __RequestChannel<TMessage, TResult>(IAsyncEnumerable<TMessage> messages, Func<TMessage, byte[]> sourcemapper, Func<byte[], TResult> resultmapper, ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, string method = default)
			{ await foreach (var _ in base.__RequestChannel(new AsyncEnumerable<TMessage>(messages), sourcemapper, resultmapper, data, metadata, tracing, service, method)) { yield return _; } }

			/// <summary>Interface forwarding from RSocket...IAsyncEnumerable to IAsyncEnumerable</summary>
			private class AsyncEnumerable<T> : RSocket.Collections.Generic.IAsyncEnumerable<T>
			{
				readonly IAsyncEnumerable<T> Source;
				public AsyncEnumerable(IAsyncEnumerable<T> source) { Source = source; }
				public RSocket.Collections.Generic.IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Enumerator(Source.GetAsyncEnumerator(cancellationToken));
				private class Enumerator : RSocket.Collections.Generic.IAsyncEnumerator<T>
				{
					readonly IAsyncEnumerator<T> Source;
					public Enumerator(IAsyncEnumerator<T> source) { Source = source; }
					public T Current => Source.Current;
					public ValueTask DisposeAsync() => Source.DisposeAsync();
					public ValueTask<bool> MoveNextAsync() => Source.MoveNextAsync();
				}
			}

#endif
			#endregion
		}

		static internal class EchoServiceExtensions
		{


		}

		public class EchoServiceServer : RSocketServer
		{
			public EchoServiceServer(IRSocketServerTransport transport) : base(transport)
			{
			}

			public override void RequestStream(in RSocketProtocol.RequestStream message,
				ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)
			{
			}

			public override void RequestResponse(in RSocketProtocol.RequestResponse message,
				ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)
			{
				var rpc = new RSocketService.RemoteProcedureCallMetadata(metadata);
				new RSocketProtocol.Payload(message.Stream, data, metadata, complete: true).Write(Transport.Output,
					data, metadata);
				Transport.Output.FlushAsync();
			}

			public override void RequestFireAndForget(in RSocketProtocol.RequestFireAndForget message,
				ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)
			{
			}

			public override void RequestChannel(in RSocketProtocol.RequestChannel message,
				ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)
			{
			}
		}
	}
}
