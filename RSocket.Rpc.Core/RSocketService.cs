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

		public RSocketService(RSocketClient client) { Client = client; }

		protected Task __RequestFireAndForget<TMessage>(TMessage message, Func<TMessage, byte[]> intransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default) => __RequestFireAndForget(new ReadOnlySequence<byte>(intransform(message)), metadata, tracing, service: service, method: method);
		protected Task __RequestFireAndForget<TMessage>(TMessage message, Func<TMessage, ReadOnlySequence<byte>> intransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default) => __RequestFireAndForget(intransform(message), metadata, tracing, service: service, method: method);
		protected async Task __RequestFireAndForget(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			var receiver = new Receiver();
			await Client.RequestFireAndForget(receiver, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing)).ConfigureAwait(false);
			receiver.TrySetResult(default);
			await receiver.Awaitable;
		}


		protected async Task<TResult> __RequestResponse<TMessage, TResult>(TMessage message, Func<TMessage, byte[]> intransform, Func<byte[], TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default) => outtransform((await __RequestResponse(new ReadOnlySequence<byte>(intransform(message)), metadata, tracing, service: service, method: method)).ToArray());
		protected async Task<TResult> __RequestResponse<TMessage, TResult>(TMessage message, Func<TMessage, ReadOnlySequence<byte>> intransform, Func<ReadOnlySequence<byte>, TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default) => outtransform(await __RequestResponse(intransform(message), metadata, tracing, service: service, method: method));
		protected async Task<ReadOnlySequence<byte>> __RequestResponse(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			var receiver = new Receiver();
			await Client.RequestResponse(receiver, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing));
			return await receiver.Awaitable;
		}

		protected IAsyncEnumerable<TResult> __RequestStream<TMessage, TResult>(TMessage message, Func<TMessage, byte[]> intransform, Func<byte[], TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return __RequestStream(new ReadOnlySequence<byte>(intransform(message)), metadata, mapper: value => outtransform(value.data.ToArray()), tracing, service: service, method: method);
		}

		protected IAsyncEnumerable<TResult> __RequestStream<TMessage, TResult>(TMessage message, Func<TMessage, ReadOnlySequence<byte>> intransform, Func<ReadOnlySequence<byte>, TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return __RequestStream(intransform(message), metadata, mapper: value => outtransform(value.data), tracing, service: service, method: method);
		}

		protected IAsyncEnumerable<T> __RequestStream<T>(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default, Func<(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata), T> mapper = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return new Receiver.Enumerable<T>(stream => Client.RequestStream(stream, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing)), value => mapper(value));   //TODO Policy
		}

		protected IAsyncEnumerable<TResult> __RequestChannel<TMessage, TResult>(IAsyncEnumerable<TMessage> messages, Func<TMessage, byte[]> intransform, Func<byte[], TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return default;
			//outtransform((await __RequestStream(new ReadOnlySequence<byte>(intransform(message)), metadata, tracing, service: service, method: method)).ToArray());
		}

		protected IAsyncEnumerable<TResult> __RequestChannel<TMessage, TResult>(IAsyncEnumerable<TMessage> messages, Func<TMessage, ReadOnlySequence<byte>> intransform, Func<ReadOnlySequence<byte>, TResult> outtransform, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return default;
			//	outtransform(await __RequestStream(intransform(message), metadata, tracing, service: service, method: method));
		}

		protected IAsyncEnumerable<ReadOnlySequence<byte>> __RequestChannel(IAsyncEnumerable<ReadOnlySequence<byte>> data, ReadOnlySequence<byte> metadata = default, ReadOnlySequence<byte> tracing = default, string service = default, [CallerMemberName]string method = default)
		{
			return default;
			//return new Receiver.Deferred(stream =>
			//	Client.RequestStream(stream, data, new RemoteProcedureCallMetadata(service, method, metadata, tracing), initial: 3)
			//);
		}



		//protected void RequestStream(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default) { Client.RequestStream(null, data, metadata); }   //TODO Initial? Or get from policy?
		//protected void RequestChannel(ReadOnlySequence<byte> data, ReadOnlySequence<byte> metadata = default) { Client.RequestChannel(null, data, metadata); } //TODO Initial?


		private class Receiver : TaskCompletionSource<ReadOnlySequence<byte>>, IRSocketStream
		{
			static public readonly IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Discard = new Null();


			public void OnCompleted() { }
			public void OnError(Exception error) => base.SetException(error);
			public void OnNext((ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) value) => base.SetResult(value.data);

			public ConfiguredTaskAwaitable<ReadOnlySequence<byte>> Awaitable => base.Task.ConfigureAwait(false);


			public class Enumerable<T> : IAsyncEnumerable<T>
			{
				readonly Func<IRSocketStream, Task> Subscriber;
				readonly Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> Mapper;

				//IObserver<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Observer;

				public Enumerable(Func<IRSocketStream, Task> subscriber, Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> mapper)
				{
					Subscriber = subscriber;
					Mapper = mapper;
				}


				public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				{
					var enumerator = new Enumerator(Mapper);
					Subscriber(enumerator);     //TODO Do we want to use this task too? It could fault. Also, cancellation. Nope, this should only await on the first MoveNext, so subscription is lower.
					return enumerator;
				}
				//IAsyncEnumerator<ReadOnlySequence<byte>> IAsyncEnumerable<ReadOnlySequence<byte>>.GetAsyncEnumerator(CancellationToken cancellationToken) => (IAsyncEnumerator<ReadOnlySequence<byte>>)GetAsyncEnumerator(cancellationToken);


				private class Enumerator : IAsyncEnumerator<T>, IRSocketStream
				{
					readonly Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> Mapper;
					public bool IsCompleted { get; private set; } = false;
					private (ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data) Value = default;
					private ConcurrentQueue<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data)> Queue;
					private AsyncManualResetEvent Continue = new AsyncManualResetEvent();
					private Exception Error;

					public Enumerator(Func<(ReadOnlySequence<byte> metadata, ReadOnlySequence<byte> data), T> mapper)
					{
						Mapper = mapper;
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

					public T Current => Mapper(Value);
					//ReadOnlySequence<byte> IAsyncEnumerator<ReadOnlySequence<byte>>.Current => Value.data;

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

					ValueTask IAsyncDisposable.DisposeAsync()
					{
						throw new NotImplementedException();
					}

					class AsyncManualResetEvent      //Steven Toub: https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
					{
						private volatile TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
						public Task WaitAsync() => Completion.Task;
						public void Set() { Completion.TrySetResult(true); }
						public void Reset() { while (true) { var previous = Completion; if (!previous.Task.IsCompleted || Interlocked.CompareExchange(ref Completion, new TaskCompletionSource<bool>(), previous) == previous) { return; } } }
					}
				}
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

