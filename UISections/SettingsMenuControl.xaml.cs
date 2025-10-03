﻿using Aimmy2.AILogic;
using Aimmy2.Class;
using Aimmy2.MouseMovementLibraries.GHubSupport;
using Aimmy2.UILibrary;
using MouseMovementLibraries.ddxoftSupport;
using MouseMovementLibraries.RazerSupport;
using Other;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using UILibrary;
using Visuality;
using LogLevel = Other.LogManager.LogLevel;

namespace Aimmy2.Controls
{
    public partial class SettingsMenuControl : UserControl
    {
        private MainWindow? _mainWindow;
        private bool _isInitialized;

        // Local minimize state management
        private readonly Dictionary<string, bool> _localMinimizeState = new()
        {
            { "Settings Menu", false },
            { "X/Y Percentage Adjustment", false },
            { "Theme Settings", false },
            { "Display Settings", false }
        };

        // Public properties for MainWindow access
        public StackPanel SettingsConfigPanel => SettingsConfig;
        public StackPanel XYPercentageEnablerMenuPanel => XYPercentageEnablerMenu;
        public StackPanel ThemeMenuPanel => ThemeMenu;
        public StackPanel DisplaySelectMenuPanel => DisplaySelectMenu;
        public ScrollViewer SettingsMenuScrollViewer => SettingsMenu;

        public SettingsMenuControl()
        {
            InitializeComponent();
        }

        public void Initialize(MainWindow mainWindow)
        {
            if (_isInitialized) return;

            _mainWindow = mainWindow;
            _isInitialized = true;

            // Load minimize states from global dictionary if they exist
            LoadMinimizeStatesFromGlobal();

            LoadSettingsConfig();
            LoadXYPercentageMenu();
            LoadThemeMenu();
            LoadDisplaySelectMenu();

            // Apply minimize states after loading
            ApplyMinimizeStates();

            // Subscribe to display changes
            DisplayManager.DisplayChanged += OnDisplayChanged;
        }

        #region Minimize State Management

        private void LoadMinimizeStatesFromGlobal()
        {
            foreach (var key in _localMinimizeState.Keys.ToList())
            {
                if (Dictionary.minimizeState.ContainsKey(key))
                {
                    _localMinimizeState[key] = Dictionary.minimizeState[key];
                }
            }
        }

        private void SaveMinimizeStatesToGlobal()
        {
            foreach (var kvp in _localMinimizeState)
            {
                Dictionary.minimizeState[kvp.Key] = kvp.Value;
            }
        }

        private void ApplyMinimizeStates()
        {
            ApplyPanelState("Settings Menu", SettingsConfigPanel);
            ApplyPanelState("X/Y Percentage Adjustment", XYPercentageEnablerMenuPanel);
            ApplyPanelState("Theme Settings", ThemeMenuPanel);
            ApplyPanelState("Display Settings", DisplaySelectMenuPanel);
        }

        private void ApplyPanelState(string stateName, StackPanel panel)
        {
            if (_localMinimizeState.TryGetValue(stateName, out bool isMinimized))
            {
                SetPanelVisibility(panel, !isMinimized);
            }
        }

