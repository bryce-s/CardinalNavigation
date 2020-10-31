using System;
using System.CodeDom;
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

            this.CheckDte(package);
            m_IVsFrames = IVsUIWindowFrameExtractor.GetIVsWindowFramesEnumerator(package);

            DTE dteService = UtilityMethods.GetDTE(package);

            SetWindowDivideSelectionSizes();

            m_ActiveWindows = WindowControlAdapter.GetLinkedWindowControlAdapters(m_IVsFrames,
                UtilityMethods.GetWindowsList(dteService.Windows),
                dteService.ActiveWindow).
                ToList();

            if (dteService.ActiveWindow == null)
            {
               // gracefully exit
            }

            m_activeWindow = WindowControlAdapter.GetActiveWindowControlAdapter(dteService.ActiveWindow, m_ActiveWindows);
        }

        /// <summary>
        /// infer dpi for gap detection; i.e. the case where we're trying to move up
        /// on to a window that's tabbed, but there's another window with a smaller
        /// border that's technically closer.
        /// </summary>
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


        private void CheckDte(AsyncPackage package)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                DTE myDTE = UtilityMethods.GetDTE(package);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to get DTE {CardinalNavigationConstants.GithubMessage}{ex.StackTrace}");
            }
        }


        /// <summary>
        /// Compute the length of these two window's adjacency
        /// </summary>
        /// <param name="activeAxis"></param>
        /// <param name="activeHeightOrWidth"></param>
        /// <param name="windowAxis"></param>
        /// <param name="windowHeightOrWidth"></param>
        /// <returns></returns>
        private static int AdjacencySize(int activeAxis, int activeHeightOrWidth, int windowAxis, int windowHeightOrWidth)
        {
            return System.Linq.Enumerable.Range(activeAxis, activeAxis + activeHeightOrWidth).Intersect(
                System.Linq.Enumerable.Range(windowAxis, windowAxis + windowHeightOrWidth)
                ).Count();
        }

        #region Filter Windows
        /// <summary>
        /// sort active windows by adjacency size
        /// </summary>
        /// <param name="direction"></param>
        private void SortByLargestAdjacency(char direction)
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


        /// <summary>
        /// remove windows that are out of alignment with the active window, regardless of distance
        /// </summary>
        /// <param name="direction"></param>
        private void RemoveWindowsNotAligned(char direction)
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

        /// <summary>
        /// remove windows that aren't adjacent to the active window
        /// </summary>
        /// <param name="direction"></param>
        private void RemoveWindowsNotAdjacent(char direction)
        {
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

        /// <summary>
        /// Remove all but the closest window + computed DPI divide (tie inclusive)
        /// </summary>
        /// <param name="direction"></param>
        private void RemoveWindowsByClosestAdjacency(char direction)
        {

            ThreadHelper.ThrowIfNotOnUIThread();

            RemoveWindowsInWrongDirection(direction);
            RemoveWindowsNotAligned(direction);

            Func<WindowControlAdapter, int> distanceFunction = (eachWindow) =>
            {
                return 0;
            };

            if (direction == CardinalNavigationConstants.UP)
            {
                distanceFunction = (eachWindow) => 
                {
                    return m_activeWindow.coordinates.y - (eachWindow.coordinates.y + eachWindow.coordinates.height); 
                };
            }

            if (direction == CardinalNavigationConstants.DOWN)
            {
                distanceFunction = (eachWindow) =>
                {
                    return eachWindow.coordinates.y - (m_activeWindow.coordinates.y + m_activeWindow.coordinates.height);
                };
            }

            if (direction == CardinalNavigationConstants.LEFT)
            {
                distanceFunction = (eachWindow) => 
                {
                    return m_activeWindow.coordinates.x - (eachWindow.coordinates.x + eachWindow.coordinates.width);
                };
            }

            if (direction == CardinalNavigationConstants.RIGHT)
            {
                distanceFunction = (eachWindow) => 
                {
                    return eachWindow.coordinates.x - (m_activeWindow.coordinates.x + m_activeWindow.coordinates.width); 
                };
            }

            var minDistance = int.MaxValue;

            // find min
            try
            {
                 minDistance = m_ActiveWindows.Min(distanceFunction);
            }
            catch (System.InvalidOperationException)
            {
                return;
            }
            var upperDistaneBound = minDistance +
                ((direction == CardinalNavigationConstants.UP || direction == CardinalNavigationConstants.DOWN) ? m_YDivide : m_XDivide );

            // filter
            m_ActiveWindows = m_ActiveWindows.Where((eachWindow) => 
            {
                var distance = distanceFunction(eachWindow);
                return distance >= minDistance && distance <= upperDistaneBound;
            }).ToList();

        }

        /// <summary>
        /// removes windows not found along the axis we're looking at
        /// </summary>
        /// <param name="direction"></param>
        private void RemoveWindowsInWrongDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Func<WindowControlAdapter, bool> filterFunction = (win) =>
            {
                return false;
            };


            if (direction == CardinalNavigationConstants.UP)
            {
                filterFunction = (win) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
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

        }

        private void RemoveHiddenOrTabbedWindows()
        {
            m_ActiveWindows = m_ActiveWindows.Where((window) =>
            {
                return !(window.coordinates.x == 0 &&
                window.coordinates.y == 0 &&
                window.coordinates.width == 0 &&
                window.coordinates.height == 0);
            }).ToList();
        }

        #endregion

        /// <summary>
        /// Remove all points in the wrong direction, or that aren't bordering out
        /// active window.
        /// </summary>
        /// <param name="direction"></param>
        private void ReduceWindowsAndSelectActive(char direction)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                RemoveHiddenOrTabbedWindows();
                RemoveWindowsInWrongDirection(direction);
                RemoveWindowsNotAligned(direction);

                RemoveWindowsByClosestAdjacency(direction);

                SortByLargestAdjacency(direction);

                if (m_ActiveWindows.Count == 0)
                {
                    return;
                }
                m_ActiveWindows.First().ActivateWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reducing Windows Failed. " +
                                $"{CardinalNavigationConstants.GithubMessage}\n{ex}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// swap the current active window for the one found here
        /// or do nothing if none is found.
        /// </summary>
        /// <param name="direction"></param>
        public void NavigateInDirection(char direction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (m_activeWindow.AutoHides() || m_activeWindow == null || m_ActiveWindows.Count == 0)
            {
                return;
            }
            ReduceWindowsAndSelectActive(direction);
        }


        /// <summary>
        /// activate the window passed as a para
        /// </summary>
        /// <param name="window"></param>
        private void ActivateWindow(EnvDTE.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            window.Activate();
        }

    }
}

