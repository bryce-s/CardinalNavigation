using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace CardinalNavigation
{

    class WindowMatrix
    {

        private List<IVsFrameView> m_IVsFrames;

        private List<WindowControlAdapter> m_ActiveWindows;

        private WindowControlAdapter m_activeWindow;

        private double m_DeviceDpiX;
        private double m_DeviceDpiY;

        private double m_DpiXScale;
        private double m_DpiYScale;

        private double m_XDivide;
        private double m_YDivide;


        /// <summary>
        /// initalize windowmatrix and track windows; no filtering
        /// </summary>
        /// <param name="package"></param>
        public WindowMatrix(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.setActiveWindows(package);
            m_IVsFrames = IVsUIWindowFrameExtractor.getIVsWindowFramesEnumerator(package);

            DTE dteService = UtilityMethods.getDTE(package);

            SetWindowDivideSelectionSizes();

            m_ActiveWindows = WindowControlAdapter.getLinkedWindowControlAdapters(m_IVsFrames,
                UtilityMethods.getWindowsList(dteService.Windows),
                dteService.ActiveWindow).
                ToList();

            m_activeWindow = WindowControlAdapter.getActiveWindowControlAdapter(dteService.ActiveWindow, m_ActiveWindows);
        }

        // note; need to test SystemDpi from hdpi monitors 
        private void SetWindowDivideSelectionSizes()
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
            DTE myDTE = UtilityMethods.getDTE(package);
        }


        private static int AdjacencySize(int activeAxis, int activeHeightOrWidth, int windowAxis, int windowHeightOrWidth)
        {
            return System.Linq.Enumerable.Range(activeAxis, activeAxis + activeHeightOrWidth).Intersect(
                System.Linq.Enumerable.Range(windowAxis, windowAxis + windowHeightOrWidth)
                ).Count();
        }

        private void sortByLargestAdjacency(char direction)
        {
            // we don't want max exactly..
            // don't use Abs; c# sort compares based on numeric result 
            if (direction == CardinalNavigationConstants.UP || direction == CardinalNavigationConstants.DOWN)
            {
                m_ActiveWindows.Sort((lhsWindow, rhsWindow) =>
                {
                    var activeWindowXCoordinate = m_activeWindow.coordinates.x;
                    var activeWindowWidth = m_activeWindow.coordinates.width;
                    var lhsWindowXCoordinate = lhsWindow.coordinates.x;
                    var lhsWindowWidth = lhsWindow.coordinates.width;
                    var rhsWindowXCoordinate = rhsWindow.coordinates.x;
                    var rhsWindowWidth = rhsWindow.coordinates.width;
                    var lhsAdjacencySize = AdjacencySize(activeWindowXCoordinate, activeWindowWidth, lhsWindowXCoordinate, lhsWindowWidth);
                    var rhsAdjacencySize = AdjacencySize(activeWindowXCoordinate, activeWindowWidth, rhsWindowXCoordinate, rhsWindowWidth);
                    return (lhsAdjacencySize - rhsAdjacencySize);
                });
            }
            else if (direction == CardinalNavigationConstants.LEFT || direction == CardinalNavigationConstants.RIGHT)
            {
                m_ActiveWindows.Sort((lhsWindow, rhsWindow) =>
                {
                    var activeWindowYCoordinate = m_activeWindow.coordinates.y;
                    var activeWindowHeight = m_activeWindow.coordinates.height;
                    var lhsWindowYCoordinate = lhsWindow.coordinates.y;
                    var lhsWindowHeight = lhsWindow.coordinates.height;
                    var rhsWindowYCoordinate = rhsWindow.coordinates.y;
                    var rhsWindowHeight = rhsWindow.coordinates.height;
                    var lhsAdjacencySize = AdjacencySize(activeWindowYCoordinate, activeWindowHeight, lhsWindowYCoordinate, lhsWindowHeight);
                    var rhsAdjacencySize = AdjacencySize(activeWindowYCoordinate, activeWindowHeight, rhsWindowYCoordinate, rhsWindowHeight);
                    return (lhsAdjacencySize - rhsAdjacencySize);

                });
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

            if (direction == CardinalNavigationConstants.UP || direction == CardinalNavigationConstants.DOWN)
            {
                filterFunction = (win) =>
                {

                    var activeWindowXCoordinate = m_activeWindow.coordinates.x;
                    var activeWindowRightCorner = m_activeWindow.coordinates.x + m_activeWindow.coordinates.width;

                    var windowXCoordinate = win.coordinates.x;
                    var windowRightCorner = win.coordinates.x + win.coordinates.width;

                    return activeWindowXCoordinate <= windowRightCorner && windowXCoordinate <= activeWindowRightCorner;

                };
            }
            else if (direction == CardinalNavigationConstants.LEFT || direction == CardinalNavigationConstants.RIGHT)
            {
                filterFunction = (win) =>
                {
                    var activeWindowYCoordinate = m_activeWindow.coordinates.y;
                    var activeWindowYTop = m_activeWindow.coordinates.y + m_activeWindow.coordinates.height;

                    var windowYCoordinate = win.coordinates.y;
                    var windowYTop = win.coordinates.y + win.coordinates.height;

                    return activeWindowYCoordinate <= windowYTop && windowYCoordinate <= activeWindowYTop;

                };
            }

            m_ActiveWindows = m_ActiveWindows.Where(filterFunction).ToList();

        }

        private void removeWindowsNotAdjacent(char direction)
        {
            //todo: should not need abs
            Func<WindowControlAdapter, bool> filterFunction = (win) =>
            {
                return false;
            };
            if (direction == CardinalNavigationConstants.UP)
            {
                filterFunction = (win) =>
                {
                    return Math.Abs((win.coordinates.y + win.coordinates.height) - m_activeWindow.coordinates.y) < m_YDivide;
                };
            }
            else if (direction == CardinalNavigationConstants.DOWN)
            {
                filterFunction = (win) =>
                {
                    return Math.Abs(win.coordinates.y - (m_activeWindow.coordinates.y + m_activeWindow.coordinates.height)) < m_YDivide;
                };
            }
            else if (direction == CardinalNavigationConstants.LEFT)
            {
                filterFunction = (win) =>
                {
                    return Math.Abs((win.coordinates.x + win.coordinates.width) - m_activeWindow.coordinates.x) < m_XDivide;
                };

            }
            else if (direction == CardinalNavigationConstants.RIGHT)
            {
                filterFunction = (win) =>
                {
                    return Math.Abs((win.coordinates.x) - (m_activeWindow.coordinates.x + m_activeWindow.coordinates.width)) < m_XDivide;
                };
            }

            m_ActiveWindows = m_ActiveWindows.Where(filterFunction).ToList();

        }

        private void removeWindowsInWrongDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Func<WindowControlAdapter, bool> filterFunction = (win) =>
            {
                return false;
            };

            // we need to eliminate docked, invis wins before this point.

            if (direction == CardinalNavigationConstants.UP)
            {
                // do we want to jump 'across' the screen or no?
                // i should think nah, only if it's 'straight'above you. 
                // we'd need to calc if it is, but this'd also be an issue with all other transfers.

                filterFunction = (win) =>
                {

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
                    return win.coordinates.y - m_activeWindow.coordinates.y > 1;
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

        private void removeHiddenOrTabbedWindows()
        {
            m_ActiveWindows = m_ActiveWindows.Where((window) =>
            {
                return !(window.coordinates.x == 0 &&
                window.coordinates.y == 0 &&
                window.coordinates.width == 0 &&
                window.coordinates.height == 0);
            }).ToList();
        }

        /// <summary>
        /// Remove all points in the wrong direction, or that aren't bordering out
        /// active window.
        /// </summary>
        /// <param name="direction"></param>
        private void ReduceWindows(char direction)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                removeHiddenOrTabbedWindows();
                removeWindowsInWrongDirection(direction);
                removeWindowsNotAdjacent(direction);
                removeWindowsNotAligned(direction);
                sortByLargestAdjacency(direction);
                if (m_ActiveWindows.Count == 0)
                {
                    return;
                }
                m_ActiveWindows.First().ActivateWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reducing Windows Failed. " +
                                $"{CardinalNavigationConstants.GITHUB}\n{ex}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// swap the current active window for the one found here
        /// or do nothing if none is found.
        /// </summary>
        /// <param name="direction"></param>
        public void navigateInDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (m_activeWindow == null || m_ActiveWindows.Count == 0)
            {
                return;
            }
            ReduceWindows(direction);
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