        private void SetPanelVisibility(StackPanel panel, bool isVisible)
        {
            foreach (UIElement child in panel.Children)
            {
                // Keep titles, spacers, and bottom rectangles always visible
                bool shouldStayVisible = child is ATitle || child is ASpacer || child is ARectangleBottom;

                child.Visibility = shouldStayVisible
                    ? Visibility.Visible
                    : (isVisible ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        private void TogglePanel(string stateName, StackPanel panel)
        {
            if (!_localMinimizeState.ContainsKey(stateName)) return;

            // Toggle the state
            _localMinimizeState[stateName] = !_localMinimizeState[stateName];

            // Apply the new visibility
            SetPanelVisibility(panel, !_localMinimizeState[stateName]);

            // Save to global dictionary
            SaveMinimizeStatesToGlobal();
        }

        #endregion

        #region Menu Section Loaders

        private void LoadSettingsConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, SettingsConfig);

            builder
                .AddTitle("Settings Menu", true, t =>
                {
                    uiManager.AT_SettingsMenu = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Settings Menu", SettingsConfigPanel);
                })
                .AddToggle("Collect Data While Playing", t => uiManager.T_CollectDataWhilePlaying = t)
                .AddToggle("Auto Label Data", t => uiManager.T_AutoLabelData = t)
                .AddDropdown("Mouse Movement Method", d =>
                {
                    uiManager.D_MouseMovementMethod = d;
                    d.DropdownBox.SelectedIndex = -1;  // Prevent auto-selection

                    // Add options
                    _mainWindow.AddDropdownItem(d, "Mouse Event");
                    _mainWindow.AddDropdownItem(d, "SendInput");
                    uiManager.DDI_LGHUB = _mainWindow.AddDropdownItem(d, "LG HUB");
                    uiManager.DDI_RazerSynapse = _mainWindow.AddDropdownItem(d, "Razer Synapse (Require Razer Peripheral)");
                    uiManager.DDI_ddxoft = _mainWindow.AddDropdownItem(d, "ddxoft Virtual Input Driver");

                    // Setup handlers
                    uiManager.DDI_LGHUB.Selected += async (s, e) =>
                    {
                        if (!new LGHubMain().Load())
                            await ResetToMouseEvent();
                    };

                    uiManager.DDI_RazerSynapse.Selected += async (s, e) =>
                    {
                        if (!await RZMouse.Load())
                            await ResetToMouseEvent();
                    };

                    uiManager.DDI_ddxoft.Selected += async (s, e) =>
                    {
                        if (!await DdxoftMain.Load())
                            await ResetToMouseEvent();
                    };
                })
                .AddDropdown("Screen Capture Method", d =>
                {
                    uiManager.D_ScreenCaptureMethod = d;
                    //d.DropdownBox.SelectedIndex = -1;  // Prevent auto-selection
                    _mainWindow.AddDropdownItem(d, "DirectX");
                    _mainWindow.AddDropdownItem(d, "GDI+");
                })
                .AddDropdown("Image Size", d =>
                {
                    uiManager.D_ImageSize = d;

                    // Add size options
                    _mainWindow.AddDropdownItem(d, "640");
                    _mainWindow.AddDropdownItem(d, "512");
                    _mainWindow.AddDropdownItem(d, "416");
                    _mainWindow.AddDropdownItem(d, "320");
                    _mainWindow.AddDropdownItem(d, "256");
                    _mainWindow.AddDropdownItem(d, "160");

                    // Set default to current value
                    var currentSize = Dictionary.dropdownState["Image Size"];
                    for (int i = 0; i < d.DropdownBox.Items.Count; i++)
                    {
                        if ((d.DropdownBox.Items[i] as ComboBoxItem)?.Content?.ToString() == currentSize)
                        {
                            d.DropdownBox.SelectedIndex = i;
                            break;
                        }
                    }

                    // Handle selection change
                    d.DropdownBox.SelectionChanged += async (s, e) =>
                    {
                        if (d.DropdownBox.SelectedItem == null || e.AddedItems.Count == 0)
                            return;

                        var newSize = (d.DropdownBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                        if (string.IsNullOrEmpty(newSize))
                            return;

                        // Check if model is loaded
                        if (FileManager.AIManager == null || Dictionary.lastLoadedModel == "N/A")
                        {
                            // No model loaded, just update the dictionary
                            Dictionary.dropdownState["Image Size"] = newSize;
                            LogManager.Log(LogLevel.Info, $"Image size set to {newSize}x{newSize} (no model loaded)", true, 2000);
                            return;
                        }

                        FileManager.CurrentlyLoadingModel = true;
                        LogManager.Log(LogLevel.Info, $"Image size changing to {newSize}");

                        try
                        {
                            // Signal the AI to prepare for shutdown
                            if (FileManager.AIManager != null)
                            {
                                FileManager.AIManager.RequestSizeChange(int.Parse(newSize));
                                await Task.Delay(100); // Give AI loop time to pause
                            }

                            // Dispose the current AIManager
                            var modelPath = System.IO.Path.Combine("bin/models", Dictionary.lastLoadedModel);
                            FileManager.AIManager?.Dispose();
                            FileManager.AIManager = null;

                            // NOW it's safe to update the dictionary
                            Dictionary.dropdownState["Image Size"] = newSize;

                            // Create new AIManager with the new size
                            FileManager.AIManager = new AIManager(modelPath);

                            LogManager.Log(LogLevel.Info, $"Successfully changed image size to {newSize}x{newSize}", true, 2000);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log(LogLevel.Error, $"Error changing image size: {ex.Message}", true, 5000);
                        }
                        finally
                        {
                            FileManager.CurrentlyLoadingModel = false;
                        }
                    };
                })
                .AddSlider("AI Minimum Confidence", "% Confidence", 1, 1, 1, 100, s =>
                {
                    uiManager.S_AIMinimumConfidence = s;
                    s.Slider.PreviewMouseLeftButtonUp += (sender, e) =>
                    {
                        var value = s.Slider.Value;
                        if (value >= 95)
                            LogManager.Log(LogLevel.Warning, "The minimum confidence you have set for Aimmy to be too high and may be unable to detect players.", true);
                        else if (value <= 35)
                            LogManager.Log(LogLevel.Warning, "The minimum confidence you have set for Aimmy may be too low can cause false positives.", true);
                    };
                })
                .AddToggle("Mouse Background Effect", t => uiManager.T_MouseBackgroundEffect = t)
                .AddToggle("UI TopMost", t => uiManager.T_UITopMost = t)
                .AddToggle("Debug Mode", t => uiManager.T_DebugMode = t)
                .AddToggle("StreamGuard", t => uiManager.T_StreamGuard = t)
                .AddKeyChanger("Enable StreamGuard TKB")
                .AddButton("Save Config", b =>
                {
                    uiManager.B_SaveConfig = b;
                    b.Reader.Click += (s, e) => new ConfigSaver().ShowDialog();
                })
                .AddSeparator();
        }

        private void LoadXYPercentageMenu()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, XYPercentageEnablerMenu);

            builder
                .AddTitle("X/Y Percentage Adjustment", true, t =>
                {
                    uiManager.AT_XYPercentageAdjustmentEnabler = t;
                    t.Minimize.Click += (s, e) =>
                        TogglePanel("X/Y Percentage Adjustment", XYPercentageEnablerMenuPanel);
                })
                .AddToggle("X Axis Percentage Adjustment", t => uiManager.T_XAxisPercentageAdjustment = t)
                .AddToggle("Y Axis Percentage Adjustment", t => uiManager.T_YAxisPercentageAdjustment = t)
                .AddSeparator();
        }

