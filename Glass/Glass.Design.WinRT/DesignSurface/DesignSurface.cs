﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Glass.Design.Pcl.Canvas;
using Glass.Design.Pcl.Core;
using Glass.Design.Pcl.DesignSurface;
using Glass.Design.Pcl.DesignSurface.VisualAids.Selection;
using Glass.Design.Pcl.PlatformAbstraction;
using PostSharp.Patterns.Model;
using FoundationPoint = Windows.Foundation.Point;
using SelectionMode = Glass.Design.Pcl.DesignSurface.VisualAids.Selection.SelectionMode;

namespace Glass.Design.WinRT.DesignSurface
{
    [NotifyPropertyChanged]
    public sealed class DesignSurface : ItemsControl, IDesignSurface
    {
        public static readonly DependencyProperty CanvasDocumentProperty = DependencyProperty.Register("CanvasDocument",
            typeof(ICanvasItemContainer), typeof(DesignSurface), new PropertyMetadata(null, OnCanvasDocumentChanged));

        public DesignSurface()
        {
            DefaultStyleKey = typeof(DesignSurface);
            SelectedItems = new ObservableCollection<ICanvasItem>();

            PointerPressed += OnPointerPressed;
            ((INotifyCollectionChanged)SelectedItems).CollectionChanged += OnCollectionChanged;
            DesignAidsProvider = new DesignAidsProvider(this);
            SelectionHandler = new SelectionHandler(this);
            CommandHandler = new DesignSurfaceCommandHandler(this, this);

            PopupsDictionary = new Dictionary<IAdorner, Popup>();

            Children = new CanvasItemCollection();
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }       

        private DesignSurfaceCommandHandler CommandHandler { get; set; }


        private SelectionHandler SelectionHandler { get; set; }

        private DesignAidsProvider DesignAidsProvider { get; set; }


        [IgnoreAutoChangeNotification]
        public ICanvasItemContainer CanvasDocument
        {
            get { return (ICanvasItemContainer)GetValue(CanvasDocumentProperty); }
            set { SetValue(CanvasDocumentProperty, value); }
        }



        public event EventHandler<object> ItemSpecified;

        public event EventHandler SelectionCleared;

        public ICommand GroupCommand { get; private set; }

        public void UnselectAll()
        {
            ClearSelectionPopups();
            SelectedItems.Clear();
        }

        public event FingerManipulationEventHandler FingerDown;

        public event FingerManipulationEventHandler FingerMove;

        public event FingerManipulationEventHandler FingerUp;

        public void CaptureInput()
        {
            //CapturePointer(new Pointer());
        }

        public void ReleaseInput()
        {
            //ReleasePointerCaptures();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public double GetCoordinate(CoordinatePart part)
        {
            throw new NotImplementedException();
        }

        void ICoordinate.SetCoordinate(CoordinatePart part, double value)
        {
            throw new NotImplementedException();
        }

        public double Left { get; set; }
        public double Top { get; set; }

        public CanvasItemCollection Children { get; private set; }

        public double Right { get; private set; }
        public double Bottom { get; private set; }
        public ICanvasItemContainer Parent { get; private set; }

        public void AddAdorner(IAdorner adorner)
        {
            var popup = new Popup();

            var coreInstance = (UIElement)adorner.GetCoreInstance();

            popup.Child = coreInstance;

            popup.HorizontalOffset = adorner.Left;
            popup.VerticalOffset = adorner.Top;
            popup.IsOpen = true;

            PopupsDictionary.Add(adorner, popup);
        }

        public void RemoveAdorner(IAdorner adorner)
        {
            var popup = PopupsDictionary[adorner];
            popup.IsOpen = false;
            PopupsDictionary.Remove(adorner);
        }

        bool IUIElement.IsVisible { get; set; }

        public object GetCoreInstance()
        {
            return this;
        }

        private static void OnCanvasDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var designSurface = ((DesignSurface)d);
            if (e.NewValue != null)
            {
                designSurface.ItemsSource = ((ICanvasItemContainer)e.NewValue).Children;
            }
            else
            {
                designSurface.ItemsSource = null;
            }
        }

        private void OnCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            OnSelectionChanged(notifyCollectionChangedEventArgs);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            RaiseNoneSpecified();
        }

