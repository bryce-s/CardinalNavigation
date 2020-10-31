using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CardinalNavigation
{
    class WindowControlAdapter
    {

        private IVsFrameView m_genericWindow;

        private Window m_dteWindow;

        public Window internalWindow { get => m_dteWindow; }

        private int m_Px, m_Py, m_Pcx, m_Pcy;
        private int m_screenLeft, m_screenTop, m_screenWidth, m_screenHeight;

        private Window m_Parent;

        public RectCoordinate coordinates
        {
            get
            {
                return this.GetScreenDisplayCoordinates();
            }
        }

        public bool stripSaveFileAsterix = false;

        /// <summary>
        /// constructor binds an IVsWindowFrame to a Dte.Window
        /// </summary>
        /// <param name="genericWindow"></param>
        /// <param name="dteWindow"></param>
        WindowControlAdapter(IVsFrameView genericWindow, Window dteWindow)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (genericWindow == null || dteWindow == null)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }

            m_genericWindow = genericWindow;
            m_dteWindow = dteWindow;

            GetWindowScreenCoordinates();

            Guid relativeTo;
            var framePos = new VSSETFRAMEPOS[1];
            m_genericWindow.GetFramePos(framePos, out relativeTo, out m_Px, out m_Py, out m_Pcx, out m_Pcy);

            m_Parent = m_dteWindow.LinkedWindowFrame;

        }

        /// <summary>
        /// Returns the dimensions of a window that's being rendered on the screen.
        /// </summary>
        private void GetWindowScreenCoordinates()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            m_genericWindow.GetWindowScreenRect(out m_screenLeft, out m_screenTop, out m_screenWidth, out m_screenHeight);
        }


        /// <summary>
        /// returns the active window from an enumerable of WindowControlAdapters
        /// </summary>
        /// <param name="activeWindow"></param>
        /// <param name="windows"></param>
        /// <returns></returns>
        public static WindowControlAdapter GetActiveWindowControlAdapter(EnvDTE.Window activeWindow, IEnumerable<WindowControlAdapter> windows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (activeWindow == null)
            {
                return null;
            }
            try
            {
                return windows?.Where((eachWindow) =>
                {
                    Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                    return UtilityMethods.CompareWindows(activeWindow, eachWindow.internalWindow);
                }).First();
            }
            catch (Exception ex)
            {
                if (ex is System.InvalidOperationException)
                {
                    MessageBox.Show($"Unable to pair active windows. {CardinalNavigationConstants.GithubMessage}\n" +
                                    $"Window:{activeWindow.Caption}\nException:{ex}\n{ex.StackTrace}");
                }
                throw;
            }

        }


        /// <summary>
        ///  returns an ienumerable to the children of our selected window's parent window. 
        /// </summary>
        /// <param name="genericWindows"></param>
        /// <param name="dteWindows"></param>
        /// <param name="activeWindow"></param>
        /// <returns></returns>
        public static IEnumerable<WindowControlAdapter> GetLinkedWindowControlAdapters(
            List<IVsFrameView> genericWindows,
            List<Window> dteWindows,
            EnvDTE.Window activeWindow
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                List<WindowControlAdapter> allWindows = GetWindowControlAdapters(genericWindows, dteWindows).ToList();

                List<EnvDTE.Window> parentWindows = UtilityMethods.GetLinkedWindowsList(activeWindow.LinkedWindowFrame, dteWindows);

                return allWindows.Where((eachActiveWindow) =>
                {
                    var internalWindow = eachActiveWindow.internalWindow;
                    foreach (var parentWindow in parentWindows)
                    {
                        if (UtilityMethods.CompareWindows(parentWindow, internalWindow))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Linking failed. Please open an issue on github\n" +
                    $"dte:{dteWindows?.Count.ToString()}\nwa:{genericWindows?.Count.ToString()}\n{ex}\n{ex.StackTrace}");
                throw;
            }
        }

        private static bool CompareReflection(object obj1, object obj2)
        {
            try
            {
                if (obj1 == null && obj2 == null)
                {
                    return true;
                }
                if (obj1 == null || obj2 == null)
                {
                    return false;
                }
                if (!obj1.GetType().Equals(obj2.GetType()))
                {
                    return false;
                }

                Type type = obj1.GetType();
                if (type.IsPrimitive || typeof(string).Equals(type))
                {
                    return obj1.Equals(obj2);
                }
                if (type.IsArray)
                {
                    Array first = obj1 as Array;
                    Array second = obj2 as Array;
                    var en = first.GetEnumerator();
                    int i = 0;
                    while (en.MoveNext())
                    {
                        if (!CompareReflection(en.Current, second.GetValue(i)))
                            return false;
                        i++;
                    }
                }
                else
                {
                    foreach (PropertyInfo pi in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                    {
                        var val = pi.GetValue(obj1);
                        var tval = pi.GetValue(obj2);
                        if (!CompareReflection(val, tval))
                            return false;
                    }
                    foreach (FieldInfo fi in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                    {
                        var val = fi.GetValue(obj1);
                        var tval = fi.GetValue(obj2);
                        if (!CompareReflection(val, tval))
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.TargetInvocationException || ex is System.Reflection.TargetParameterCountException)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks for duplicate window frames from either API.
        /// </summary>
        /// <param name="genericWindows"></param>
        /// <param name="dteWindows"></param>
        private static void CheckForDuplicateFrames(List<IVsFrameView> genericWindows, List<Window> dteWindows)
        {
            if (genericWindows.Count != genericWindows.DistinctBy(keySelector: (genericWindow) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return genericWindow.internalFrame;
                }).ToList().Count 
                ||
                dteWindows.Count != dteWindows.DistinctBy(keySelector: (dteWindow) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return dteWindow;
                }).ToList().Count)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }
        }

        /// <summary>
        /// checks that the intersection of the windows from our two APIs is equal.
        /// </summary>
        /// <param name="genericWindows"></param>
        /// <param name="dteWindows"></param>
        private static void CheckIntersection(List<IVsFrameView> genericWindows, List<Window> dteWindows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var intersection = genericWindows.Where(genericWindow =>
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                Window genericWindowDteWindow = VsShellUtilities.GetWindowObject(genericWindow);
                foreach (var dteWindow in dteWindows)
                {
                    if (UtilityMethods.CompareWindows(genericWindowDteWindow, dteWindow))
                    {
                        return true;
                    }
                }
                return false;
            });

            if (intersection.ToList().Count != genericWindows.Count)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }
        }

        private static void SortWindowsByName(List<IVsFrameView> genericWindows, List<Window> dteWindows)
        {
            genericWindows.Sort((lhsWindow, rhsWindow) => String.Compare(lhsWindow.GetWindowName(), rhsWindow.GetWindowName()));
            dteWindows.Sort((lhsWindow, rhsWindow) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return String.Compare(lhsWindow.Caption, rhsWindow.Caption);
            }
            );
        }


        private static void CheckPairedWindowsForErrors(List<IVsFrameView> genericWindows, List<Window> dteWindows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (genericWindows?.Count != dteWindows?.Count || dteWindows?.Count == 0)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }

            CheckForDuplicateFrames(genericWindows, dteWindows);
            CheckIntersection(genericWindows, dteWindows);
        }


        /// <summary>
        /// Returns an ienum to this class, bound to lists from the DTE and IVs shell api.
        /// </summary>
        /// <param name="genericWindows"></param>
        /// <param name="dteWindows"></param>
        /// <returns></returns>
        public static IEnumerable<WindowControlAdapter> GetWindowControlAdapters(List<IVsFrameView> genericWindows, List<Window> dteWindows)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // instead of pairing, we'll take straight from IVsUi. It seems more inclusive.

            foreach (var genericWindow in genericWindows)
            {
                yield return new WindowControlAdapter(genericWindow, VsShellUtilities.GetWindowObject(genericWindow));
            }
        }


        /// <summary>
        /// returns the name of this window.
        /// </summary>
        /// <returns></returns>
        public string GetWindowName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return m_genericWindow.GetWindowName();
        }

        /// <summary>
        /// returns if this window is eligible for selection, with no respect to its position.
        /// false if docked, minimized, etc.
        /// </summary>
        /// <returns></returns>
        public bool EligibleForActiviation()
        {
            return m_screenWidth == 0 && m_screenHeight == 0 && m_screenLeft == 0 && m_screenTop == 0;
        }

        // return is floating

        // return coordinates of parent window
        public RectCoordinate GetParentWindowDisplayCoordinates()
        {
            // if window floating return dte top and left
            // else return dte parent top and left
            throw new NotImplementedException();
        }

        /// <summary>
        /// returns the absolute screen position and dimensions of this window
        /// </summary>
        /// <returns></returns>
        public RectCoordinate GetScreenDisplayCoordinates()
        {
            return new RectCoordinate(m_screenLeft, m_screenTop, m_screenWidth, m_screenHeight);
        }


        /// <summary>
        /// activates the given window
        /// </summary>
        public void ActivateWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            m_dteWindow.Activate();
        }

        /// <summary>
        /// returns whether the window autohides
        /// </summary>
        /// <returns></returns>
        public bool AutoHides()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return m_dteWindow.AutoHides;
        }


    }
}

