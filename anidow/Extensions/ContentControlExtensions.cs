﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Anidow.Extensions;

public static class ContentControlExtensions
{
    public static readonly DependencyProperty ContentChangedAnimationProperty = DependencyProperty.RegisterAttached(
        "ContentChangedAnimation", typeof(Storyboard), typeof(ContentControlExtensions),
        new PropertyMetadata(default(Storyboard), ContentChangedAnimationPropertyChangedCallback));

    public static void SetContentChangedAnimation(DependencyObject element, Storyboard value)
    {
        element.SetValue(ContentChangedAnimationProperty, value);
    }

    public static Storyboard GetContentChangedAnimation(DependencyObject element) =>
        (Storyboard)element.GetValue(ContentChangedAnimationProperty);

    private static void ContentChangedAnimationPropertyChangedCallback(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        if (dependencyObject is not ContentControl contentControl)
        {
            throw new Exception("Can only be applied to a ContentControl");
        }

        var propertyDescriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty,
            typeof(ContentControl));

        propertyDescriptor.RemoveValueChanged(contentControl, ContentChangedHandler);
        propertyDescriptor.AddValueChanged(contentControl, ContentChangedHandler);
    }

    private static void ContentChangedHandler(object sender, EventArgs eventArgs)
    {
        var animateObject = (FrameworkElement)sender;
        var storyboard = GetContentChangedAnimation(animateObject);
        storyboard.Begin(animateObject);
    }
}