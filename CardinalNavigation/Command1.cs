using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CardinalNavigation
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("c2180c7a-1fe2-49d1-8ade-2e4376a6f8bf");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command1(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command1 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command1(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "Command1";
            //foreach (var window in windows)
            //{

            //    Console.WriteLine("Window");
            //}


            //IVsUIShell svui = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
            //IEnumWindowFrames test;
            //int ok1 = svui.GetToolWindowEnum(out test);
            //if (ok1 != VSConstants.S_OK)
            //{
            //    //do error things
            //}

            //List<GenericWindowFrame> lgwm = new List<GenericWindowFrame>();
            //// get the frames
            //var frame = new IVsWindowFrame[1];
            //int ok = VSConstants.S_OK;
            //while (ok == VSConstants.S_OK)
            //{
            //    uint fetched;
            //    ok = test.Next(1, frame, out fetched);
            //    ErrorHandler.ThrowOnFailure(ok);
            //    if (fetched == 1)
            //    {
            //        var framepos = new VSSETFRAMEPOS[1];
            //        var guid = new Guid();
            //        Int32 ux, uy, pcy, pcx;
            //        frame[0].GetFramePos(framepos, out guid, out ux, out uy, out pcy, out pcx);
            //        object frameType = new object();
            //        frame[0].GetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, out frameType);
            //        var isVisible = frame[0].IsVisible();

            //        // this one, finally, answers the question "is the window docked and invisible?"
            //        // along w "VSM_Docked" from above..
            //        int onScreen;
            //        var isOnScreen = frame[0].IsOnScreen(out onScreen);
            //        // ok
            //        ok = 0;
                    
            //        var gwf = new GenericWindowFrame(frame[0]);
            //        lgwm.Add(gwf);
            //    }
            //}
            //IServiceProvider. Microsoft.VisualStudio.Shell.Interop.SVsUIShell
            // 'is in same tab group'
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivswindowframe6?view=visualstudiosdk-2019
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivswindowframe4?view=visualstudiosdk-2019


            WindowMatrix windowMatrix = new WindowMatrix(package);
            windowMatrix.navigateInDirection(Constants.DOWN);
            // seems like Windows only registers those we've opened in this session..
            // windowMatrix.addWindows(myDTE.Windows);

            // Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
