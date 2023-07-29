using Autofac.Features.ResolveAnything;
using Autofac;
using CipherLibrary.Wcf.Contracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using Autofac.Integration.Wcf;
using CipherLibrary.Services.EventLoggerService;
using WpfApp.ViewModels;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContainer _container;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var builder = new ContainerBuilder();

            // builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            builder.Register(c => new ChannelFactory<ICipherService>(
                    new BasicHttpBinding(),
                    new EndpointAddress("http://localhost:8000/FileCipher")))
                .SingleInstance();

            builder.Register(c => c.Resolve<ChannelFactory<ICipherService>>().CreateChannel())
                .As<ICipherService>()
                .UseWcfSafeRelease()
                .SingleInstance();


            builder.RegisterType<MainAppViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<EventLoggerService>().As<IEventLoggerService>();

            // Resolve MainWindow and set its DataContext to the MainViewModel
            builder.RegisterType<MainWindow>().OnActivated(w =>
            {
                w.Instance.DataContext = w.Context.Resolve<MainAppViewModel>();
            }).SingleInstance();

            _container = builder.Build();


            var mainWindow = _container.Resolve<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _container.Dispose();

            base.OnExit(e);
        }
    }
}
