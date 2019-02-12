using System;
using System.Buffers;
using System.Threading.Tasks;
using RSocket;
using RSocket.RPC;
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
