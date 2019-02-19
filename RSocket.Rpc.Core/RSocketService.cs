using System;
using System.Buffers;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using RSocket.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

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



		private class Receiver : TaskCompletionSource<ReadOnlySequence<byte>>, IRSocketStream
		{
			static public readonly IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Discard = new Null();


			public void OnCompleted() { }
			public void OnError(Exception error) => base.SetException(error);
			public void OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value) => base.SetResult(value.data);

			public ConfiguredTaskAwaitable<ReadOnlySequence<byte>> Awaitable => base.Task.ConfigureAwait(false);


			public class Enumerable<TSource, T> : Enumerable<T>
			{
				public Enumerable(Func<IRSocketStream, Task<IRSocketChannel>> subscriber, IAsyncEnumerable<TSource> source, Func<TSource, (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> sourcemapper, Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> resultmapper) :
					base(stream => Subscribe(stream, subscriber(stream), source, sourcemapper), resultmapper)
				{
				}

				static async Task Subscribe(IRSocketStream stream, Task<IRSocketChannel> original, IAsyncEnumerable<TSource> source, Func<TSource, (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> sourcemapper)
				{
					var channel = await original;     //Let the receiver hook up first before we start generating values.
					var enumerator = source.GetAsyncEnumerator();
					try
					{
						while (await enumerator.MoveNextAsync())
						{
							await channel.Send(sourcemapper(enumerator.Current));
						}
					}
					finally { await enumerator.DisposeAsync(); }
				}
			}


			public class Enumerable<T> : IAsyncEnumerable<T>
			{
				readonly Func<IRSocketStream, Task> Subscriber;
				readonly Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> Mapper;

				public Enumerable(Func<IRSocketStream, Task> subscriber, Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> mapper)
				{
					Subscriber = subscriber;
					Mapper = mapper;
				}

				public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				{
					var enumerator = new MappedEnumerator(Mapper);
					Subscriber(enumerator);     //TODO Do we want to use this task too? It could fault. Also, cancellation. Nope, this should only await on the first MoveNext, so subscription is lower.
					return enumerator;
				}

				private class Enumerator : IAsyncEnumerator<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>, IRSocketStream
				{
					public bool IsCompleted { get; private set; } = false;
					private (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) Value = default;
					private ConcurrentQueue<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Queue;
					private AsyncManualResetEvent Continue = new AsyncManualResetEvent();
					private Exception Error;

					public Enumerator()
					{
						Queue = new ConcurrentQueue<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>();
					}

					public async ValueTask<bool> MoveNextAsync()
					{
						while (true)
						{
							if (Queue.TryDequeue(out Value)) { return true; }
							await Continue.WaitAsync();
							if (Error != default) { throw Error; }
							else if (IsCompleted) { return false; }
							else { Continue.Reset(); }
						}
					}

					public (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) Current => Value;

					public ValueTask DisposeAsync()
					{
						return new ValueTask();
					}

					public void OnCompleted() { IsCompleted = true; ; Continue.Set(); }
					public void OnError(Exception error) { Error = error; Continue.Set(); }
					public void OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value)
					{
						//TODO Would we really need to interlock this? If the Queue isn't allocated, it's the first time through...?
						//var value = Interlocked.Exchange<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>(ref Value, default);     //TODO Hmmm, no ValueTuples... Could save Queue allocation if only going to get one...
						Queue.Enqueue(value);
						Continue.Set();
					}

					class AsyncManualResetEvent      //Steven Toub: https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
					{
						private volatile TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
						public Task WaitAsync() => Completion.Task;
						public void Set() { Completion.TrySetResult(true); }
						public void Reset() { while (true) { var previous = Completion; if (!previous.Task.IsCompleted || Interlocked.CompareExchange(ref Completion, new TaskCompletionSource<bool>(), previous) == previous) { return; } } }
					}
				}

				private class MappedEnumerator : Enumerator, IAsyncEnumerator<T>, IRSocketStream
				{
					readonly Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> Mapper;

					public MappedEnumerator(Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> mapper)
					{
						Mapper = mapper;
					}

					public new T Current => Mapper(base.Current);
					//ReadOnlySequence<byte> IAsyncEnumerator<ReadOnlySequence<byte>>.Current => Value.data;

					public new ValueTask DisposeAsync()
					{
						return base.DisposeAsync();
					}
				}



				//private class Enumerator : IAsyncEnumerator<T>, IRSocketStream
				//{
				//	readonly Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> Mapper;
				//	public bool IsCompleted { get; private set; } = false;
				//	private (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) Value = default;
				//	private ConcurrentQueue<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Queue;
				//	private AsyncManualResetEvent Continue = new AsyncManualResetEvent();
				//	private Exception Error;

				//	public Enumerator(Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> mapper)
				//	{
				//		Mapper = mapper;
				//		Queue = new ConcurrentQueue<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>();
				//	}

				//	public async ValueTask<bool> MoveNextAsync()
				//	{
				//		while (true)
				//		{
				//			if (Queue.TryDequeue(out Value)) { return true; }
				//			await Continue.WaitAsync();
				//			if (Error != default) { throw Error; }
				//			else if (IsCompleted) { return false; }
				//			else { Continue.Reset(); }
				//		}
				//	}

				//	public T Current => Mapper(Value);
				//	//ReadOnlySequence<byte> IAsyncEnumerator<ReadOnlySequence<byte>>.Current => Value.data;

				//	public ValueTask DisposeAsync()
				//	{
				//		return new ValueTask();
				//	}


				//	public void OnCompleted() { IsCompleted = true; ; Continue.Set(); }
				//	public void OnError(Exception error) { Error = error; Continue.Set(); }
				//	public void OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value)
				//	{
				//		//TODO Would we really need to interlock this? If the Queue isn't allocated, it's the first time through...?
				//		//var value = Interlocked.Exchange<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>(ref Value, default);     //TODO Hmmm, no ValueTuples... Could save Queue allocation if only going to get one...
				//		Queue.Enqueue(value);
				//		Continue.Set();
				//	}

				//	class AsyncManualResetEvent      //Steven Toub: https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
				//	{
				//		private volatile TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
				//		public Task WaitAsync() => Completion.Task;
				//		public void Set() { Completion.TrySetResult(true); }
				//		public void Reset() { while (true) { var previous = Completion; if (!previous.Task.IsCompleted || Interlocked.CompareExchange(ref Completion, new TaskCompletionSource<bool>(), previous) == previous) { return; } } }
				//	}
				//}


			}


			public class Deferred : IObservable<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>, IRSocketStream, IDisposable
			{
				IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Observer;
				//IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> ObserverSafe => Observer ?? throw new InvalidOperationException($"Stream has been Disposed");
				readonly Func<IRSocketStream, Task> Subscriber;

				public Deferred(Func<IRSocketStream, Task> subscriber)
				{
					Subscriber = subscriber;
				}

				public IDisposable Subscribe(IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> observer)
				{
					if (Observer == Discard) { throw new InvalidOperationException($"Stream already Disposed."); }
					if (Observer != null) { throw new InvalidOperationException($"Streams can only have a single Observer."); }
					Observer = observer;
					Subscribe();
					return this;
				}

				private async void Subscribe()
				{
					try { await Subscriber(this).ConfigureAwait(false); }
					catch (Exception ex) { Observer.OnError(ex); this.Dispose(); }
				}

				public void Dispose()
				{
					Observer = Discard;    //Anything that arrives after this must be discarded. ObserverSafe above was the alternative, but throwing behind the scenes on a last packet seems bad.
										   //TODO Close connection.
				}

				//This would be unnecessary if the underlying receivers just requested IObserver<T>.
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnCompleted() => Observer.OnCompleted();
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnError(Exception error) => Observer.OnError(error);
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value) => Observer.OnNext(value);
			}


			private class Null : IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>
			{
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnCompleted() { }
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnError(Exception error) { }
				void IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)>.OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value) { }
			}
		}

	}
}