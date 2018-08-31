// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary> Adorner used for previewing a dragged element. </summary>
    internal class PreviewAdorner : Adorner
    {
        #region private variables
        /// <summary> Element displayed. </summary>
        private readonly FrameworkElement child;
        /// <summary> Current offset. </summary>
        private Vector offset;
        #endregion

        #region public variables
        /// <summary> Gets or sets the current offset. </summary>
        public Vector Offset
        {
            get => offset;
            set
            {
                offset = value;
                (Parent as AdornerLayer)?.Update(AdornedElement);
            }
        }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="PreviewAdorner"/> class. </summary>
        /// <param name="adornedElement"> Adorned element. </param>
        /// <param name="presentationElement"> Element to be presented on the adorner layer. </param>
        /// <param name="opacity"> Opacity to apply to presentationElement when it is added to the adorner layer. </param>
        /// <returns> Documentation in progress... </returns>
        public PreviewAdorner(UIElement adornedElement, FrameworkElement presentationElement, double opacity)
            : base(adornedElement)
        {
            child = presentationElement;
            child.Opacity = opacity;
        }

        /// <summary> Initializes a new instance of the <see cref="PreviewAdorner"/> class. Takes a snapshot of the adorned element, which is then used to present the element on the adorner layer. </summary>
        /// <param name="adornedElement"> Adorned element. </param>
        /// <param name="opacity"> Opacity to apply to the element's snapshot. </param>
        /// <returns> Documentation in progress... </returns>
        public PreviewAdorner(UIElement adornedElement, double opacity)
            : base(adornedElement)
        {
            child = adornedElement.takeSnapshot();
            child.Opacity = opacity;
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>  
        protected override Size MeasureOverride(Size constraint)
        {
            child.Measure(constraint);
            return child.DesiredSize;
        }

        /// <inheritdoc/>  
        protected override Size ArrangeOverride(Size finalSize)
        {
            child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <inheritdoc/>  
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new IndexOutOfRangeException();

            return child;
        }

        /// <inheritdoc/>  
        protected override int VisualChildrenCount => 1;

        #endregion

        #region public methods
        /// <inheritdoc/>  
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var generalTransformGroup = new GeneralTransformGroup();

            // add base transformations
            generalTransformGroup.Children.Add(base.GetDesiredTransform(transform));

            // add a TranslateTransform taking our offset into account
            generalTransformGroup.Children.Add(new TranslateTransform(offset.X, offset.Y));

            return generalTransformGroup;
        }

        /// <summary> Attach adorner. </summary>
        /// <param name="adorner"> Stores the adorner instance. An instance is only 
        /// create if adorner is null on input. </param>
        /// <param name="adornedElement"> Function that provides the adorned element. 
        /// Only called when a new instance has to be created. </param>
        /// <param name="presentationElement"> Function that provides the element to be 
        /// presented on the adorner layer. Only called when a new instance 
        /// has to be created. If this function returns null, a snapshot of the adorned
        /// element will be taken. </param>
        /// <param name="opacity"> Opacity to apply to presentationElement before it is 
        /// added to the adorner layer. </param>
        public static void attach(ref PreviewAdorner adorner, Func<FrameworkElement> adornedElement, Func<FrameworkElement> presentationElement, double opacity)
        {
            if (adorner != null) return;

            adorner = presentationElement != null ? new PreviewAdorner(adornedElement(), presentationElement(), opacity) : new PreviewAdorner(adornedElement(), opacity);

            AdornerLayer.GetAdornerLayer(adorner.AdornedElement).Add(adorner);
        }

        /// <summary> Detach adorner instance.  </summary>
        /// <param name="adorner"> Documentation in progress... </param>
        public static void detach(ref PreviewAdorner adorner)
        {
            if (adorner == null) return;

            AdornerLayer.GetAdornerLayer(adorner.AdornedElement).Remove(adorner);
            adorner = null;
        }
        #endregion
    }
}
