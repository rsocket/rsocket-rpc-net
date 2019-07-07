using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RSocket.RPC
{
	public interface IRSocketService
	{
		string ServiceName { get; }
		IAsyncEnumerable<ReadOnlySequence<byte>> Dispatch(ReadOnlySequence<byte> data, string method, ReadOnlySequence<byte> tracing, ReadOnlySequence<byte> metadata, IAsyncEnumerable<ReadOnlySequence<byte>> messages);
	}

	public abstract partial class RSocketService
	{
		private readonly RSocket Socket;

		public RSocketService(RSocket socket) { Socket = socket; }


		protected Task __RequestFireAndForget<TMessage>(TMessage message, Func<TMessage, byte[]> messagemapper, 
			ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestFireAndForget(new ReadOnlySequence<byte>(messagemapper(message)), metadata, tracing, service: service, method: method);

		protected Task __RequestFireAndForget<TMessage>(TMessage message, Func<TMessage, ReadOnlySequence<byte>> messagemapper, 
			ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestFireAndForget(messagemapper(message), metadata, tracing, service: service, method: method);

		private protected Task __RequestFireAndForget(
			ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> Socket.RequestFireAndForget(data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));


		protected Task<TResult> __RequestResponse<TMessage, TResult>(TMessage message, Func<TMessage, byte[]> messagemapper,
			Func<byte[], TResult> resultmapper,
			ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default) 
			=> __RequestResponse(_ => resultmapper(_.data.ToArray()), new ReadOnlySequence<byte>(messagemapper(message)), metadata, tracing, service: service, method: method);

		protected Task<TResult> __RequestResponse<TMessage, TResult>(TMessage message, Func<TMessage, ReadOnlySequence<byte>> messagemapper,
			Func<ReadOnlySequence<byte>, TResult> resultmapper,
			ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestResponse(_ => resultmapper(_.data), messagemapper(message), metadata, tracing, service: service, method: method);

		private protected Task<T> __RequestResponse<T>(
			Func<(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata), T> resultmapper,
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> Socket.RequestResponse(resultmapper, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));


		protected IAsyncEnumerable<TResult> __RequestStream<TMessage, TResult>(TMessage message,
			Func<TMessage, byte[]> sourcemapper, Func<byte[], TResult> resultmapper, 
			ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestStream(resultmapper: value => resultmapper(value.data.ToArray()), new ReadOnlySequence<byte>(sourcemapper(message)), metadata, tracing, service: service, method: method);

		protected IAsyncEnumerable<TResult> __RequestStream<TMessage, TResult>(TMessage message,
			Func<TMessage, ReadOnlySequence<byte>> sourcemapper, Func<ReadOnlySequence<byte>, TResult> resultmapper,
			ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestStream(result => resultmapper(result.data), sourcemapper(message), metadata, tracing, service: service, method: method);

		private protected IAsyncEnumerable<T> __RequestStream<T>(
			Func<(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata), T> resultmapper,
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default,
			ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		=> Socket.RequestStream(resultmapper, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));


		protected IAsyncEnumerable<TResult> __RequestChannel<TMessage, TResult>(IAsyncEnumerable<TMessage> messages, 
			Func<TMessage, byte[]> sourcemapper, Func<byte[], TResult> resultmapper, 
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestChannel<TMessage, TResult>(messages, message => new ReadOnlySequence<byte>(sourcemapper(message)), result => resultmapper(result.data.ToArray()), data, metadata, tracing, service: service, method: method);

		protected IAsyncEnumerable<TResult> __RequestChannel<TMessage, TResult>(IAsyncEnumerable<TMessage> messages,
			Func<TMessage, ReadOnlySequence<byte>> sourcemapper, Func<ReadOnlySequence<byte>, TResult> resultmapper,
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> __RequestChannel<TMessage, TResult>(messages, message => sourcemapper(message), result => resultmapper(result.data), data, metadata, tracing, service: service, method: method);

		private protected IAsyncEnumerable<T> __RequestChannel<TMessage, T>(IAsyncEnumerable<TMessage> source,
			Func<TMessage, ReadOnlySequence<byte>> sourcemapper, Func<(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata), T> resultmapper,
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> Socket.RequestChannel<TMessage, T>(source, sourcemapper, resultmapper, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));


		// ****** Producer-side dispatching ***** //

		//TODO! Non-static, per-socket

		static System.Collections.Concurrent.ConcurrentDictionary<string, IRSocketService> Services = new System.Collections.Concurrent.ConcurrentDictionary<string, IRSocketService>();

		static public void Register(RSocket socket, IRSocketService service)
		{
			Services[service.ServiceName] = service;

			//TODO Need to ensure that this really only happens once per Socket.

			socket.Respond(message => (RPC: new RSocketService.RemoteProcedureCallMetadata(message.Metadata), message.Data),
				request => Dispatch(request.Data, request.RPC.Service, request.RPC.Method, request.RPC.Tracing, request.RPC.Metadata),
				result => (Data: result, Metadata: default));

			//TODO This looks data/metadata backwards?
			socket.Stream(message => (RPC: new RSocketService.RemoteProcedureCallMetadata(message.Metadata), message.Data),
				request => Dispatch(request.Data, request.RPC.Service, request.RPC.Method, request.RPC.Tracing, request.RPC.Metadata),
				result => (Data: result, Metadata: default));

			socket.Channel((request, messages) => Dispatch(request.Data, request.RPC.Service, request.RPC.Method, request.RPC.Tracing, request.RPC.Metadata, messages.ToAsyncEnumerable()),
				message => (RPC: new RSocketService.RemoteProcedureCallMetadata(message.Metadata), message.Data),
				incoming => incoming.Data,
				result => (Data: result, Metadata: default));
		}

		static IAsyncEnumerable<ReadOnlySequence<byte>> Dispatch(ReadOnlySequence<byte> data, string service, string method, ReadOnlySequence<byte> tracing, ReadOnlySequence<byte> metadata, IAsyncEnumerable<ReadOnlySequence<byte>> messages = default)
			=> (Services.TryGetValue(service, out var target)) ? target.Dispatch(data, method, tracing, metadata, messages ?? AsyncEnumerable.Empty<ReadOnlySequence<byte>>()) : throw new InvalidOperationException();      //TODO Handle invalid service name request.
	}

	static public class RSocketServiceExtensions
	{
		////TODO Java style
		//socket.AddService(new MyEchoServiceServer());

		static public void AddService(this RSocketServer socket, IRSocketService service) => RSocketService.Register(socket, service);
	}
}