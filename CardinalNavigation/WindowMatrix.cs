using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CardinalNavigation
{
    
    class WindowMatrix
    {

        private List<IVsFrameView> m_IVsFrames;
        private List<EnvDTE.Window> m_LinkedDTEWindows;

        private List<WindowControlAdapter> m_ActiveWindows;

        private WindowControlAdapter m_activeWindow;

        private double m_DeviceDpiX;
        private double m_DeviceDpiY;

        private double m_DpiXScale;
        private double m_DpiYScale;

        private double m_XDivide;
        private double m_YDivide;


        public WindowMatrix(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.setActiveWindows(package);
            m_IVsFrames = IVsUIWindowFrameExtractor.getIVsWindowFramesEnumerator(package);

            DTE myDTE = HelperMethods.getDTE(package);

            setWindowSelectionData();

            m_ActiveWindows = WindowControlAdapter.getLinkedWindowControlAdapters(m_IVsFrames, m_LinkedDTEWindows, myDTE.ActiveWindow).ToList();
            m_activeWindow = WindowControlAdapter.getActiveWindowControlAdapter(myDTE.ActiveWindow, m_ActiveWindows);
        }

        // note; need to test SystemDpi from hdpi monitors 
        private void setWindowSelectionData()
        {

            m_DeviceDpiX = DpiAwareness.SystemDpiX;
            m_DeviceDpiY = DpiAwareness.SystemDpiY;
            m_DpiXScale = m_DeviceDpiX / DpiAwareness.DefaultLogicalDpi;
            m_DpiYScale = m_DeviceDpiY / DpiAwareness.DefaultLogicalDpi;

            m_XDivide = CardinalNavigationConstants.DefaultLogicalXWindowDivide * m_DpiYScale;
            m_YDivide = CardinalNavigationConstants.DefaultLogicalTabPaneDivide * m_DpiYScale;

            // increase by scale factor to be safe
            m_XDivide *= CardinalNavigationConstants.DefaultLogicalSelectorScale;
            m_YDivide *= CardinalNavigationConstants.DefaultLogicalSelectorScale;

            var awarenessContext = DpiAwareness.ProcessDpiAwarenessContext;
        }

        private void setActiveWindows(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE myDTE = HelperMethods.getDTE(package);
            m_LinkedDTEWindows = HelperMethods.getLinkedWindowsList(myDTE.ActiveWindow.LinkedWindowFrame.LinkedWindows);
        }


        private static int AdjacencySize(int activeAxis, int activeHeightOrWidth, int windowAxis, int windowHeightOrWidth)
        {
            return System.Linq.Enumerable.Range(activeAxis, activeAxis+activeHeightOrWidth).Intersect(
                System.Linq.Enumerable.Range(windowAxis, windowAxis + windowHeightOrWidth)
                ).Count();
        }

        private void sortByLargestAdjacency(char direction)
        {
            // could just take max, but might be useful to have this sorted.
            if (direction == CardinalNavigationConstants.UP)
            {
                m_ActiveWindows.Sort((lhsWindow, rhsWindow) => { return 1; });

            }
            else if (direction == CardinalNavigationConstants.DOWN)
            {
                m_ActiveWindows.Sort((lhsWindow, rhsWindow) => {
                    var activeX = m_activeWindow.coordinates.x;
                    var activeWidth = m_activeWindow.coordinates.width;
                    var lhsX = lhsWindow.coordinates.x;
                    var lhsWidth = lhsWindow.coordinates.y;
                    var rhsX = rhsWindow.coordinates.x;
                    var rhsWidth = rhsWindow.coordinates.y;
                    var lhsAdjacencySize = AdjacencySize(activeX, activeWidth, lhsX, lhsWidth);
                    var rhsAdjacencySize = AdjacencySize(activeX, activeWidth, rhsX, rhsWidth);
                    return (lhsAdjacencySize - rhsAdjacencySize);
                });

            }
            else if (direction == CardinalNavigationConstants.LEFT)
            {
                
            }
            else if (direction == CardinalNavigationConstants.RIGHT)
            {

            }

            m_ActiveWindows.Reverse();

        }


        // pick top enumerable.range() to find the size of ranges while tiebreaking
        private void removeWindowsNotAligned(char direction)
        {    
            Func<WindowControlAdapter, bool> filterFunction = (win) =>
            {
                return false;
            };

            if (direction == CardinalNavigationConstants.UP)
            {
            }
            else if (direction == CardinalNavigationConstants.DOWN)
            {
                // var c = Enumerable.Range(9, 22).Count();

                filterFunction = (win) =>
                {
                    var activeX = m_activeWindow.coordinates.x;
                    var activeXWidth = m_activeWindow.coordinates.width;
                    var winX = win.coordinates.x;
                    var winWidth = win.coordinates.width;
                    return ((winX >= activeX && winX <= activeXWidth+activeX) || ((winX+winWidth >= activeX) && (winX+winWidth <= activeXWidth+activeX)));


                };

            }
            else if (direction == CardinalNavigationConstants.LEFT)
            {

            }
            else if (direction == CardinalNavigationConstants.RIGHT)
            {

            }

            m_ActiveWindows = m_ActiveWindows.Where(filterFunction).ToList();
        }

        private void removeWindowsNotAdjacent(char direction)
        {
            Func<WindowControlAdapter, bool> filterFunction = (win) =>
            {
                return false;
            };
            if (direction == CardinalNavigationConstants.UP)
            {
                
            }
            else if (direction == CardinalNavigationConstants.DOWN)
            {
                filterFunction = (win) =>
                {
                    return win.coordinates.y - (m_activeWindow.coordinates.y + m_activeWindow.coordinates.height) < m_YDivide;
                };
                
            }
            else if (direction == CardinalNavigationConstants.LEFT)
            {

            }
            else if (direction == CardinalNavigationConstants.RIGHT)
            {

            }

            m_ActiveWindows = m_ActiveWindows.Where(filterFunction).ToList();

        }

        private void removePointsInWrongDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Func<WindowControlAdapter, bool> filterFunction = (win) => {
                return false;
            };

            // we need to eliminate docked, invis wins before this point.

            if (direction == CardinalNavigationConstants.UP)
            {
                // do we want to jump 'across' the screen or no?
                // i should think nah, only if it's 'straight'above you. 
                // we'd need to calc if it is, but this'd also be an issue with all other transfers.

                filterFunction = (win) => {

                    ThreadHelper.ThrowIfNotOnUIThread();
                    // win.Top should be of a higher priority than m_seletedWindow.Top; since
                    // closer to top of IDE is lower no, the 'higher priority' here is 
                    // a lower number
                    return win.coordinates.y < m_activeWindow.coordinates.y;
                };
            }
            else if (direction == CardinalNavigationConstants.DOWN)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    // weird but main editor's 1 higher by default
                    return win.coordinates.y- m_activeWindow.coordinates.y > 1;
                };
            }
            else if (direction == CardinalNavigationConstants.LEFT)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return win.coordinates.x < m_activeWindow.coordinates.x;
                };
            }
            else if (direction == CardinalNavigationConstants.RIGHT)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return win.coordinates.x > m_activeWindow.coordinates.x;
                };
            }

            m_ActiveWindows = m_ActiveWindows.Where(filterFunction)
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

            ThreadHelper.ThrowIfNotOnUIThread();
            removePointsInWrongDirection(direction);
            removeWindowsNotAdjacent(direction);
            removeWindowsNotAligned(direction);
            sortByLargestAdjacency(direction);
            if (m_ActiveWindows.Count == 0)
            {
                return;
            }
            m_ActiveWindows.First().ActivateWindow();
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
        /// activate the window passed as a para
        /// </summary>
        /// <param name="window"></param>
        private void activateWindow(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            window.Activate();
        }

    }
}

