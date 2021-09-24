using Microsoft.Extensions.DependencyInjection;
using SortThing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing
{
    public class ServiceContainer
    {
        private static IServiceProvider _instance;
        public static IServiceProvider Instance => _instance ??= Build();

        private static IServiceProvider Build()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IMetadataReader, MetadataReader>();
            serviceCollection.AddScoped<ILogger, Logger>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
