
using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CardinalNavigation
{
    class UtilityMethods
    {
        /// <summary>
        /// Gets the root automation model
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static DTE getDTE(AsyncPackage package)
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
        public static IVsUIShell getIVsUIShell(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            System.IServiceProvider serviceProvider = package as System.IServiceProvider;
            return (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
        }


        private static List<EnvDTE.Window> getLinkedParentWindows(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var linkedFrame = window?.LinkedWindowFrame;

            if (linkedFrame == null || linkedFrame.LinkedWindows == null)
            {
                return null;
            }

            return (List<EnvDTE.Window>)linkedFrame.LinkedWindows;
        }

        private static List<EnvDTE.Window> findAllLinkedWindows(EnvDTE.Window window, List<EnvDTE.Window> linkedParentWindows)
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
        public static bool windowIsLinked(EnvDTE.Window window)
        {
            var linkedParentWindows = getLinkedParentWindows(window);
            return findAllLinkedWindows(window, linkedParentWindows)?.Count > 0;
        }

        /// <summary>
        /// if a given window is docked, we get the most recently used window
        /// docked at the same position. Otherwise, we return the window.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static EnvDTE.Window getMostRecentlyUsedLinkedWindow(EnvDTE.Window window)
        {
            var linkedParentWindows = getLinkedParentWindows(window);
            if (linkedParentWindows?.Count <= 1)
            {
                return window;
            }
            return findAllLinkedWindows(window, linkedParentWindows)?[0];
        }

        /// <summary>
        /// returns a list to LinkedWindows
        /// </summary>
        /// <param name="windows"></param>
        /// <returns></returns>
        public static List<EnvDTE.Window> getWindowsList(EnvDTE.Windows windows)
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
        public static List<EnvDTE.Window> getLinkedWindowsList(EnvDTE.LinkedWindows windows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (windows == null)
            {
                throw new NullReferenceException();
            }

            List<EnvDTE.Window> linkedWindows = new List<EnvDTE.Window>();

            foreach (var window in windows)
            {
                linkedWindows.Add((EnvDTE.Window)window);
            }

            return linkedWindows;
        }


        /// <summary>
        /// Compares lhs and rhs, with special cases where returned objects from these apis differ.
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

            // properties window props differ when from activeWindow; if this is fixed, above if statement should work. 
            if (lhs.Caption == rhs.Caption
                &&
                (lhs.Type == vsWindowType.vsWindowTypeToolWindow && rhs.Type == vsWindowType.vsWindowTypeProperties) ||
                (lhs.Type == vsWindowType.vsWindowTypeProperties && rhs.Type == vsWindowType.vsWindowTypeToolWindow)
                )
            {
                return true;
            }

            return false;

        }



    }
}

