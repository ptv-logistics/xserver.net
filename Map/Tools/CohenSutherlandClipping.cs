// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary>
    /// <para>
    /// Cohen-Sutherland clipping - initially taken from 
    /// http://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland.
    /// </para>
    /// <para>
    /// Slightly modified to incorporate the .Net data types 
    /// System.Windows.Rect and System.Windows.Point.
    /// </para>
    /// </summary>
    public class CohenSutherlandClipping
    {
        #region private constants
        /// <summary> Documentation in progress... </summary>
        private const int INSIDE = 0; // 0000
        /// <summary> Documentation in progress... </summary>
        private const int LEFT = 1;   // 0001
        /// <summary> Documentation in progress... </summary>
        private const int RIGHT = 2;  // 0010
        /// <summary> Documentation in progress... </summary>
        private const int BOTTOM = 4; // 0100
        /// <summary> Documentation in progress... </summary>
        private const int TOP = 8;    // 1000
        #endregion

        #region private methods
        /// <summary> Compute the bit code for a point using the given clip rectangle. </summary>
        /// <param name="r"> Clip rectangle. </param>
        /// <param name="p"> Point to compute the bit code for. </param>
        /// <returns> The bit code. </returns>
        private static int ComputeOutCode(System.Windows.Rect r, System.Windows.Point p)
        {
            int code = INSIDE;                   // initialized as being inside of clip window

            if (r.Left - p.X > 1e-4)             // to the left of clip window
                code |= LEFT;
            else if (p.X - r.Right > 1e-4)        // to the right of clip window
                code |= RIGHT;
            if (r.Top - p.Y > 1e-4)              // below the clip window
                code |= BOTTOM;
            else if (p.Y - r.Bottom > 1e-4)      // above the clip window
                code |= TOP;

            return code;
        }
        #endregion

        #region public methods
        /// <summary> Cohen–Sutherland clipping algorithm; clips the line specified by p0 to p1
        /// against the clipping rectangle specified by clipRect. </summary>
        /// <param name="clipRect"> Clipping rectangle. Be sure that the rectangle 
        /// satisfies the conditions left &lt;= right and top &lt;= bottom. </param>
        /// <param name="p0"> Line start point. </param>
        /// <param name="p1"> Line end point. </param>            
        /// <returns> Boolean value showing whether the line is visible in the rectangle (true) or not (false). </returns>
        public static bool Clip(System.Windows.Rect clipRect, ref System.Windows.Point p0, ref System.Windows.Point p1)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            int outcode0 = ComputeOutCode(clipRect, p0);
            int outcode1 = ComputeOutCode(clipRect, p1);

            while (true)
            {
                if (0 == (outcode0 | outcode1))
                {
                    // logical or is 0 (both points inside). 
                    // Trivially accept and get out of loop
                    return true;
                }
                if (0 != (outcode0 & outcode1))
                {
                    // logical and is not 0 (both points outside, no part visible). 
                    // Trivially reject and get out of loop.
                    return false;
                }
                // failed both tests, so calculate the line segment to clip
                // from an outside point to an intersection with clip edge
                var p = new System.Windows.Point();

                // At least one endpoint is outside the clip rectangle; pick it.
                int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                // Now find the intersection point;
                // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)
                if (0 != (outcodeOut & TOP))
                {
                    // point is above the clip rectangle
                    p.X = p0.X + (p1.X - p0.X) * (clipRect.Bottom - p0.Y) / (p1.Y - p0.Y);
                    p.Y = clipRect.Bottom;
                }
                else if (0 != (outcodeOut & BOTTOM))
                {
                    // point is below the clip rectangle
                    p.X = p0.X + (p1.X - p0.X) * (clipRect.Top - p0.Y) / (p1.Y - p0.Y);
                    p.Y = clipRect.Top;
                }
                else if (0 != (outcodeOut & RIGHT))
                {
                    // point is to the right of clip rectangle
                    p.Y = p0.Y + (p1.Y - p0.Y) * (clipRect.Right - p0.X) / (p1.X - p0.X);
                    p.X = clipRect.Right;
                }
                else if (0 != (outcodeOut & LEFT))
                {
                    // point is to the left of clip rectangle
                    p.Y = p0.Y + (p1.Y - p0.Y) * (clipRect.Left - p0.X) / (p1.X - p0.X);
                    p.X = clipRect.Left;
                }

                // Now we move outside point to intersection point to clip
                // and get ready for next pass.

                if (outcodeOut == outcode0)
                    outcode0 = ComputeOutCode(clipRect, p0 = p);
                else
                    outcode1 = ComputeOutCode(clipRect, p1 = p);
            }
        }
        #endregion
    }
}
