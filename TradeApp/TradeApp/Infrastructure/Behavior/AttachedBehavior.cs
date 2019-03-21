using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TradeApp.Infrastructure.Behavior
{
    /// <summary>
    /// AttachedBehavior Class
    /// </summary>
    public static class AttachedBehavior
    {
        /// <summary>
        /// Attach a Behavior of type IBehavior
        /// </summary>
        public static readonly DependencyProperty BehaviorProperty = DependencyProperty.RegisterAttached("Behavior", typeof(IBehavior), typeof(AttachedBehavior), new UIPropertyMetadata(null));

        /// <summary>
        /// Is Enabled, when set to true, fires Behaviors OnEnabled Method
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(AttachedBehavior), new UIPropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Handles IsEnabledChanged
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            var behavior = GetBehavior(d);
            if (behavior == null)
                return;
            if ((bool)e.NewValue)
                behavior.OnEnabled(d);
            else
                behavior.OnDisabling(d);
        }

        /// <summary>
        /// Gets the Behavior from the attached object
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        public static IBehavior GetBehavior(DependencyObject dependencyObject)
        {
            return (IBehavior)dependencyObject.GetValue(BehaviorProperty);
        }

        /// <summary>
        /// Sets the Behavior to an attached object
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="value"></param>
        public static void SetBehavior(DependencyObject dependencyObject, IBehavior value)
        {
            dependencyObject.SetValue(BehaviorProperty, value);
        }

        /// <summary>
        /// Gets IsEnabled from the attached object
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        public static bool GetIsEnabled(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets the IsEnabled property to an attached object
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="value"></param>
        public static void SetIsEnabled(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsEnabledProperty, value);
        }

    }
}
