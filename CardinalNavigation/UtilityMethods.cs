
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CardinalNavigation
{
    class UtilityMethods
    {
        /// <summary>
        /// root automation model
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static DTE GetDTE(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            System.IServiceProvider serviceProvider = package as System.IServiceProvider;
            return (DTE)serviceProvider.GetService(typeof(DTE));
        }


        /// <summary>
        /// more involved window functionality than provided by the DTE 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static IVsUIShell GetIVsUIShell(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            System.IServiceProvider serviceProvider = package as System.IServiceProvider;
            return (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
        }


        private static List<EnvDTE.Window> GetLinkedParentWindows(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var linkedFrame = window?.LinkedWindowFrame;

            if (linkedFrame == null || linkedFrame.LinkedWindows == null)
            {
                return null;
            }

            return (List<EnvDTE.Window>)linkedFrame.LinkedWindows;
        }


        private static List<EnvDTE.Window> FindAllLinkedWindows(EnvDTE.Window window, List<EnvDTE.Window> linkedParentWindows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var top = window.Top;
            var left = window.Left;

            return linkedParentWindows?.FindAll((eachWindow) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return eachWindow.Top == top && eachWindow.Left == left;
            });
        }

        /// <summary>
        /// Is our our parent window the main window?
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static bool WindowIsLinked(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var linkedParentWindows = GetLinkedParentWindows(window);
            return FindAllLinkedWindows(window, linkedParentWindows)?.Count > 0;
        }

        /// <summary>
        /// if a given window is docked, we get the most recently used window
        /// docked at the same position. Otherwise, we return the window.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static EnvDTE.Window GetMostRecentlyUsedLinkedWindow(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var linkedParentWindows = GetLinkedParentWindows(window);
            if (linkedParentWindows?.Count <= 1)
            {
                return window;
            }
            return FindAllLinkedWindows(window, linkedParentWindows)?[0];
        }

        /// <summary>
        /// returns a list to LinkedWindows
        /// </summary>
        /// <param name="windows"></param>
        /// <returns></returns>
        public static List<EnvDTE.Window> GetWindowsList(EnvDTE.Windows windows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<EnvDTE.Window> windowsList = new List<Window>();
            foreach (var window in windows)
            {
                windowsList.Add((Window)window);
            }
            return windowsList;
        }

        /// <summary>
        /// converts enumerable to list--needed due to lack of full enumerator support.
        /// </summary>
        /// <param name="windows"></param>
        /// <returns></returns>
        public static List<EnvDTE.Window> GetLinkedWindowsList(EnvDTE.Window parentWindow, List<Window> allWindows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (parentWindow == null)
            {
                // it also seems intuitive that we could have multiple nested windows.. but i haven't seen a 
                // case where it happens yet.
                MessageBox.Show("Unable to find parent for active window.\n");
                throw new NullReferenceException();
            }

            // note: not all windows linked to a parent are in parent.LinkedWindows; we'll pair
            //       them manually.
            List<EnvDTE.Window> linkedWindows = new List<EnvDTE.Window>();

            foreach (var window in allWindows)
            {
                var eachWindowParentWindow = window?.LinkedWindowFrame;

                if (UtilityMethods.CompareWindows(eachWindowParentWindow, parentWindow))
                {
                    linkedWindows.Add(window);
                }
            }



            return linkedWindows;
        }


        /// <summary>
        /// special comparison function for EnvDTE.Window
        /// this is needed because some windows (e.g. properties) seem not to
        /// compare against eachother correctly from the IVsShell interface and
        /// the DTE.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool CompareWindows(EnvDTE.Window lhs, EnvDTE.Window rhs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (lhs == rhs)
            {
                return true;
            }

            if (lhs == null && rhs != null || lhs != null && rhs == null)
            {
                return false;
            }

            // properties window props differ when from activeWindow; if this is fixed, lhs == rhs should suffice. 
            if (lhs.Caption == rhs.Caption &&
                (
                (lhs.Type == vsWindowType.vsWindowTypeToolWindow && rhs.Type == vsWindowType.vsWindowTypeProperties) ||
                (lhs.Type == vsWindowType.vsWindowTypeProperties && rhs.Type == vsWindowType.vsWindowTypeToolWindow)
                )
                )
            {
                return true;
            }

            return false;

        }

    }
}

