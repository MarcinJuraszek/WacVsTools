using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using EnvDTE;
using WacVsTools.Core;
using WacVsTools.Core.AttachToWacProcess;

namespace WacVsTools.VS2019
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidWacVsToolsPkgString)]
	public sealed class WacVsToolsPackage : Package
	{
		internal static Lazy<DTE2> DTE
						= new Lazy<DTE2>(() => ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2);

		private AttachToWacProcessMenuCommands _attachToWacProcessMenu;

		protected override void Initialize()
		{
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			var shell = GetService(typeof(SVsUIShell)) as IVsUIShell;
			if (mcs != null)
			{
				_attachToWacProcessMenu = new AttachToWacProcessMenuCommands(DTE.Value, mcs, shell);
				_attachToWacProcessMenu.SetupCommands();
			}
		}
	}
}
