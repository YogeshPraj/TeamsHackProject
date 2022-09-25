using Emgu.CV;
using Emgu.CV.Structure;
using OpenScreen.Core.Screenshot.WinFeatures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TeamsHack;
using static Emgu.CV.Text.TextDetectorCNN;

namespace OpenScreen.Core.Screenshot
{
    /// <summary>
    /// Provides methods for creating screenshots.
    /// </summary>
    public static class Screenshot
    {
        /// <summary>
        /// Provides enumeration of screenshots.
        /// </summary>
        /// <param name="requiredResolution">Required screenshot resolution.</param>
        /// <param name="isDisplayCursor">Whether to display the cursor in screenshots.</param>
        /// <returns>Enumeration of screenshots.</returns>
        public static IEnumerable<Image> TakeSeriesOfScreenshots(Resolution.Resolutions requiredResolution,
            bool isDisplayCursor)
        {
            var screenSize = new Size(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);
            var rawImage = new Bitmap(screenSize.Width, screenSize.Height);
            var rawGraphics = Graphics.FromImage(rawImage);
            var screenshot = rawImage;
            var source = new Rectangle(0, 0, screenSize.Width, screenSize.Height);
            var result = screenshot;
            while (true)
            {
                var areas = Areas.GetAreas();

                //bottom to front
                areas.Sort();
                
                rawGraphics.CopyFromScreen(0, 0, 0, 0, screenSize);
                using (Image<Bgr, Byte> originalImage = rawImage.ToImage<Bgr, Byte>())
                {
                    using (var blurredImage = originalImage.SmoothBlur(20, 20))
                    {
                        screenshot = blurredImage.ToBitmap();
                    }

                    result = (Bitmap)screenshot.Clone();
                    using (Graphics grD = Graphics.FromImage(result))
                    {
                        //foreach (Area area in areas)
                        for (var i = 0; i< areas.Count; i++)
                        {
                            var area = areas[i];
                            if (area.IsShared)
                            {
                                grD.DrawImage(rawImage, area.GetRectangle(), area.GetRectangle(), GraphicsUnit.Pixel);
                            }
                            //else
                            //{
                            //    grD.DrawImage(screenshot, area.GetRectangle(), area.GetRectangle(), GraphicsUnit.Pixel);
                            //}
                        }
                    }
                }

                if (isDisplayCursor)
                {
                    AddCursorToScreenshot(rawGraphics, source);
                }

                yield return result;

            }
        }

        public static void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }


        /// <summary>
        /// Provides enumeration of screenshots of a specific application window.
        /// </summary>
        /// <param name="applicationName">The title of the main application window.</param>
        /// <param name="isDisplayCursor">Whether to display the cursor in screenshots.</param>
        /// <returns>Enumeration of screenshots of a specific application window.</returns>
        public static IEnumerable<Image> TakeSeriesOfScreenshotsAppWindow(string applicationName,
            bool isDisplayCursor)
        {
            var windowHandle = ApplicationWindow.FindWindow(null, applicationName);

            var screeRectangle = new Rectangle();

            while (true)
            {
                ApplicationWindow.GetWindowRect(windowHandle, ref screeRectangle);

                screeRectangle.Width -= screeRectangle.X;
                screeRectangle.Height -= screeRectangle.Y;

                var image = new Bitmap(screeRectangle.Width, screeRectangle.Height);

                var graphics = Graphics.FromImage(image);

                var hdc = graphics.GetHdc();

                if (!ApplicationWindow.PrintWindow(windowHandle, hdc,
                    ApplicationWindow.DrawAllWindow))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception($"An error occurred while creating a screenshot"
                        + $" of the application window. Error Number: {error}.");
                }

                graphics.ReleaseHdc(hdc);

                if (isDisplayCursor)
                {
                    AddCursorToScreenshot(graphics, screeRectangle);
                }

                yield return image;

                image.Dispose();
                graphics.Dispose();
            }
        }

        /// <summary>
        /// Adds a cursor to a screenshot.
        /// </summary>
        /// <param name="graphics">Drawing surface.</param>
        /// <param name="bounds">Screen bounds.</param>
        private static void AddCursorToScreenshot(Graphics graphics, Rectangle bounds)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            MouseCursor.CursorInfo pci;
            pci.cbSize = Marshal.SizeOf(typeof(MouseCursor.CursorInfo));

            if (!MouseCursor.GetCursorInfo(out pci))
            {
                return;
            }

            if (pci.flags != MouseCursor.CursorShowing)
            {
                return;
            }

            const int logicalWidth = 0;
            const int logicalHeight = 0;
            const int indexOfFrame = 0;

            MouseCursor.DrawIconEx(graphics.GetHdc(), pci.ptScreenPos.x - bounds.X,
                pci.ptScreenPos.y - bounds.Y, pci.hCursor, logicalWidth,
                logicalHeight, indexOfFrame, IntPtr.Zero, MouseCursor.DiNormal);

            graphics.ReleaseHdc();
        }
    }
}