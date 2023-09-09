using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.Wcf;
using CipherLibrary.DTOs;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileCryptorService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Services.PasswordService;
using CipherLibrary.Services.SecureConfigManager;
using CipherLibrary.Wcf.Contracts;

namespace CipherConsoleService
{
    internal class Program
    {
        private static IContainer Container { get; set; }
        private static ServiceHost _serviceHost;
        private static ISecureConfigManager _config;
        public static void Main(string[] args)
        {
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

            _serviceHost = new ServiceHost(typeof(CipherService), new Uri("net.tcp://localhost:8001"));
            _serviceHost.AddServiceEndpoint(typeof(ICipherService), binding, "FileCipher");

            var debug = _serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (debug == null)
            {
                _serviceHost.Description.Behaviors.Add(
                    new ServiceDebugBehavior() {IncludeExceptionDetailInFaults = true});
            }
            else
            {
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            _serviceHost.AddDependencyInjectionBehavior<ICipherService>(Container);

            _serviceHost.Opened += HostOnOpened;
            _serviceHost.Open();

            Console.ReadLine();
            _serviceHost.Close();
        }

        private static void HostOnOpened(object sender, EventArgs e)
        {
            Console.WriteLine("Message service started");
        }
    }
}