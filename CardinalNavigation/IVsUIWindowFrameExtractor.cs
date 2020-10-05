using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using stdole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CardinalNavigation
{
    class IVsUIWindowFrameExtractor
    {


        private static IEnumerable<IVsFrame> extractFrames(IEnumWindowFrames frames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var frame = new IVsWindowFrame[1];
            int ok = VSConstants.S_OK;
            while (ok == VSConstants.S_OK)
            {
                uint fetched;
                ok = frames.Next(1, frame, out fetched);
                ErrorHandler.ThrowOnFailure(ok);
                if (fetched == 1)
                {
                    var framepos = new VSSETFRAMEPOS[1];
                    Guid guid = new Guid();
                    Int32 ux, uy, pcy, pcx;
                    frame[0].GetFramePos(framepos, out guid, out ux, out uy, out pcy, out pcx);
                    var frameType = new object();
                    frame[0].GetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, out frameType);
                    var isVisible = frame[0].IsVisible();
                    int onScreen;
                    var isOnScreen = frame[0].IsOnScreen(out onScreen);
                    yield return new IVsFrame(frame[0]);
                }
            }
        }


        /// <summary>
        /// returns an enumerable to both tool and document IVsWindowFrames
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static List<IVsFrame>getIVsWindowFramesEnumerator(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsUIShell uiShell = HelperMethods.getIVsUIShell(package);
            List<IVsFrame> genericFrames = new List<IVsFrame>();


            IEnumWindowFrames toolFramesEnum;
            ErrorHandler.ThrowOnFailure(uiShell.GetToolWindowEnum(out toolFramesEnum));
            genericFrames = extractFrames(toolFramesEnum).ToList();

            IEnumWindowFrames documentFramesEnum;
            ErrorHandler.ThrowOnFailure(uiShell.GetDocumentWindowEnum(out documentFramesEnum));
            genericFrames.AddRange(extractFrames(documentFramesEnum).ToList());

            return genericFrames;
        }
    }
}
