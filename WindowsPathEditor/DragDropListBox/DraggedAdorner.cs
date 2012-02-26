using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace DragDropListBox
{
	public class DraggedAdorner : Adorner
	{
		private ContentPresenter contentPresenter;
		private double left;
		private double top;
		private AdornerLayer adornerLayer;

		public DraggedAdorner(object dragDropData, DataTemplate dragDropTemplate, UIElement adornedElement, AdornerLayer adornerLayer)
			: base(adornedElement)
		{
			this.adornerLayer = adornerLayer;

			this.contentPresenter = new ContentPresenter();
			this.contentPresenter.Content = dragDropData;
			this.contentPresenter.ContentTemplate = dragDropTemplate;
			this.contentPresenter.Opacity = 0.7;

			this.adornerLayer.Add(this);
		}

		public void SetPosition(double left, double top)
		{
			// -1 and +13 align the dragged adorner with the dashed rectangle that shows up
			// near the mouse cursor when dragging.
			this.left = left - 1;
			this.top = top + 13;
			if (this.adornerLayer != null)
			{
				this.adornerLayer.Update(this.AdornedElement);
			}
		}

		protected override Size MeasureOverride(Size constraint)
		{
			this.contentPresenter.Measure(constraint);
			return this.contentPresenter.DesiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			this.contentPresenter.Arrange(new Rect(finalSize));
			return finalSize;
		}

		protected override Visual GetVisualChild(int index)
		{
			return this.contentPresenter;
		}

		protected override int VisualChildrenCount
		{
			get { return 1; }
		}

		public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
		{
			GeneralTransformGroup result = new GeneralTransformGroup();
			result.Children.Add(base.GetDesiredTransform(transform));
			result.Children.Add(new TranslateTransform(this.left, this.top));

			return result;
		}

		public void Detach()
		{
			this.adornerLayer.Remove(this);
		}

	}
}
