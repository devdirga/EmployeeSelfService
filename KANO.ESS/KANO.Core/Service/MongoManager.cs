using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KANO.Core.Service
{
    public static class MongoManagerServiceExtension
    {
        public static IServiceCollection AddMongoManager(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IMongoManager, MongoManager>());

            return services;
        }
    }

    public interface IMongoManager
    {
        IMongoDatabase Database();
        IMongoDatabase Database(string connectionname);
    }

    public class MongoManagerConnectionInfo
    {
        public MongoManagerConnectionInfo() { }

        public MongoManagerConnectionInfo(string connString)
        {
            ConnectionString = connString;
        }

        public MongoManagerConnectionInfo(MongoManagerConnectionInfo conn)
        {
            ConnectionString = conn.ConnectionString;
        }

        public string ConnectionString { get; set; }
    }

    public class MongoManagerOptions : IOptions<MongoManagerOptions>
    {
        public IDictionary<string, MongoManagerConnectionInfo> ConnectionList { get; set; }
        public string DefaultConnection { get; set; }

        MongoManagerOptions IOptions<MongoManagerOptions>.Value => this;
    }

    public class MongoManager: IMongoManager
    {
        protected class MongoClientData
        {
            public MongoUrl Uri { get; }
            public string ConnectionString { get; }
            public IMongoClient Client { set; get; } = null;
            public object Lock { set; get; } = new object();

            public MongoClientData(string connString)
            {
                ConnectionString = connString;
                Uri = MongoUrl.Create(ConnectionString);
            }

            public MongoClientData(MongoManagerConnectionInfo conn)
            {
                ConnectionString = conn.ConnectionString;
                Uri = MongoUrl.Create(ConnectionString);
            }

            public MongoClientData(MongoClientData conn)
            {
                ConnectionString = conn.ConnectionString;
                Uri = MongoUrl.Create(ConnectionString);
            }
        }

        private readonly string _defaultConnectionName;
        private readonly MongoClientData _defaultConnectionInfo;
        private readonly IDictionary<string, MongoClientData> _connList;
        private object _connLock = new object();
        
        private void ValidateOptions(MongoManagerOptions options)
        {
            if (string.IsNullOrEmpty(options.DefaultConnection))
            {
                throw new ArgumentException(
                    $"{nameof(options.DefaultConnection)} cannot be empty or null.");
            }

            if (options.ConnectionList == null || options.ConnectionList.Count() == 0)
            {
                throw new ArgumentException(
                    $"{nameof(options.ConnectionList)} cannot be empty or null.");
            }

            var found = false;
            foreach (var con in options.ConnectionList)
            {
                if (con.Key == options.DefaultConnection)
                {
                    found = true;
                }

                if (options.ConnectionList == null || options.ConnectionList.Count() == 0)
                {
                    throw new ArgumentException(
                        $"{nameof(options.ConnectionList)}.{nameof(con.Value.ConnectionString)} cannot be empty or null.");
                }
            }

            if (!found)
            {
                throw new ArgumentException(
                    "DefaultConnection contains invalid connection name");
            }
        }

        public MongoManager(IOptions<MongoManagerOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            ValidateOptions(options);

            _connList = new Dictionary<string, MongoClientData>();
            foreach (var conn in options.ConnectionList)
            {
                _connList.Add(conn.Key, new MongoClientData(conn.Value));
            }

            _defaultConnectionName = options.DefaultConnection;
            _defaultConnectionInfo = _connList[_defaultConnectionName];
        }

        public IMongoDatabase Database()
        {
            return Connect(_defaultConnectionInfo);
        }

        public IMongoDatabase Database(string connectionname)
        {
            var found = _connList.TryGetValue(connectionname, out MongoClientData info);
            if (!found)
            {
                throw new ArgumentException(
                    "ConnectionName does not exist on ConnectionList Config");
            }

            return Connect(info);
        }

        private IMongoDatabase Connect(MongoClientData info)
        {
            lock (info.Lock)
            {
                if (info.Client == null)
                    info.Client = new MongoClient(info.Uri);
            }
            var replicaordbname = info.Uri.DatabaseName != null ? info.Uri.DatabaseName : info.Uri.ReplicaSetName;
            return info.Client.GetDatabase(replicaordbname);
        }
    }
}
