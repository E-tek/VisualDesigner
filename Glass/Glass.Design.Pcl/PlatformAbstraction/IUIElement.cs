﻿using Glass.Design.Pcl.Canvas;
using Glass.Design.Pcl.Core;

namespace Glass.Design.Pcl.PlatformAbstraction
{
    public interface IUIElement : IUserInputReceiver, ICanvasItem
    {
        void AddAdorner(IAdorner adorner);
        bool IsVisible { get; set; }
        bool IsHitTestVisible { get; set; }
    }


    public delegate void PointingManipulationEventHandler(object sender, PointingManipulationEventArgs args);

    public class PointingManipulationEventArgs
    {
        public IPoint Point { get; set; }
    }
}