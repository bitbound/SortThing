using Microsoft.Extensions.DependencyInjection;
using SortThing.Services;
using System;

namespace SortThing
{
    public class ServiceContainer
    {
        private static IServiceProvider _instance;
        public static IServiceProvider Instance => _instance ??= Build();

        public static IServiceProvider Build()
        {
            if (_instance is not null)
            {
                return _instance;
            }

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IMetadataReader, MetadataReader>();
            serviceCollection.AddScoped<IJobRunner, JobRunner>();
            serviceCollection.AddSingleton<IJobWatcher, JobWatcher>();
            serviceCollection.AddScoped<IPathTransformer, PathTransformer>();
            serviceCollection.AddScoped<ILogger, Logger>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
