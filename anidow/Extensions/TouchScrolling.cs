using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Anidow.Extensions
{
    /// <summary>
    /// http://matthamilton.net/touchscrolling-for-scrollviewer
    /// Edited by Davut C.
    /// </summary>
    public class TouchScrolling : DependencyObject
    {
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled",
                typeof(bool),
                typeof(TouchScrolling),
                new PropertyMetadata(false, IsEnabledChanged));

        private static readonly Dictionary<object, MouseCapture> Captures = new Dictionary<object, MouseCapture>();

        private static void IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer target) return;

            if ((bool)e.NewValue)
            {
                if (target.IsLoaded)
                {
                    Target_Loaded(target, null);
                    return;
                }
                target.Loaded += Target_Loaded;
            }
            else
            {
                Target_Unloaded(target, null);
            }
        }

        private static void Target_Unloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Target Unloaded");

            if (sender is not ScrollViewer target) return;

            Captures.Remove(sender);

            target.Unloaded -= Target_Unloaded;
            target.PreviewMouseLeftButtonDown -= Target_PreviewMouseLeftButtonDown;
            target.PreviewMouseMove -= Target_PreviewMouseMove;

            target.PreviewMouseLeftButtonUp -= Target_PreviewMouseLeftButtonUp;
        }

        private static void Target_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Border
                && e.Source is ScrollViewer 
                || e.Source is TextBox 
                || e.Source is PasswordBox
                || e.OriginalSource.ToString() == "System.Windows.Controls.TextBoxView")
            {
                return;
            }

            if (sender is not ScrollViewer target) return;

            Captures[sender] = new MouseCapture
            {
                VerticalOffset = target.VerticalOffset,
                HorizontalOffset = target.HorizontalOffset,
                Point = e.GetPosition(target),
            };
        }

        private static void Target_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScrollViewer target) return;

            System.Diagnostics.Debug.WriteLine("Target Loaded");

            target.Unloaded += Target_Unloaded;
            target.PreviewMouseLeftButtonDown += Target_PreviewMouseLeftButtonDown;
            target.PreviewMouseMove += Target_PreviewMouseMove;
            target.PreviewMouseLeftButtonUp += Target_PreviewMouseLeftButtonUp;
        }

        private static void Target_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Border && e.Source is ScrollViewer) return;

            var target = sender as ScrollViewer;

            if (Captures.ContainsKey(sender))
            {
                Captures.Remove(sender);
            }

            target?.ReleaseMouseCapture();
        }

        private static void Target_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!Captures.ContainsKey(sender)) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                Captures.Remove(sender);
                return;
            }

            if (sender is not ScrollViewer target) return;

            var capture = Captures[sender];

            var point = e.GetPosition(target);

            var dy = point.Y - capture.Point.Y;
            var dx = point.X - capture.Point.X;
            if (Math.Abs(dy) > 5 || Math.Abs(dx) > 5)
            {
                target.CaptureMouse();
            }

            if (target.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                target.ScrollToVerticalOffset(capture.VerticalOffset - dy);
            }

            if (target.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                target.ScrollToHorizontalOffset(capture.HorizontalOffset - dx);
            }
        }

        private class MouseCapture
        {
            public double VerticalOffset { get; set; }
            public double HorizontalOffset { get; set; }
            public Point Point { get; set; }
        }
    }
}