using System;
using System.Collections.Generic;
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
        public static void Main(string[] args)
        {

            var builder = new ContainerBuilder();
            builder.RegisterType<EventLoggerService>().As<IEventLoggerService>().SingleInstance();
            builder.RegisterType<FileCryptorService>().As<IFileCryptorService>().SingleInstance();
            builder.RegisterType<FileEncryptionService>().As<IFileEncryptionService>();
            builder.RegisterType<FileDecryptionService>().As<IFileDecryptionService>();
            builder.RegisterType<FileListeningService>().As<IFileListeningService>();
            builder.RegisterType<CipherService>().As<ICipherService>();

            Container = builder.Build();

            using (var scope = Container.BeginLifetimeScope())
            {
                var service = scope.Resolve<IFileCryptorService>();
                service.Setup();

                var binding = new BasicHttpBinding();


                _serviceHost = new ServiceHost(typeof(CipherService), new Uri("http://localhost:8000"));
                _serviceHost.AddDependencyInjectionBehavior<ICipherService>(scope);

                _serviceHost.AddServiceEndpoint(typeof(ICipherService), binding, "FileCipher");
                var smb = new ServiceMetadataBehavior { HttpGetEnabled = true }; smb.HttpGetEnabled = true;
                _serviceHost.Description.Behaviors.Add(smb);


                _serviceHost.Opened += HostOnOpened;
                _serviceHost.Open();
            }


            Console.ReadLine();
            _serviceHost.Close();
        }
        private static void HostOnOpened(object sender, EventArgs e)
        {
            Console.WriteLine("Message service started");
        }
    }
}
