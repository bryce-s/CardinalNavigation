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


        private static IEnumerable<IVsFrameView> extractFrames(IEnumWindowFrames frames)
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
                    yield return new IVsFrameView(frame[0]);
                }
            }
        }


        /// <summary>
        /// returns an enumerable to both tool and document IVsWindowFrames
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static List<IVsFrameView>getIVsWindowFramesEnumerator(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsUIShell uiShell = UtilityMethods.getIVsUIShell(package);
            List<IVsFrameView> genericFrames = new List<IVsFrameView>();


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

