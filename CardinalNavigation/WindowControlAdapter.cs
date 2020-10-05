﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardinalNavigation
{
    class WindowControlAdapter
    {
        private IVsFrame m_genericWindow;
        private Window m_dteWindow;

        private int m_Px, m_Py, m_Pcx, m_Pcy;
        private int m_screenLeft, m_screenTop, m_screenWidth, m_screenHeight;

        private Window m_Parent;


        WindowControlAdapter(IVsFrame genericWindow, Window dteWindow)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (genericWindow == null || dteWindow == null)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }
            m_genericWindow = genericWindow;
            m_dteWindow = dteWindow;

            m_genericWindow.GetWindowScreenRect(out m_screenLeft, out m_screenTop, out m_screenWidth, out m_screenHeight);

            Guid relativeTo;
            var framePos = new VSSETFRAMEPOS[1];
            m_genericWindow.GetFramePos(framePos, out relativeTo, out m_Px, out m_Py, out m_Pcx, out m_Pcy);

            m_Parent = m_dteWindow.LinkedWindowFrame;

        }

        /// <summary>
        /// Returns an ienum to this class, bound to lists from the DTE and IVs shell api.
        /// </summary>
        /// <param name="genericWindows"></param>
        /// <param name="dteWindows"></param>
        /// <returns></returns>
        public static IEnumerable<WindowControlAdapter> GetWindowControlAdapters(List<IVsFrame> genericWindows, List<Window> dteWindows)
        {
            if (genericWindows?.Count != dteWindows?.Count || dteWindows?.Count == 0)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_FAIL);
            }
            // todo: need logic to pair windows here, they won't necessarilyh be in the correct order.
            for (var i = 0; i < genericWindows.Count; i++)
            {
                yield return new WindowControlAdapter(genericWindows[i], dteWindows[i]);
            }
        }

        /// <summary>
        /// returns if this window is eligible for selection, with no respect to its position.
        /// false if docked, minimized, etc.
        /// </summary>
        /// <returns></returns>
        public bool eligibleForSelection()
        {
            // need the parent IVsFrame also.
            return true;
        }

        // return is floating

        // return coordinates of parent window

        // return coordinates of screen display
        public Coordinate getScreenDisplayCoordinates()
        {
            return new Coordinate(m_screenLeft, m_screenTop, m_screenWidth, m_screenHeight);
        }

        // activate window


        // close window




    }
}