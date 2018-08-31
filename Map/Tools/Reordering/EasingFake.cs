// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;


namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary> The .NET version 3.5 does not support easing. Therefore, we implement basic easing support in here.
    /// We also implement an own DoubleAnimation providing easing. There's a compiler switch, FAKE_EASING, with which
    /// we can assure to use to framework classes starting with .NET 4.0. </summary>
    internal interface IEasingFunction
    {
        /// <summary> Documentation in progress... </summary>
        /// <param name="f"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        double Ease(double f);
    }

    /// <summary> Documentation in progress... </summary>
    internal enum EasingMode
    {
        /// <summary> Documentation in progress... </summary>
        EaseOut,
        /// <summary> Documentation in progress... </summary>
        EaseIn
    }

    /// <summary> Documentation in progress... </summary>
    internal class DoubleAnimation : System.Windows.Media.Animation.DoubleAnimationBase
    {
        #region public variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double From
        {
            get => (double)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double To
        {
            get => (double)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }
        #endregion

        #region dependency properties
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty FromProperty;
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty ToProperty;
        /// <summary> Documentation in progress... </summary>
        public static readonly DependencyProperty EasingFunctionProperty;
        #endregion

        #region constructor
        /// <summary> Initializes static members of the <see cref="DoubleAnimation"/> class. </summary>
        static DoubleAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(double),
                typeof(DoubleAnimation));

            ToProperty = DependencyProperty.Register("To", typeof(double), 
                typeof(DoubleAnimation));

            EasingFunctionProperty = DependencyProperty.Register("EasingFunction", typeof(IEasingFunction),
                typeof(DoubleAnimation));
        }

        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override Freezable CreateInstanceCore()
        {
            return new DoubleAnimation();
        }

        /// <inheritdoc/>
        protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock)
        {
            double f = animationClock.CurrentProgress.Value;

            if (EasingFunction != null)
                f = EasingFunction.Ease(f);

            double min = Math.Min(From, To), max = To;

            if (To == min)
            {
                max = From;
                f = 1 - f;
            }

            return f * (max - min) + min;
        }
        #endregion
    }

    /// <summary> Documentation in progress... </summary>
    internal class CircleEase : IEasingFunction
    {
        #region public variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        public EasingMode EasingMode
        {
            get;
            set;
        }
        #endregion

        #region public methods
        /// <summary> Documentation in progress... </summary>
        /// <param name="f"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public double Ease(double f)
        {
            f = f <= 0 ? 0 : f >= 1 ? 1 : f;
            switch (EasingMode)
            {
                case EasingMode.EaseIn: return 1 - Math.Sqrt(1 - f*f);
                case EasingMode.EaseOut: return Math.Sqrt(f * (2 - f));
                default: return f;
            }
        }
        #endregion
    }
}
