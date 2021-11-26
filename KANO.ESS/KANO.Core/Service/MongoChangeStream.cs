using KANO.Core.Lib.Extension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public static class MongoChangeStreamServiceExtension
    {
        public static IServiceCollection AddMongoChangeStream(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAdd(ServiceDescriptor.Singleton<IMongoChangeStream, MongoChangeStream>());

            return services;
        }
    }

    public class MongoChangeStreamError : Exception { }

    public interface IMongoChangeStream
    {
        IMongoChangeStreamWatcher SubscribeTo<T>(Action<ChangeStreamDocument<T>> onEvent, Action<Exception> onShutdown);
    }

    public interface IMongoChangeStreamWatcher
    {
        void Run();
        void Stop();
    }

    public class MongoChangeStreamWatcher<T> : IMongoChangeStreamWatcher
    {
        private CancellationTokenSource Cancellation = new CancellationTokenSource();
        private object StateLock = new object();
        private bool AlreadyRun = false;

        private readonly IMongoCollection<T> Collection;
        private readonly Action<ChangeStreamDocument<T>> OnEvent;
        private readonly Action<Exception> OnShutdown;

        public MongoChangeStreamWatcher(IMongoCollection<T> collection, Action<ChangeStreamDocument<T>> onEvent, Action<Exception> onShutdown)
        {
            Collection = collection;
            OnEvent = onEvent;
            OnShutdown = onShutdown;
        }

        public async void Run()
        {
            lock (StateLock)
            {
                if (AlreadyRun)
                {
                    throw new Exception("This watcher is already run");
                }

                AlreadyRun = true;
            }

            try
            {
                Cancellation = new CancellationTokenSource();
                var options = new ChangeStreamOptions
                {
                    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
                };
                var watcher = Collection.Watch(options, Cancellation.Token);

                await watcher.ForEachAsync(OnEvent);
            }
            catch (TaskCanceledException)
            {
                OnShutdown(null);
            }
            catch (Exception e)
            {
                OnShutdown(e);
            }
            finally
            {
                Cancellation.Dispose();
            }
        }

        public void Stop()
        {
            lock (StateLock)
            {
                if (!AlreadyRun)
                    return;
            }

            Cancellation.Cancel();
        }
    }

    public class MongoChangeStream : IMongoChangeStream
    {
        private IMongoManager _mongo;

        public MongoChangeStream(IMongoManager db)
        {
            _mongo = db;
        }

        public IMongoChangeStreamWatcher SubscribeTo<T>(Action<ChangeStreamDocument<T>> onEvent, Action<Exception> onShutdown)
        {
            var attr = typeof(T).GetCustomAttribute<CollectionAttribute>(true);

            if (attr == null)
            {
                throw new ArgumentException(
                    $"Type {nameof(T)} does not declared as Collection");
            }

            return SubscribeTo(attr.Name, onEvent, onShutdown);
        }

        public IMongoChangeStreamWatcher SubscribeTo<T>(string CollectionName, Action<ChangeStreamDocument<T>> onEvent, Action<Exception> onShutdown)
        {
            var collection = _mongo.Database().GetCollection<T>(CollectionName);

            return new MongoChangeStreamWatcher<T>(collection, onEvent, onShutdown);
        }
    }
}