        private void LoadDisplaySelectMenu()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, DisplaySelectMenu);

            builder
                .AddTitle("Display Settings", true, t =>
                {
                    uiManager.AT_DisplaySelector = t;
                    t.Minimize.Click += (s, e) =>
                        TogglePanel("Display Settings", DisplaySelectMenuPanel);
                })
                .AddSeparator();

            // Handle DisplaySelector separately as it's a custom control
            uiManager.DisplaySelector = new ADisplaySelector();
            uiManager.DisplaySelector.RefreshDisplays();

            // Insert after title but before separator
            var insertIndex = DisplaySelectMenu.Children.Count - 2;
            DisplaySelectMenu.Children.Insert(insertIndex, uiManager.DisplaySelector);

            // Add refresh button after DisplaySelector
            var refreshButton = new APButton("Refresh Displays");
            refreshButton.Reader.Click += (s, e) =>
            {
                try
                {
                    DisplayManager.RefreshDisplays();
                    uiManager.DisplaySelector.RefreshDisplays();
                    LogManager.Log(LogLevel.Info, "Display list refreshed successfully.", true);
                }
                catch (Exception ex)
                {
                    LogManager.Log(LogLevel.Error, $"Error refreshing displays: {ex.Message}", true);
                }
            };
            DisplaySelectMenu.Children.Insert(insertIndex + 1, refreshButton);
        }

        private void LoadThemeMenu()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, ThemeMenu);

            builder
                .AddTitle("Theme Settings", true, t =>
                {
                    uiManager.AT_ThemeColorWheel = t;
                    t.Minimize.Click += (s, e) =>
                        TogglePanel("Theme Settings", ThemeMenuPanel);
                })
                .AddSeparator();

            // Handle ColorWheel separately as it's a custom control
            uiManager.ThemeColorWheel = new AColorWheel();

            //--
            var arrowButton = uiManager.ThemeColorWheel.FindName("ArrowButton") as Button;
            arrowButton.Visibility = Visibility.Visible;
            //--

            // Insert before separator
            var insertIndex = ThemeMenu.Children.Count - 2;
            ThemeMenu.Children.Insert(insertIndex, uiManager.ThemeColorWheel);
        }

        #endregion

        #region Helper Methods

        private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    LogManager.Log(LogLevel.Info, $"AI focus switched to Display {e.DisplayIndex + 1} ({e.Bounds.Width}x{e.Bounds.Height})", true);
                    UpdateDisplayRelatedSettings(e);
                }
                catch (Exception ex)
                {
                }
            });
        }

        private void UpdateDisplayRelatedSettings(DisplayChangedEventArgs e)
        {
            Dictionary.sliderSettings["SelectedDisplay"] = e.DisplayIndex;
        }

        private async Task ResetToMouseEvent()
        {
            await Task.Delay(500);
            _mainWindow!.uiManager.D_MouseMovementMethod!.DropdownBox.SelectedIndex = 0;
        }

        public void UpdateImageSizeDropdown(string newSize)
        {
            if (_mainWindow?.uiManager.D_ImageSize != null)
            {
                var dropdown = _mainWindow.uiManager.D_ImageSize;
                for (int i = 0; i < dropdown.DropdownBox.Items.Count; i++)
                {
                    if ((dropdown.DropdownBox.Items[i] as ComboBoxItem)?.Content?.ToString() == newSize)
                    {
                        dropdown.DropdownBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            DisplayManager.DisplayChanged -= OnDisplayChanged;
            _mainWindow?.uiManager.DisplaySelector?.Dispose();

            // Save minimize states before disposing
            SaveMinimizeStatesToGlobal();
        }

        #endregion

        #region Control Creation Methods

        private AToggle CreateToggle(string title)
        {
            var toggle = new AToggle(title);
            _mainWindow!.toggleInstances[title] = toggle;

            // Set initial state
            if (Dictionary.toggleState[title])
                toggle.EnableSwitch();
            else
                toggle.DisableSwitch();

            // Handle click
            toggle.Reader.Click += (sender, e) =>
            {
                Dictionary.toggleState[title] = !Dictionary.toggleState[title];
                _mainWindow.UpdateToggleUI(toggle, Dictionary.toggleState[title]);
                _mainWindow.Toggle_Action(title);
            };

            return toggle;
        }

        //copied & Pasted from other class
        private AKeyChanger CreateKeyChanger(string title, string keybind)
        {
            var keyChanger = new AKeyChanger(title, keybind);

            keyChanger.Reader.Click += (sender, e) =>
            {
                keyChanger.KeyNotifier.Content = "...";
                _mainWindow!.bindingManager.StartListeningForBinding(title);

                Action<string, string>? bindingSetHandler = null;
                bindingSetHandler = (bindingId, key) =>
                {
                    if (bindingId == title)
                    {
                        keyChanger.KeyNotifier.Content = KeybindNameManager.ConvertToRegularKey(key);
                        Dictionary.bindingSettings[bindingId] = key;
                        _mainWindow.bindingManager.OnBindingSet -= bindingSetHandler;
                    }
                };

                _mainWindow.bindingManager.OnBindingSet += bindingSetHandler;
            };

            return keyChanger;
        }

        private ASlider CreateSlider(string title, string label, double frequency, double buttonSteps,
            double min, double max)
        {
            var slider = new ASlider(title, label, buttonSteps)
            {
                Slider = { Minimum = min, Maximum = max, TickFrequency = frequency }
            };

            slider.Slider.Value = Dictionary.sliderSettings.TryGetValue(title, out var value) ? value : min;
            slider.Slider.ValueChanged += (s, e) => Dictionary.sliderSettings[title] = slider.Slider.Value;

            return slider;
        }

        private ADropdown CreateDropdown(string title) => new(title, title);

        #endregion

        #region Section Builder

        private class SectionBuilder
        {
            private readonly SettingsMenuControl _parent;
            private readonly StackPanel _panel;

            public SectionBuilder(SettingsMenuControl parent, StackPanel panel)
            {
                _parent = parent;
                _panel = panel;
            }

            public SectionBuilder AddTitle(string title, bool canMinimize, Action<ATitle>? configure = null)
            {
                var titleControl = new ATitle(title, canMinimize);
                configure?.Invoke(titleControl);
                _panel.Children.Add(titleControl);
                return this;
            }

            public SectionBuilder AddToggle(string title, Action<AToggle>? configure = null)
            {
                var toggle = _parent.CreateToggle(title);
                configure?.Invoke(toggle);
                _panel.Children.Add(toggle);
                return this;
            }

            //copied & Pasted from other class
            public SectionBuilder AddKeyChanger(string title, Action<AKeyChanger>? configure = null, string? defaultKey = null)
            {
                var key = defaultKey ?? Dictionary.bindingSettings[title];
                var keyChanger = _parent.CreateKeyChanger(title, key);
                configure?.Invoke(keyChanger);
                _panel.Children.Add(keyChanger);
                return this;
            }

            public SectionBuilder AddSlider(string title, string label, double frequency, double buttonSteps,
                double min, double max, Action<ASlider>? configure = null)
            {
                var slider = _parent.CreateSlider(title, label, frequency, buttonSteps, min, max);
                configure?.Invoke(slider);
                _panel.Children.Add(slider);
                return this;
            }

            public SectionBuilder AddDropdown(string title, Action<ADropdown>? configure = null)
            {
                var dropdown = _parent.CreateDropdown(title);
                configure?.Invoke(dropdown);
                _panel.Children.Add(dropdown);
                return this;
            }

            public SectionBuilder AddButton(string title, Action<APButton>? configure = null)
            {
                var button = new APButton(title);
                configure?.Invoke(button);
                _panel.Children.Add(button);
                return this;
            }

            public SectionBuilder AddSeparator()
            {
                _panel.Children.Add(new ARectangleBottom());
                _panel.Children.Add(new ASpacer());
                return this;
            }
        }

        #endregion
    }
}