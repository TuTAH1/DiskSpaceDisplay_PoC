using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DiskSpaceDisplay_PoC.Logic
{
	public static class AppLogic
	{
			//: For debug purposes
		public static void SynchronouslyRedraw(this UIElement uiElement) {
			uiElement.InvalidateVisual();
			Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait();
			Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => { })).Wait();
		}

		public static void ApplySquareGradient(Grid grid, Color centerColor, Color edgeColor, int borderSize = 20)
		{
			grid.UpdateLayout();
			var shadow = new Border
			{
				Width = grid.Width - borderSize,
				Height = grid.Height - borderSize,
				Background = new SolidColorBrush(centerColor),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Effect = new BlurEffect
				{
					Radius = borderSize / 2.0 // Чем больше, тем мягче градиент
				}
			};

			grid.Children.Clear();
			grid.Children.Add(shadow);
		}
	}
}