        private void OnSelectionChanged(NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var newItems = notifyCollectionChangedEventArgs.NewItems;
            var removedItems = notifyCollectionChangedEventArgs.OldItems;

            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearSelectionPopups();
            }

            else if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ICanvasItem newItem in newItems)
                {
                    DesignAidsProvider.AddItemToSelection(newItem);
                }
            }
            else if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ICanvasItem removedItem in removedItems)
                {
                    DesignAidsProvider.RemoveItemFromSelection(removedItem);
                }
            }
        }



        private void ContainerOnLeftButtonDown(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            var item = ItemContainerGenerator.ItemFromContainer((DependencyObject)sender);
            RaiseItemSpecified(item);
            pointerRoutedEventArgs.Handled = true;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var designerItem = (CanvasItemControl)element;
            designerItem.PointerPressed += ContainerOnLeftButtonDown;
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            var designerItem = (CanvasItemControl)element;
            designerItem.PointerPressed -= ContainerOnLeftButtonDown;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is CanvasItemControl;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new CanvasItemControl();
        }

        private void RaiseItemSpecified(object e)
        {
            SelectedItem = e;

            var handler = ItemSpecified;
            if (handler != null) handler(this, e);
        }

        private void RaiseNoneSpecified()
        {
            SelectedItem = null;

            var handler = SelectionCleared;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnFingerDown(FingerManipulationEventArgs args)
        {
            var handler = FingerDown;
            if (handler != null) handler(this, args);
        }

        private void OnFingerMove(FingerManipulationEventArgs args)
        {
            var handler = FingerMove;
            if (handler != null) handler(this, args);
        }

        private void OnFingerUp(FingerManipulationEventArgs args)
        {
            var handler = FingerUp;
            if (handler != null) handler(this, args);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                RaisePropertyChanged(propertyName);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region PlaneOperationMode

        public static readonly DependencyProperty PlaneOperationModeProperty =
            DependencyProperty.Register("PlanePlaneOperationMode", typeof(PlaneOperation), typeof(DesignSurface),
                new PropertyMetadata(PlaneOperation.Resize,
                    OnOperationModeChanged));

        private readonly DesignSurfaceCommandHandler designSurfaceCommandHandler;

        private ICanvasItem _rootCanvasItem;


        [IgnoreAutoChangeNotification]
        public PlaneOperation PlaneOperationMode
        {
            get { return (PlaneOperation)GetValue(PlaneOperationModeProperty); }
            set { SetValue(PlaneOperationModeProperty, value); }
        }

        private static void OnOperationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (DesignSurface)d;
            var oldOperationMode = (PlaneOperation)e.OldValue;
            var newOperationMode = target.PlaneOperationMode;
            target.OnOperationModeChanged(oldOperationMode, newOperationMode);
        }

        private void OnOperationModeChanged(PlaneOperation oldOperationMode, PlaneOperation newOperationMode)
        {
            DesignAidsProvider.PlaneOperation = newOperationMode;
        }

        #endregion



        //{
        //    base.OnPreviewMouseLeftButtonUp(e);

        //    var point = e.GetPosition(null);
        //    var pclPoint = Mapper.Map<Point>(point);
        //    OnFingerUp(new FingerManipulationEventArgs());
        //}

        private CanvasItemCollection children;
        public object SelectedItem { get; private set; }
        private Dictionary<IAdorner, Popup> PopupsDictionary { get; set; }
        //protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)

        public IList SelectedItems { get; private set; }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);

            var currentPoint = e.GetCurrentPoint(this);
            var point = new Point(currentPoint.Position.X, currentPoint.Position.Y);

            var args = new FingerManipulationEventArgs { Point = point, Handled = true };            

            OnFingerDown(args);
        }

        private void ClearSelectionPopups()
        {
            foreach (ICanvasItem item in SelectedItems)
            {
                DesignAidsProvider.RemoveItemFromSelection(item);
            }
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
        {

            if (keyRoutedEventArgs.Key == VirtualKey.Control && keyRoutedEventArgs.KeyStatus.WasKeyDown)
            {
                SelectionHandler.SelectionMode = SelectionMode.Add;
            }
        }

        private void OnKeyUp(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
        {
            if (keyRoutedEventArgs.Key == VirtualKey.Control && keyRoutedEventArgs.KeyStatus.IsKeyReleased)
            {
                SelectionHandler.SelectionMode = SelectionMode.Direct;
            }

        }    
    }
}