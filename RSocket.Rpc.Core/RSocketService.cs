using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RSocket.RPC
{
	public abstract partial class RSocketService
	{
		private readonly RSocketClient Client;
		private RSocket Socket => Client;

		public RSocketService(RSocketClient client) { Client = client; }


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
			Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> resultmapper,
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
			Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> resultmapper,
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
			Func<TMessage, ReadOnlySequence<byte>> sourcemapper, Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> resultmapper,
			ReadOnlySequence<byte> data = default, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
			=> Socket.RequestChannel<TMessage, T>(source, sourcemapper, resultmapper, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));
	}
}