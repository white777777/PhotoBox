﻿using System.Windows;
using System.Windows.Media;

namespace ImageMaker.CommonView.Helpers
{
    public static class WPFControlHelpers
    {
        public static T GetChildOfType<T>(this DependencyObject depObj)
            where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}
