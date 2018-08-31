// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows.Media;

[assembly: CLSCompliant(true)]
namespace Ptv.XServer.Controls.Map.Symbols
{
    /// <summary>
    /// Documentation in progress...
    /// </summary>
    public static class LightenColorExtension
    {
        /// <summary>
        ///   This method applies lighting to a color including keeping the transparency.
        ///   For instance, a color that has a lighting factor of 1 applies, appears at its original value.
        ///   A color with a lighting factor of 0.5 appears only half as bright as it was before.
        ///   A color with a lighting factor of 1.5 appears roughly twice as bright as before.
        ///   A color with a lightning factor of 2 appears white.
        /// </summary>
        /// <param name="originalColor"> Base color. </param>
        /// <param name="lightFactor">
        ///  Amount of light applied to the color.
        /// </param>
        /// <returns> Lit color. </returns>
        /// <remarks>
        ///   This routine is very fast. Even when using it in tight loops, it is not possible to 
        ///   measure a significant amount of time spent in this routine (always less than 1 millisecond). Originally
        ///   concerned about the performance of this, a caching mechanism was added, but that slowed things down
        ///   by 2 orders of magnitude.
        /// </remarks>
        public static Color Lighten(this Color originalColor, float lightFactor)
        {
            if (TransformationNotNeeded(lightFactor))
                return originalColor;

            if (RealBright(lightFactor))
                return Colors.White;

            if (ShouldDarken(lightFactor))
                return DarkenColor(originalColor, lightFactor);

            return LightenColor(originalColor, lightFactor);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="lightFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static bool TransformationNotNeeded(float lightFactor)
        {
            return lightFactor < 1.01f && lightFactor > 0.99f;
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="lightFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static bool RealBright(float lightFactor)
        {
            return lightFactor >= 2.0f;
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="lightFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static bool ShouldDarken(float lightFactor)
        {
            return lightFactor < 1.0f;
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="color"> Documentation in progress... </param>
        /// <param name="lightFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static Color DarkenColor(Color color, float lightFactor)
        {
            var red = (byte)(color.R * lightFactor);
            var green = (byte)(color.G * lightFactor);
            var blue = (byte)(color.B * lightFactor);

            return Color.FromArgb(color.A, red, green, blue);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="color"> Documentation in progress... </param>
        /// <param name="lightFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static Color LightenColor(Color color, float lightFactor)
        {
            // Lighten
            // We do this by approaching 256 for a light factor of 2.0f
            float fFactor2 = lightFactor;
            if (fFactor2 > 1.0f)
            {
                fFactor2 -= 1.0f;
            }

            var red = LightenColorComponent(color.R, fFactor2);
            var green = LightenColorComponent(color.G, fFactor2);
            var blue = LightenColorComponent(color.B, fFactor2);

            return Color.FromArgb(color.A, red, green, blue);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="colorComponent"> Documentation in progress... </param>
        /// <param name="fFactor"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static byte LightenColorComponent(byte colorComponent, float fFactor)
        {
            if (colorComponent == 0)
                return colorComponent;

            var inverse = 255 - colorComponent;
            colorComponent += (byte)(inverse * fFactor);

            return colorComponent < 255
                       ? colorComponent
                       : (byte)255;
        }
    }
}
