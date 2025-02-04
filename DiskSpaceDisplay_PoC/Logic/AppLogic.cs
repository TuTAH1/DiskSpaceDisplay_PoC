using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;

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
	}
}
