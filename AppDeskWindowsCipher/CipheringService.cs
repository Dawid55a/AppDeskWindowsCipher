using CipherLibrary.Wcf.Contracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileCryptorService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Services.PasswordService;
using System.ServiceModel.Description;
using System.Collections.Specialized;
using Autofac.Integration.Wcf;
using CipherLibrary.DTOs;
using CipherLibrary.Services.SecureConfigManager;

namespace AppDeskWindowsCipher
{
    public partial class CipheringService : ServiceBase
    {
        private static IContainer Container { get; set; }
        private static ServiceHost _serviceHost;
        private static ISecureConfigManager _config;
        private IEventLoggerService _logger = new EventLoggerService();
        public CipheringService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Inicjalizacja kontenera przed pierwszym logowaniem
            var builder = new ContainerBuilder();
            builder.RegisterType<EventLoggerService>().As<IEventLoggerService>().SingleInstance();
            builder.RegisterType<FileCryptorService>().As<IFileCryptorService>().SingleInstance();
            builder.RegisterType<FileEncryptionService>().As<IFileEncryptionService>();
            builder.RegisterType<FileDecryptionService>().As<IFileDecryptionService>();
            builder.RegisterType<FileListeningService>().As<IFileListeningService>();
            builder.RegisterType<PasswordService>().As<IPasswordService>();
            builder.RegisterType<CipherService>().As<ICipherService>();
            builder.RegisterType<SecureConfigManager>().As<ISecureConfigManager>();



            Container = builder.Build();

            _config = Container.Resolve<ISecureConfigManager>();
            if (string.IsNullOrEmpty(_config.GetSetting(AppConfigKeys.WorkFolder)))
            {
                _config.SaveSetting(AppConfigKeys.WorkFolder, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }

            var binding = new NetTcpBinding();

            try
            {
                _serviceHost = new ServiceHost(typeof(CipherService), new Uri("net.tcp://localhost:8001"));
                _serviceHost.AddServiceEndpoint(typeof(ICipherService), binding, "FileCipher");

                var debug = _serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (debug == null)
                {
                    _serviceHost.Description.Behaviors.Add(
                        new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
                }
                else
                {
                    if (!debug.IncludeExceptionDetailInFaults)
                    {
                        debug.IncludeExceptionDetailInFaults = true;
                    }
                }

                _serviceHost.AddDependencyInjectionBehavior<ICipherService>(Container);

                _serviceHost.Open();
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Failed to open the service host: {ex}");
                throw;
            }

            _logger.WriteInfo("Service has started.");
        }

        protected override void OnStop()
        {
            // Logowanie zatrzymania usługi
            _logger.WriteInfo("Service is stopping.");

            _serviceHost?.Close();

            _logger.WriteInfo("Service has stopped.");
        }
    }
}
