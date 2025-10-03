﻿using Aimmy2;
using Aimmy2.Class;
using Aimmy2.Theme;
using AimmyWPF.Class;
using InputLogic;
using Other;
using System.Windows;
using System.Windows.Threading;

namespace Visuality
{
    /// <summary>
    /// Interaction logic for SetAntiRecoil.xaml
    /// </summary>
    public partial class SetAntiRecoil : Window
    {
        private MainWindow MainWin { get; set; }
        private DispatcherTimer HoldDownTimer = new DispatcherTimer();
        private DateTime LastClickTime;
        private int FireRate;
        private int ChangingFireRate;

        public SetAntiRecoil(MainWindow MW)
        {
            InitializeComponent();

            //I kind of forgot this one was a thing, I honestly love its design, but i feel like the media will overwrite the window so subscribe!
            ThemeManager.ExcludeWindowFromBackground(this);

            MW.WindowState = WindowState.Minimized;

            MainWin = MW;

            BulletBorder.Opacity = 0;
            BulletBorder.Margin = new Thickness(0, 0, 0, -140);

            HoldDownTimer.Tick += HoldDownTimerTicker;
            HoldDownTimer.Interval = TimeSpan.FromMilliseconds(1);
            HoldDownTimer.Start();

            ChangingFireRate = (int)Dictionary.AntiRecoilSettings["Fire Rate"];

            // Initialize theme colors
            UpdateThemeColors();

            // Subscribe to theme changes
            ThemeManager.RegisterElement(this);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, System.Windows.Media.Color newColor)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateThemeColors();
            });
        }

        private void UpdateThemeColors()
        {
            // Update gradient colors
            TopGradientStop.Color = ThemeManager.ThemeColorDark;
            ThemeGradientStop.Color = ThemeManager.ThemeColorDark;
        }

        private void HoldDownTimerTicker(object? sender, EventArgs e)
        {
            if (InputBindingManager.IsHoldingBinding("Anti Recoil Keybind"))
            {
                GetReading();
                HoldDownTimer.Stop();
            }
        }

        private async void GetReading()
        {
            LastClickTime = DateTime.Now;
            while (InputBindingManager.IsHoldingBinding("Anti Recoil Keybind"))
            {
                await Task.Delay(1);
            }
            FireRate = (int)(DateTime.Now - LastClickTime).TotalMilliseconds;

            Animator.Fade(BulletBorder);
            Animator.ObjectShift(TimeSpan.FromMilliseconds(350), BulletBorder, BulletBorder.Margin, new Thickness(0, 0, 0, 100));

            UpdateFireRate();
        }

        private void BulletNumberTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (BulletBorder.Opacity == 1 && BulletBorder.Margin == new Thickness(0, 0, 0, 100))
            {
                UpdateFireRate();
            }
        }

        private void UpdateFireRate()
        {
            if (BulletNumberTextbox.Text != null && BulletNumberTextbox.Text.Any(char.IsDigit))
            {
                ChangingFireRate = (int)(FireRate / Convert.ToInt64(BulletNumberTextbox.Text));
            }
            else
            {
                ChangingFireRate = FireRate;
            }

            SettingLabel.Content = $"Fire Rate has been set to {ChangingFireRate}ms, please confirm to save it.";
        }

        private void ConfirmB_Click(object sender, RoutedEventArgs e)
        {
            Dictionary.AntiRecoilSettings["Fire Rate"] = ChangingFireRate;
            MainWin.uiManager.S_FireRate!.Slider.Value = ChangingFireRate;

            MainWin.WindowState = WindowState.Normal;

            LogManager.Log(LogManager.LogLevel.Info, $"The Fire Rate is set to {ChangingFireRate}ms", true);

            Close();
        }

        private void TryAgainB_Click(object sender, RoutedEventArgs e)
        {
            SettingLabel.Content = $"Press and hold the mouse button the bullet is removed.";

            Animator.FadeOut(BulletBorder);
            Animator.ObjectShift(TimeSpan.FromMilliseconds(350), BulletBorder, BulletBorder.Margin, new Thickness(0, 0, 0, -140));

            HoldDownTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unregister from theme manager
            ThemeManager.ThemeChanged -= OnThemeChanged;
            ThemeManager.UnregisterElement(this);
            base.OnClosed(e);
        }
    }
}