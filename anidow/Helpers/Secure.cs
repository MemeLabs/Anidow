using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Anidow.Helpers
{
    /// <summary>
    ///     Source: https://github.com/canton7/Stylet/blob/master/Samples/Stylet.Samples.ModelValidation/Xaml/Secure.cs
    /// </summary>
    public static class Secure
    {
        // Using a DependencyProperty as the backing store for PasswordInitialized.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty PasswordInitializedProperty =
            DependencyProperty.RegisterAttached("PasswordInitialized", typeof(bool), typeof(Secure),
                new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for SettingPassword.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty SettingPasswordProperty =
            DependencyProperty.RegisterAttached("SettingPassword", typeof(bool), typeof(Secure),
                new PropertyMetadata(false));

        // We play a trick here. If we set the initial value to something, it'll be set to something else when the binding kicks in,
        // and HandleBoundPasswordChanged will be called, which allows us to set up our event subscription.
        // If the binding sets us to a value which we already are, then this doesn't happen. Therefore start with a value that's
        // definitely unique.
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(Secure),
                new FrameworkPropertyMetadata(Guid.NewGuid().ToString(), HandleBoundPasswordChanged)
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // Match the default on Binding
                });

        // Using a DependencyProperty as the backing store for SecurePasswordInitialized.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty SecurePasswordInitializedProperty =
            DependencyProperty.RegisterAttached("SecurePasswordInitialized", typeof(bool), typeof(Secure),
                new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for SettingSecurePassword.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty SettingSecurePasswordProperty =
            DependencyProperty.RegisterAttached("SettingSecurePassword", typeof(bool), typeof(Secure),
                new PropertyMetadata(false));

        // Similarly, we'll be set to something which isn't this exact instance of SecureString when the binding kicks in
        public static readonly DependencyProperty SecurePasswordProperty =
            DependencyProperty.RegisterAttached("SecurePassword", typeof(SecureString), typeof(Secure),
                new FrameworkPropertyMetadata(new SecureString(), HandleBoundSecurePasswordChanged)
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

        private static bool GetPasswordInitialized(DependencyObject obj) =>
            (bool) obj.GetValue(PasswordInitializedProperty);

        private static void SetPasswordInitialized(DependencyObject obj, bool value)
        {
            obj.SetValue(PasswordInitializedProperty, value);
        }


        private static bool GetSettingPassword(DependencyObject obj) => (bool) obj.GetValue(SettingPasswordProperty);

        private static void SetSettingPassword(DependencyObject obj, bool value)
        {
            obj.SetValue(SettingPasswordProperty, value);
        }

        public static string GetPassword(DependencyObject obj) => (string) obj.GetValue(PasswordProperty);

        public static void SetPassword(DependencyObject obj, string value)
        {
            obj.SetValue(PasswordProperty, value);
        }


        private static void HandleBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = (PasswordBox) dp;
            if (passwordBox == null)
            {
                return;
            }

            if (GetSettingPassword(passwordBox))
            {
                return;
            }

            // If this is the initial set
            if (!GetPasswordInitialized(passwordBox))
            {
                SetPasswordInitialized(passwordBox, true);
                passwordBox.PasswordChanged += HandlePasswordChanged;
            }

            passwordBox.Password = e.NewValue as string ?? string.Empty;
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox) sender;
            SetSettingPassword(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetSettingPassword(passwordBox, false);
        }


        private static bool GetSecurePasswordInitialized(DependencyObject obj) =>
            (bool) obj.GetValue(SecurePasswordInitializedProperty);

        private static void SetSecurePasswordInitialized(DependencyObject obj, bool value)
        {
            obj.SetValue(SecurePasswordInitializedProperty, value);
        }

        private static bool GetSettingSecurePassword(DependencyObject obj) =>
            (bool) obj.GetValue(SettingSecurePasswordProperty);

        private static void SetSettingSecurePassword(DependencyObject obj, bool value)
        {
            obj.SetValue(SettingSecurePasswordProperty, value);
        }


        public static SecureString GetSecurePassword(DependencyObject obj) =>
            (SecureString) obj.GetValue(SecurePasswordProperty);

        public static void SetSecurePassword(DependencyObject obj, SecureString value)
        {
            obj.SetValue(SecurePasswordProperty, value);
        }


        private static void HandleBoundSecurePasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = (PasswordBox) dp;
            if (passwordBox == null)
            {
                return;
            }

            if (GetSettingSecurePassword(passwordBox))
            {
                return;
            }

            if (!GetSecurePasswordInitialized(passwordBox))
            {
                passwordBox.PasswordChanged += HandleSecurePasswordChanged;
                SetSecurePasswordInitialized(passwordBox, true);
            }
        }

        private static void HandleSecurePasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox) sender;
            SetSettingSecurePassword(passwordBox, true);
            SetSecurePassword(passwordBox, passwordBox.SecurePassword);
            SetSettingSecurePassword(passwordBox, false);
        }
    }
}