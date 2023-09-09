using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AppDeskWindowsCipher
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller processInstaller;
        public ProjectInstaller()
        {
            InitializeComponent();
            //
            // processInstaller = new ServiceProcessInstaller();
            // serviceInstaller = new ServiceInstaller();
            //
            // // Ustawienia konta, pod którym uruchomiona zostanie usługa
            // processInstaller.Account = ServiceAccount.LocalSystem;
            //
            // // Nazwa i konfiguracja usługi
            // serviceInstaller.ServiceName = "CipheringService";
            // serviceInstaller.DisplayName = "CipheringService for AppDesk";
            // serviceInstaller.Description = "****************Service for ciphering files in AppDesk******************";
            // serviceInstaller.StartType = ServiceStartMode.Automatic;
            //
            // // Dodanie instalatorów do kolekcji.
            // Installers.Add(serviceInstaller);
            // Installers.Add(processInstaller);
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            // using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            // {
            //     sc.Start();
            // }
            ServiceInstaller serviceInstaller = (ServiceInstaller)sender;
            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            {
                sc.Start();
            }
        }
    }
}
