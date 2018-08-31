// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows.Media.Animation;
using System.Windows;


namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary>
    /// <para>
    /// Animation class for animating GridLength properties, taken from http://www.codeproject.com/KB/WPF/GridLengthAnimation.aspx.
    /// </para>
    /// <para>
    /// Modifications:
    /// - added an easing function
    /// </para>
    /// </summary>
    internal class GridLengthAnimation : AnimationTimeline
    {
        #region public variables
        /// <inheritdoc/>
        public override Type TargetPropertyType => typeof(GridLength);

        /// <summary> Gets or sets Documentation in progress... </summary>
        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }
        #endregion

        #region dependecy properties
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty FromProperty;
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty ToProperty;
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty EasingFunctionProperty;
        #endregion

        #region constructor
        /// <summary> Initializes static members of the <see cref="GridLengthAnimation"/> class. </summary>
        static GridLengthAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(GridLength),
                typeof(GridLengthAnimation));

            ToProperty = DependencyProperty.Register("To", typeof(GridLength), 
                typeof(GridLengthAnimation));

            EasingFunctionProperty = DependencyProperty.Register("EasingFunction", typeof(IEasingFunction),
                typeof(GridLengthAnimation));
        }

        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }
        #endregion

        #region public methods
        /// <inheritdoc/>
        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            // fail on GridUnitType != GridUnitType.Pixel, just return "To"
            if (!From.IsAbsolute || !To.IsAbsolute)
                return To;

            double f = animationClock.CurrentProgress.Value;

            if (EasingFunction != null)
                f = EasingFunction.Ease(f);

            double min = Math.Min(From.Value, To.Value), max = To.Value;

            if (To.Value == min)
            {
                max = From.Value;
                f = 1 - f;
            }

            return new GridLength(f * (max - min) + min, GridUnitType.Pixel);
        }
        #endregion
    }
}
