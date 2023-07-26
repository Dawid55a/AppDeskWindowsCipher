using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AppDeskWindowsCipher
{
    public partial class CipheringService : ServiceBase
    {
        private ServiceHost _host;
        public CipheringService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _host = new ServiceHost(typeof(CipherWcfService.CipherService));
            _host.Open();
        }

        protected override void OnStop()
        {
            _host?.Close();
        }
    }
}
