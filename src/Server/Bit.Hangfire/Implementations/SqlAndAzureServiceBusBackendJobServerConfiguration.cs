using System;
using System.Transactions;
using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Azure.ServiceBusQueue;
using Hangfire.Logging;
using Hangfire.SqlServer;

namespace Bit.Hangfire.Implementations
{
    public class SqlAndAzureServiceBusBackendJobServerConfiguration : IAppEvents
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private BackgroundJobServer _backgroundJobServer;
        private readonly JobActivator _jobActivator;
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILogProvider _logProvider;

#if DEBUG
        protected SqlAndAzureServiceBusBackendJobServerConfiguration()
        {
        }
#endif

        public SqlAndAzureServiceBusBackendJobServerConfiguration(IAppEnvironmentProvider appEnvironmentProvider, JobActivator jobActivator, IAutofacDependencyManager dependencyManager, ILogProvider logProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (jobActivator == null)
                throw new ArgumentNullException(nameof(jobActivator));

            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (logProvider == null)
                throw new ArgumentNullException(nameof(logProvider));

            _logProvider = logProvider;

            _appEnvironmentProvider = appEnvironmentProvider;
            _jobActivator = jobActivator;
            _lifetimeScope = dependencyManager.GetContainer();
        }

        public virtual void OnAppStartup()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string jobSchedulerDbConnectionString = activeAppEnvironment.GetConfig<string>("JobSchedulerDbConnectionString");

            SqlServerStorage storage = new SqlServerStorage(jobSchedulerDbConnectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false,
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                SchemaName = "Jobs"
            });

            if (activeAppEnvironment.DebugMode == false)
            {
                string signalRAzureServiceBusConnectionString = activeAppEnvironment.GetConfig<string>("JobSchedulerAzureServiceBusConnectionString");

                storage.UseServiceBusQueues(signalRAzureServiceBusConnectionString);
            }

            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_lifetimeScope);
            GlobalConfiguration.Configuration.UseLogProvider(_logProvider);

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Activator = _jobActivator
            }, storage);
        }

        public virtual void OnAppEnd()
        {
            _backgroundJobServer?.Dispose();
        }
    }
}