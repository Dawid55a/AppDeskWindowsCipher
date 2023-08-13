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
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileCryptorService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Wcf.Contracts;

namespace CipherConsoleService
{
    internal class Program
    {
        private static IContainer Container { get; set; }
        private static ServiceHost _serviceHost;
        private static readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private static Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public static void Main(string[] args)
        {

            if (_allAppSettings["WorkFolder"] == "")
            {
                _allAppSettings.Set("WorkFolder", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                _config.Save(ConfigurationSaveMode.Modified);
            }

            var builder = new ContainerBuilder();
            builder.RegisterType<EventLoggerService>().As<IEventLoggerService>().SingleInstance();
            builder.RegisterType<FileCryptorService>().As<IFileCryptorService>().SingleInstance();
            builder.RegisterType<FileEncryptionService>().As<IFileEncryptionService>();
            builder.RegisterType<FileDecryptionService>().As<IFileDecryptionService>();
            builder.RegisterType<FileListeningService>().As<IFileListeningService>();
            builder.RegisterType<CipherService>().As<ICipherService>();

            Container = builder.Build();


            var binding = new BasicHttpBinding();

            _serviceHost = new ServiceHost(typeof(CipherService), new Uri("http://localhost:8000"));
            _serviceHost.AddServiceEndpoint(typeof(ICipherService), binding, "FileCipher");


            var smb = _serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = true
                };
                _serviceHost.Description.Behaviors.Add(smb);
            }
            else
            {
                if (!smb.HttpGetEnabled)
                {
                    smb.HttpGetEnabled = true;
                }
            }


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