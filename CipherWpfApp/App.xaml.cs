using Autofac;
using HeBianGu.General.WpfControlLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using Autofac.Integration.Wcf;
using CipherLibrary.Wcf.Contracts;
using CipherWpfApp.Views;

namespace CipherWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var builder = new ContainerBuilder();

            // ...other Autofac registrations...


            builder.Register(c => new ChannelFactory<ICipherService>(
                    new BasicHttpBinding(),
                    new EndpointAddress("http://localhost:8000/FileCipher")))
                .SingleInstance();

            builder.Register(c => c.Resolve<ChannelFactory<ICipherService>>().CreateChannel())
                .As<ICipherService>()
                .UseWcfSafeRelease();

            builder.RegisterType<MainViewModel>().AsSelf();
            builder.RegisterType<MainWindow>().AsSelf();
            var container = builder.Build();

            // Resolve MainWindow and set its DataContext to the MainViewModel
            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.Show();
        }
    }
}
