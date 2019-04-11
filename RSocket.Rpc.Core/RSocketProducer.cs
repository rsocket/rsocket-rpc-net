using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RSocket.RPC
{
	public interface IRSocketNamedService
	{
		string ServiceName { get; }
	}

	public interface IRSocketService
	{
		string ServiceName { get; }
		IAsyncEnumerable<ReadOnlySequence<byte>> Dispatch(ReadOnlySequence<byte> data, string method, ReadOnlySequence<byte> tracing, ReadOnlySequence<byte> metadata);
	}


	static public class RSocketServiceExtensions
	{
		////TODO Java style
		//socket.AddService(new MyEchoServiceServer());

		static public void AddService(this RSocketServer socket, IRSocketService service) => RSocketProducer.Register(socket, service);
	}

	public class RSocketProducer        //TOD RemoteService? Need a better name, look at Java.
	{
		//TODO Non-static, per-socket

		static ConcurrentDictionary<string, IRSocketService> Services = new ConcurrentDictionary<string, IRSocketService>();

		static public void Register(RSocket socket, IRSocketService service)
		{
			Services[service.ServiceName] = service;

			//TODO Need to ensure that this really only happens once per Socket.

			socket.Stream(message => (Remote: new RSocketService.RemoteProcedureCallMetadata(message.Metadata), message.Data),
				request => Dispatch(request.Data, request.Remote.Service, request.Remote.Method, request.Remote.Tracing, request.Remote.Metadata),
				result => (Data: result, Metadata: default));
		}

		static IAsyncEnumerable<ReadOnlySequence<byte>> Dispatch(ReadOnlySequence<byte> data, string service, string method, ReadOnlySequence<byte> tracing, ReadOnlySequence<byte> metadata)
			=> (Services.TryGetValue(service, out var target)) ? target.Dispatch(data, method, tracing, metadata) : throw new InvalidOperationException();      //TODO Handle invalid service name request.
	}
}
