using System.ComponentModel;
using System.Configuration.Install;

namespace Seq.Client.WindowsLogins
{
    [RunInstaller(true)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}