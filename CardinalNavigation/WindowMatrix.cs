using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CardinalNavigation
{
    
    class WindowMatrix
    {

        List<WindowControlAdapter> m_windows;
        private List<GenericWindowFrame> m_IVsFrames;
        private List<EnvDTE.Window> m_activeWindows;
        private EnvDTE.Window m_selectedWindow;

        public WindowMatrix(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.setActiveWindows(package);
            m_IVsFrames = IVsUIWindowFrameExtractor.getIVsWindowFramesEnumerator(package);
            var controlAdapters = WindowControlAdapter.GetWindowControlAdapters(m_IVsFrames, m_activeWindows);
            foreach (var window in controlAdapters)
            {
                Console.WriteLine("window");
            }
        }

        private void setActiveWindows(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE myDTE = HelperMethods.getDTE(package);
            m_selectedWindow = myDTE.ActiveWindow;
            if (m_selectedWindow == null)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }
            m_activeWindows = HelperMethods.getLinkedWindowsList(m_selectedWindow.LinkedWindowFrame.LinkedWindows);
        }

        /// <summary>
        /// register the currently active windows so that we may switch between them.
        /// </summary>
        /// <param name="windows"></param>
        private void addWindows(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<EnvDTE.Window> linkedWindows = HelperMethods.getLinkedWindowsList(window.LinkedWindows);

            foreach (var childWindow in linkedWindows)
            {
                Console.WriteLine("window!");
                var left = childWindow.Left;
                var top = childWindow.Top;
                var width = childWindow.Width;
                var height = childWindow.Height;
                var isFloating = childWindow.IsFloating;

                // i suspect this is 'sub-windows'
                // it should be? but doesn't work? hmm...
                var linkedWindowFrame = childWindow.LinkedWindowFrame;
                var visible = linkedWindowFrame.Visible;

                var tabbedType = EnvDTE.vsLinkedWindowType.vsLinkedWindowTypeTabbed;
                var dockedType = EnvDTE.vsLinkedWindowType.vsLinkedWindowTypeDocked;
                
            }
        }


        private void removePointsInWrongDirection(char direction)
        {
            Func<EnvDTE.Window, bool> filterFunction = (win) => {
                return false;
            };

            // we need to eliminate docked, invis wins before this point.

            if (direction == Constants.UP)
            {
                // do we want to jump 'across' the screen or no?
                // i should think nah, only if it's 'straight'above you. 
                // we'd need to calc if it is, but this'd also be an issue with all other transfers.

                filterFunction = (win) => {

                    ThreadHelper.ThrowIfNotOnUIThread();
                    // win.Top should be of a higher priority than m_seletedWindow.Top; since
                    // closer to top of IDE is lower no, the 'higher priority' here is 
                    // a lower number
                    return win.Top < m_selectedWindow.Top;
                };
            }
            else if (direction == Constants.DOWN)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    // weird but main editor's 1 higher by default
                    return win.Top - m_selectedWindow.Top > 1;
                };
            }
            else if (direction == Constants.LEFT)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return win.Left < m_selectedWindow.Left;
                };
            }
            else if (direction == Constants.RIGHT)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return win.Left > m_selectedWindow.Left;
                };
            }

            m_activeWindows = m_activeWindows.Where(filterFunction)
                                             .ToList();
            // can't activate here
            // need to find adjacent windows

        }

        /// <summary>
        /// Remove all points in the wrong direction, or that aren't bordering out
        /// active window.
        /// </summary>
        /// <param name="direction"></param>
        private void removeIneligiblePoints(char direction)
        {

            removePointsInWrongDirection(direction);
            // remove non-adjacent
            // select one 
        }

        /// <summary>
        /// swap the current active window for the one found here
        /// or do nothing if none is found.
        /// </summary>
        /// <param name="direction"></param>
        public void navigateInDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            removeIneligiblePoints(direction);
        }

        /// <summary>
        /// activate the window passed as a param
        /// </summary>
        /// <param name="window"></param>
        private void activateWindow(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            window.Activate();
        }

    }
}
