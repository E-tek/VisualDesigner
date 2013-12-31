﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Glass.Basics.Extensions;
using Glass.Design.Pcl;
using Glass.Design.Pcl.CanvasItem;
using Glass.Design.Pcl.Core;
using Glass.Design.Pcl.DesignSurface.VisualAids.Snapping;
using ImpromptuInterface;

namespace Glass.Design.Wpf.DesignSurface.VisualAids.Resize
{
    public sealed class ResizeControl : Control
    {
        public ISnappingEngine SnappingEngine { get; set; }

        static ResizeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeControl), new FrameworkPropertyMetadata(typeof(ResizeControl)));
        }

        public ResizeControl(CanvasItem itemToResize, IInputElement parent, ISnappingEngine snappingEngine)
        {
            SnappingEngine = snappingEngine;
            FrameOfReference = parent;
            CanvasItem = itemToResize;
        }

        private WindowsSizeCursorsThumbCursorConverter WindowsSizeCursorsThumbCursorConverter { get; set; }
        private WpfUIResizeOperationHandleConnector WpfUIResizeOperationHandleConnector { get; set; }

        #region CanvasItem

        public static readonly DependencyProperty CanvasItemProperty =
            DependencyProperty.Register("CanvasItem", typeof(ICanvasItem), typeof(ResizeControl),
                new FrameworkPropertyMetadata(null, OnCanvasItemChanged));

        public ICanvasItem CanvasItem
        {
            get { return (ICanvasItem)GetValue(CanvasItemProperty); }
            set { SetValue(CanvasItemProperty, value); }
        }

        private static void OnCanvasItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ResizeControl)d;
            var oldCanvasItem = (ICanvasItem)e.OldValue;
            var newCanvasItem = target.CanvasItem;
            target.OnCanvasItemChanged(oldCanvasItem, newCanvasItem);
        }

        private void OnCanvasItemChanged(ICanvasItem oldCanvasItem, ICanvasItem newCanvasItem)
        {
            if (IsLoaded)
            {
                RegisterHandles();
            }
            else
            {
                Loaded += OnLoaded;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            RegisterHandles();
            Loaded -= OnLoaded;
        }

        private void RegisterHandles()
        {
            WpfUIResizeOperationHandleConnector = new WpfUIResizeOperationHandleConnector(CanvasItem, FrameOfReference, SnappingEngine);
            WindowsSizeCursorsThumbCursorConverter = new WindowsSizeCursorsThumbCursorConverter();


            var thumbContainer = (UIElement)Template.FindName("PART_ThumbContainer", this);


            var enumerable = LogicalTreeHelper.GetChildren(thumbContainer);
            var logicalChildren = enumerable.OfType<FrameworkElement>();
            foreach (var logicalChild in logicalChildren)
            {
                var parentSize = ServiceLocator.CoreTypesFactory.CreateSize(ActualWidth, ActualHeight);
                var proportionalHandlePoint = GetProportionalHandlePoint(logicalChild, parentSize);
                WpfUIResizeOperationHandleConnector.RegisterHandler(logicalChild, proportionalHandlePoint);
                SetCursorToHandle(logicalChild);
            }
        }

        private void SetCursorToHandle(FrameworkElement handle)
        {
            var handleRect = handle.GetRectRelativeToParent().ActLike<IRect>();
            var parentRect = ServiceLocator.CoreTypesFactory.CreateRect(0, 0, ActualWidth, ActualHeight);
            handle.Cursor = WindowsSizeCursorsThumbCursorConverter.GetCursor(handleRect, parentRect);
        }

        private static IPoint GetProportionalHandlePoint(Visual logicalChild, ISize parentSize)
        {
            var rect = logicalChild.GetRectRelativeToParent().ActLike<IRect>();
            var proportionalHandlePoint = rect.GetHandlePoint(parentSize);
            return proportionalHandlePoint;
        }

        #endregion

        #region FrameOfReference

        public static readonly DependencyProperty FrameOfReferenceProperty =
            DependencyProperty.Register("FrameOfReference", typeof(IInputElement), typeof(ResizeControl),
                new FrameworkPropertyMetadata(null, OnFrameOfReferenceChanged));

        public IInputElement FrameOfReference
        {
            get { return (IInputElement)GetValue(FrameOfReferenceProperty); }
            set { SetValue(FrameOfReferenceProperty, value); }
        }

        private static void OnFrameOfReferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ResizeControl)d;
            var oldFrameOfReference = (IInputElement)e.OldValue;
            var newFrameOfReference = target.FrameOfReference;
            target.OnFrameOfReferenceChanged(oldFrameOfReference, newFrameOfReference);
        }

        protected void OnFrameOfReferenceChanged(IInputElement oldFrameOfReference, IInputElement newFrameOfReference)
        {
            if (IsLoaded)
            {
                RegisterHandles();
            }
            else
            {
                Loaded += OnLoaded;
            }
        }

        #endregion
    }
}