﻿using Aimmy2.AILogic;
using Aimmy2.Class;
using Aimmy2.UILibrary;
using Class;
using InputLogic;
using Microsoft.Win32;
using Other;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UILibrary;
using Visuality;

namespace Aimmy2.Controls
{
    public partial class AimMenuControl : UserControl
    {
        //--
        UISections.ColorPicker colorPickerInstance = null;
        UISections.ColorPicker fovColorPickerInstance = null;
        //--
        private MainWindow? _mainWindow;
        private bool _isInitialized;

        // Local minimize state management
        private readonly Dictionary<string, bool> _localMinimizeState = new()
        {
            { "Aim Assist", false },
            { "Aim Config", false },
            { "Auto Trigger", false },
            { "Anti Recoil", false },
            { "Anti Recoil Config", false },
            { "FOV Config", false },
            { "ESP Config", false }
        };

        // Public properties for MainWindow access
        public StackPanel AimAssistPanel => AimAssist;
        public StackPanel TriggerBotPanel => TriggerBot;
        public StackPanel AntiRecoilPanel => AntiRecoil;
        public StackPanel ESPConfigPanel => ESPConfig;
        public StackPanel AimConfigPanel => AimConfig;
        public StackPanel ARConfigPanel => ARConfig;
        public StackPanel FOVConfigPanel => FOVConfig;
        public ScrollViewer AimMenuScrollViewer => AimMenu;

        public AimMenuControl()
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

            AIManager.ClassesUpdated += OnClassesChanged;
            AIManager.ImageSizeUpdated += OnImageSizeChanged;

            // Load all sections
            LoadAimAssist();
            LoadAimConfig();
            LoadTriggerBot();
            LoadAntiRecoil();
            LoadAntiRecoilConfig();
            LoadFOVConfig();
            LoadESPConfig();

            // Apply minimize states after loading
            ApplyMinimizeStates();
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
            ApplyPanelState("Aim Assist", AimAssistPanel);
            ApplyPanelState("Aim Config", AimConfigPanel);
            ApplyPanelState("Auto Trigger", TriggerBotPanel);
            ApplyPanelState("Anti Recoil", AntiRecoilPanel);
            ApplyPanelState("Anti Recoil Config", ARConfigPanel);
            ApplyPanelState("FOV Config", FOVConfigPanel);
            ApplyPanelState("ESP Config", ESPConfigPanel);
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

        private void LoadAimAssist()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, AimAssist);

            builder
                .AddTitle("Aim Assist", true, t =>
                {
                    uiManager.AT_Aim = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Aim Assist", AimAssistPanel);
                })
                .AddToggle("Aim Assist", t =>
                {
                    uiManager.T_AimAligner = t;
                    t.Reader.Click += (s, e) =>
                    {
                        if (Dictionary.toggleState["Aim Assist"] && Dictionary.lastLoadedModel == "N/A")
                        {
                            Dictionary.toggleState["Aim Assist"] = false;
                            _mainWindow.UpdateToggleUI(t, false);
                            LogManager.Log(LogManager.LogLevel.Warning, "Please load a model first", true);
                        }
                    };
                })
                .AddKeyChanger("Aim Assist TKB")
                .AddKeyChanger("Aim Keybind", k => uiManager.C_Keybind = k)
                .AddKeyChanger("Second Aim Keybind")
                .AddToggle("Sticky Aim", t => uiManager.T_StickyAim = t)
                .AddKeyChanger("Sticky Aim TKB")
                .AddToggle("Constant AI Tracking", t =>
                {
                    uiManager.T_ConstantAITracking = t;
                    t.Reader.Click += (s, e) =>
                    {
                        if (Dictionary.toggleState["Constant AI Tracking"])
                        {
                            if (Dictionary.lastLoadedModel == "N/A")
                            {
                                Dictionary.toggleState["Constant AI Tracking"] = false;
                                _mainWindow.UpdateToggleUI(t, false);
                            }
                            else
                            {
                                Dictionary.toggleState["Aim Assist"] = true;
                                _mainWindow.UpdateToggleUI(uiManager.T_AimAligner, true);
                            }
                        }
                    };
                })
                .AddKeyChanger("Constant AI Tracking TKB")
                .AddToggle("Predictions", t => uiManager.T_Predictions = t)
                .AddKeyChanger("Predictions TKB")
                .AddToggle("EMA Smoothening", t => uiManager.T_EMASmoothing = t)
                .AddKeyChanger("EMA Smoothening TKB")
                .AddKeyChanger("Emergency Stop Keybind", k => uiManager.C_EmergencyKeybind = k)
                .AddToggle("Enable Model Switch Keybind", t => uiManager.T_EnableModelSwitchKeybind = t)
                .AddKeyChanger("Model Switch Keybind", k => uiManager.C_ModelSwitchKeybind = k)
                .AddSeparator();
        }

        private void LoadAimConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, AimConfig);

            builder
                .AddTitle("Aim Config", true, t =>
                {
                    uiManager.AT_AimConfig = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Aim Config", AimConfigPanel);
                })
                .AddDropdown("Prediction Method", d =>
                {
                    d.DropdownBox.SelectedIndex = -1;
                    uiManager.D_PredictionMethod = d;
                    _mainWindow.AddDropdownItem(d, "Kalman Filter");
                    _mainWindow.AddDropdownItem(d, "Shall0e's Prediction");
                    _mainWindow.AddDropdownItem(d, "wisethef0x's EMA Prediction");
                })

                .AddDropdown("Movement Path", d =>
                {
                    d.DropdownBox.SelectedIndex = 0;
                    uiManager.D_MovementPath = d;
                    _mainWindow.AddDropdownItem(d, "Cubic Bezier");
                    _mainWindow.AddDropdownItem(d, "Exponential");
                    _mainWindow.AddDropdownItem(d, "Linear");
                    _mainWindow.AddDropdownItem(d, "Adaptive");
                    _mainWindow.AddDropdownItem(d, "Perlin Noise");
                })
                .AddDropdown("Detection Area Type", d =>
                {
                    d.DropdownBox.SelectedIndex = -1;
                    uiManager.D_DetectionAreaType = d;
                    uiManager.DDI_ClosestToCenterScreen = _mainWindow.AddDropdownItem(d, "Closest to Center Screen");
                    _mainWindow.AddDropdownItem(d, "Closest to Mouse");

                    uiManager.DDI_ClosestToCenterScreen.Selected += async (s, e) =>
                    {
                        await Task.Delay(100);
                        MainWindow.FOVWindow.FOVStrictEnclosure.Margin = new Thickness(
                            Convert.ToInt16((WinAPICaller.ScreenWidth / 2) / WinAPICaller.scalingFactorX) - 320,
                            Convert.ToInt16((WinAPICaller.ScreenHeight / 2) / WinAPICaller.scalingFactorY) - 320,
                            0, 0);
                    };
                })
                .AddDropdown("Aiming Boundaries Alignment", d =>
                {
                    d.DropdownBox.SelectedIndex = -1;
                    uiManager.D_AimingBoundariesAlignment = d;
                    _mainWindow.AddDropdownItem(d, "Center");
                    _mainWindow.AddDropdownItem(d, "Top");
                    _mainWindow.AddDropdownItem(d, "Bottom");
                })
                .AddDropdown("Target Class", d =>
                {
                    d.DropdownBox.SelectedIndex = 0;
                    uiManager.D_TargetClass = d;
                    _mainWindow.AddDropdownItem(d, "Best Confidence");
                    UpdateTargetClassDropdown(d);
                });

            // Add sliders with validation
            AddConfigSliders(builder, uiManager);
            builder.AddSeparator();
        }

        private void AddConfigSliders(SectionBuilder builder, UI uiManager)
        {
            builder
                .AddSlider("Mouse Sensitivity (+/-)", "Sensitivity", 0.01, 0.01, 0.01, 1, s =>
                {
                    uiManager.S_MouseSensitivity = s;
                    s.Slider.PreviewMouseLeftButtonUp += (sender, e) =>
                    {
                        var value = s.Slider.Value;
                        if (value >= 0.98)
                            LogManager.Log(LogManager.LogLevel.Warning,
                                "The Mouse Sensitivity you have set can cause Aimmy to be unable to aim, please decrease if you suffer from this problem", true);
                        else if (value <= 0.1)
                            LogManager.Log(LogManager.LogLevel.Warning,
                                "The Mouse Sensitivity you have set can cause Aimmy to be unstable to aim, please increase if you suffer from this problem", true);
                    };
                })
                .AddSlider("Mouse Jitter", "Jitter", 1, 1, 0, 15, s => uiManager.S_MouseJitter = s)
                .AddSlider("Sticky Aim Threshold", "Pixels", 1, 1, 0, 100, s =>
                {
                    uiManager.S_StickyAimThreshold = s;
                    var value = s.Slider.Value;
                    if (value <= 10)
                    {
                        LogManager.Log(LogManager.LogLevel.Warning,
                            "The threshold you have set may cause sticky aim to not work as intended, please increase if you suffer from this issue.\nINFO: The Sticky aim threshold is how many pixels it will take until it realizes the target is gone and moves on to another target",
                            true, 10000);
                    }
                })
                .AddSlider("Y Offset (Up/Down)", "Offset", 1, 1, -150, 150, s => uiManager.S_YOffset = s)
                .AddSlider("Y Offset (%)", "Percent", 1, 1, 0, 100, s => uiManager.S_YOffsetPercent = s)
                .AddSlider("X Offset (Left/Right)", "Offset", 1, 1, -150, 150, s => uiManager.S_XOffset = s)
                .AddSlider("X Offset (%)", "Percent", 1, 1, 0, 100, s => uiManager.S_XOffsetPercent = s)
                .AddSlider("EMA Smoothening", "Amount", 0.01, 0.01, 0.01, 1, s =>
                {
                    uiManager.S_EMASmoothing = s;
                    s.Slider.ValueChanged += (sender, e) =>
                    {
                        if (Dictionary.toggleState["EMA Smoothening"])
                        {
                            MouseManager.smoothingFactor = s.Slider.Value;
                        }
                    };
                });
        }
        private void LoadTriggerBot()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, TriggerBot);

            builder
                .AddTitle("Auto Trigger", true, t =>
                {
                    uiManager.AT_TriggerBot = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Auto Trigger", TriggerBotPanel);
                })
                .AddToggle("Auto Trigger", t => uiManager.T_AutoTrigger = t)
                .AddToggle("Cursor Check", t => uiManager.T_CursorCheck = t)
                .AddToggle("Spray Mode", t => uiManager.T_SprayMode = t)
                //.AddToggle("Only When Held", t => uiManager.T_OnlyWhenHeld = t) 
                .AddSlider("Auto Trigger Delay", "Seconds", 0.01, 0.1, 0.01, 1, s => uiManager.S_AutoTriggerDelay = s)
                .AddSeparator();
        }

        private void LoadAntiRecoil()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, AntiRecoil);

            builder
                .AddTitle("Anti Recoil", true, t =>
                {
                    uiManager.AT_AntiRecoil = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Anti Recoil", AntiRecoilPanel);
                })
                .AddToggle("Anti Recoil", t => uiManager.T_AntiRecoil = t)
                .AddKeyChanger("Anti Recoil Keybind", k => uiManager.C_AntiRecoilKeybind = k, "Left")
                .AddKeyChanger("Disable Anti Recoil Keybind", k => uiManager.C_ToggleAntiRecoilKeybind = k, "Oem6")
                .AddSlider("Hold Time", "Milliseconds", 1, 1, 1, 1000, s => uiManager.S_HoldTime = s, true)
                .AddButton("Record Fire Rate", b =>
                {
                    uiManager.B_RecordFireRate = b;
                    b.Reader.Click += (s, e) => new SetAntiRecoil(_mainWindow).Show();
                })
                .AddSlider("Fire Rate", "Milliseconds", 1, 1, 1, 5000, s => uiManager.S_FireRate = s, true)
                .AddSlider("Y Recoil (Up/Down)", "Move", 1, 1, -1000, 1000, s => uiManager.S_YAntiRecoilAdjustment = s, true)
                .AddSlider("X Recoil (Left/Right)", "Move", 1, 1, -1000, 1000, s => uiManager.S_XAntiRecoilAdjustment = s, true)
                .AddSeparator();
        }

        private void LoadAntiRecoilConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, ARConfig);

            builder
                .AddTitle("Anti Recoil Config", true, t =>
                {
                    uiManager.AT_AntiRecoilConfig = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Anti Recoil Config", ARConfigPanel);
                })
                .AddToggle("Enable Gun Switching Keybind", t => uiManager.T_EnableGunSwitchingKeybind = t)
                .AddButton("Save Anti Recoil Config", b =>
                {
                    uiManager.B_SaveRecoilConfig = b;
                    b.Reader.Click += (s, e) =>
                    {
                        var saveFileDialog = new SaveFileDialog
                        {
                            InitialDirectory = $"{Directory.GetCurrentDirectory()}\\bin\\anti_recoil_configs",
                            Filter = "Aimmy Style Recoil Config (*.cfg)|*.cfg"
                        };

                        if (saveFileDialog.ShowDialog() == true)
                        {
                            SaveDictionary.WriteJSON(Dictionary.AntiRecoilSettings, saveFileDialog.FileName);
                            LogManager.Log(LogManager.LogLevel.Info, $"[Anti Recoil] Config has been saved to \"{saveFileDialog.FileName}\"", true);
                        }
                    };
                })
                .AddKeyChanger("Gun 1 Key", k => uiManager.C_Gun1Key = k, "D1")
                .AddFileLocator("Gun 1 Config", f => uiManager.AFL_Gun1Config = f, "Aimmy Style Recoil Config (*.cfg)|*.cfg", "\\bin\\anti_recoil_configs")
                .AddKeyChanger("Gun 2 Key", k => uiManager.C_Gun2Key = k, "D2")
                .AddFileLocator("Gun 2 Config", f => uiManager.AFL_Gun2Config = f, "Aimmy Style Recoil Config (*.cfg)|*.cfg", "\\bin\\anti_recoil_configs")
                .AddButton("Load Gun 1 Config", b =>
                {
                    uiManager.B_LoadGun1Config = b;
                    b.Reader.Click += (s, e) => _mainWindow.LoadAntiRecoilConfig(Dictionary.filelocationState["Gun 1 Config"], true);
                })
                .AddButton("Load Gun 2 Config", b =>
                {
                    uiManager.B_LoadGun2Config = b;
                    b.Reader.Click += (s, e) => _mainWindow.LoadAntiRecoilConfig(Dictionary.filelocationState["Gun 2 Config"], true);
                })
                .AddSeparator();
        }

        private void LoadFOVConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, FOVConfig);

            builder
                .AddTitle("FOV Config", true, t =>
                {
                    uiManager.AT_FOV = t;
                    t.Minimize.Click += (s, e) => TogglePanel("FOV Config", FOVConfigPanel);
                })
                .AddToggle("FOV", t => uiManager.T_FOV = t)
                .AddToggle("Dynamic FOV", t => uiManager.T_DynamicFOV = t)
                .AddToggle("Third Person Support", t => uiManager.T_ThirdPersonSupport = t)
                .AddKeyChanger("Dynamic FOV Keybind", k => uiManager.C_DynamicFOV = k)
                .AddDropdown("FOV Style", d =>
                {
                    uiManager.D_FOVSTYLE = d;

                    var circleItem = _mainWindow.AddDropdownItem(d, "Circle");
                    var rectangleItem = _mainWindow.AddDropdownItem(d, "Rectangle");

                    circleItem.Selected += (s, e) =>
                    {
                        MainWindow.FOVWindow.Circle.Visibility = Visibility.Visible;
                        MainWindow.FOVWindow.RectangleShape.Visibility = Visibility.Collapsed;
                    };

                    rectangleItem.Selected += (s, e) =>
                    {
                        MainWindow.FOVWindow.Circle.Visibility = Visibility.Collapsed;
                        MainWindow.FOVWindow.RectangleShape.Visibility = Visibility.Visible;
                    };
                })
                .AddColorChanger("FOV Color", c =>
                {
                    c.Reader.Click += (s, e) =>
                    {
                        if (fovColorPickerInstance != null && fovColorPickerInstance.IsVisible)
                        {
                            fovColorPickerInstance.Activate();
                            return;
                        }

                        Color initialColor = Colors.White;
                        if (c.Reader.Background is SolidColorBrush scb)
                            initialColor = scb.Color;
                        fovColorPickerInstance = new UISections.ColorPicker(initialColor, "FOV Color");

                        fovColorPickerInstance.ColorChanged += (color) =>
                        {
                            if (uiManager?.CC_FOVColor?.Reader != null)
                            {
                                uiManager.CC_FOVColor.Reader.Background = new SolidColorBrush(color);
                            }
                            PropertyChanger.PostColor(color);
                        };

                        fovColorPickerInstance.Closed += (sender, args) =>
                        {
                            fovColorPickerInstance = null;
                        };

                        fovColorPickerInstance.Show();
                    };
                })
                .AddSlider("FOV Size", "Size", 1, 1, 10, 640, s =>
                {
                    uiManager.S_FOVSize = s;
                    s.Slider.ValueChanged += (sender, e) =>
                    {
                        _mainWindow.ActualFOV = s.Slider.Value;
                        PropertyChanger.PostNewFOVSize(_mainWindow.ActualFOV);
                    };
                })
                .AddSlider("Dynamic FOV Size", "Size", 1, 1, 10, 640, s =>
                {
                    uiManager.S_DynamicFOVSize = s;
                    s.Slider.ValueChanged += (sender, e) =>
                    {
                        if (Dictionary.toggleState["Dynamic FOV"])
                            PropertyChanger.PostNewFOVSize(s.Slider.Value);
                    };
                })
                .AddSeparator();
        }

        private void LoadESPConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, ESPConfig);

            builder
                .AddTitle("ESP Config", true, t =>
                {
                    uiManager.AT_DetectedPlayer = t;
                    t.Minimize.Click += (s, e) => TogglePanel("ESP Config", ESPConfigPanel);
                })
                .AddToggle("Show Detected Player", t => uiManager.T_ShowDetectedPlayer = t)
                .AddToggle("Show AI Confidence", t => uiManager.T_ShowAIConfidence = t)
                .AddToggle("Show Tracers", t => uiManager.T_ShowTracers = t);

            builder.AddDropdown("Tracer Position", d =>
            {
                d.DropdownBox.SelectedIndex = 0;
                uiManager.D_TracerPosition = d;
                // Changed the positions of these as top is above middle & bottom - ts (this) bothered me so i had to
                _mainWindow.AddDropdownItem(d, "Top");
                _mainWindow.AddDropdownItem(d, "Middle");
                _mainWindow.AddDropdownItem(d, "Bottom");
                d.DropdownBox.SelectionChanged += (s, e) =>
                {
                    if (Dictionary.toggleState["Show Detected Player"])
                    {
                        // simulate a click to turn it off - this is to force a reload of the ui cause tracer doesn't update otherwise - helz
                        uiManager.T_ShowDetectedPlayer.Reader.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                        // simulate a click to turn it back on - same as before ^ - helz
                        uiManager.T_ShowDetectedPlayer.Reader.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    }
                    else
                    {
                        if (Dictionary.DetectedPlayerOverlay != null)
                        {
                            Dictionary.DetectedPlayerOverlay.ForceReposition();
                        }
                    }
                };
            });

            builder
                .AddColorChanger("Detected Player Color", c =>
                {
                    c.Reader.Click += (s, e) =>
                    {
                        if (colorPickerInstance != null && colorPickerInstance.IsVisible)
                        {
                            colorPickerInstance.Activate();
                            return;
                        }

                        Color initialColor = Colors.White;
                        if (c.Reader.Background is SolidColorBrush scb)
                            initialColor = scb.Color;
                        colorPickerInstance = new UISections.ColorPicker(initialColor, "ESP Color");

                        colorPickerInstance.ColorChanged += (color) =>
                        {
                            if (uiManager?.CC_DetectedPlayerColor?.Reader != null)
                            {
                                uiManager.CC_DetectedPlayerColor.Reader.Background = new SolidColorBrush(color);
                            }
                            PropertyChanger.PostDPColor(color);
                        };

                        colorPickerInstance.Closed += (sender, args) =>
                        {
                            colorPickerInstance = null;
                        };

                        colorPickerInstance.Show();
                    };
                })
                .AddSlider("AI Confidence Font Size", "Size", 1, 1, 1, 30, s =>
                {
                    uiManager.S_DPFontSize = s;
                    s.Slider.ValueChanged += (sender, e) => PropertyChanger.PostDPFontSize((int)s.Slider.Value);
                })
                .AddSlider("Corner Radius", "Radius", 1, 1, 0, 100, s =>
                {
                    uiManager.S_DPCornerRadius = s;
                    s.Slider.ValueChanged += (sender, e) => PropertyChanger.PostDPWCornerRadius((int)s.Slider.Value);
                })
                .AddSlider("Border Thickness", "Thickness", 0.1, 1, 0.1, 10, s =>
                {
                    uiManager.S_DPBorderThickness = s;
                    s.Slider.ValueChanged += (sender, e) => PropertyChanger.PostDPWBorderThickness(s.Slider.Value);
                })
                .AddSlider("Opacity", "Opacity", 0.1, 0.1, 0, 1, s =>
                {
                    uiManager.S_DPOpacity = s;
                    s.Slider.ValueChanged += (sender, e) => PropertyChanger.PostDPWOpacity(s.Slider.Value);
                })
                .AddSeparator();
        }

        #endregion

        #region Helper Methods

        private void OnClassesChanged(Dictionary<int, string> classes)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_mainWindow?.uiManager.D_TargetClass != null)
                {
                    UpdateTargetClassDropdown(_mainWindow.uiManager.D_TargetClass, classes);
                }
            });
        }

        private void UpdateTargetClassDropdown(ADropdown dropdown, Dictionary<int, string>? _classes = null)
        {
            if (dropdown?.DropdownBox == null) return;
            string? selection = (dropdown.DropdownBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var removedItems = dropdown.DropdownBox.Items.Cast<ComboBoxItem>()
                .Where(item => item.Content?.ToString() != "Best Confidence")
                .ToList();

            foreach (var item in removedItems)
            {
                dropdown.DropdownBox.Items.Remove(item);
            }

            var classes = _classes ?? FileManager.AIManager?.ModelClasses ?? new Dictionary<int, string>();

            foreach (var kvp in classes.OrderBy(x => x.Key))
            {
                _mainWindow!.AddDropdownItem(dropdown, kvp.Value);
            }

            if (!string.IsNullOrEmpty(selection)) // tries to restore the selection
            {
                for (int i = 0; i < dropdown.DropdownBox.Items.Count; i++)
                {
                    if ((dropdown.DropdownBox.Items[i] as ComboBoxItem)?.Content?.ToString() == selection)
                    {
                        dropdown.DropdownBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            dropdown.DropdownBox.SelectedIndex = 0;
        }

        private void OnImageSizeChanged(int imageSize)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_mainWindow?.uiManager.S_FOVSize != null && _mainWindow?.uiManager.S_DynamicFOVSize != null)
                {
                    UpdateFovSizeSlider(_mainWindow.uiManager.S_FOVSize, imageSize);
                    UpdateFovSizeSlider(_mainWindow.uiManager.S_DynamicFOVSize, imageSize);
                }
            });
        }
        private void UpdateFovSizeSlider(ASlider slider, int imageSize = 640)
        {
            if (slider.Slider == null) return;
            if (imageSize < slider.Slider.Value)
            {
                slider.Slider.Value = imageSize;
            }
            slider.Slider.Maximum = imageSize;
        }

        private void HandleColorChange(AColorChanger colorChanger, string settingKey, Action<Color> updateAction)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                colorChanger.ColorChangingBorder.Background = new SolidColorBrush(color);
                Dictionary.colorState[settingKey] = color.ToString();
                updateAction(color);
            }
        }

        public void Dispose()
        {
            // Save minimize states before disposing
            SaveMinimizeStatesToGlobal();
        }

        #endregion

        #region Section Builder

        private class SectionBuilder
        {
            private readonly AimMenuControl _parent;
            private readonly StackPanel _panel;

            public SectionBuilder(AimMenuControl parent, StackPanel panel)
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

            public SectionBuilder AddKeyChanger(string title, Action<AKeyChanger>? configure = null, string? defaultKey = null)
            {
                var key = defaultKey ?? Dictionary.bindingSettings[title];
                var keyChanger = _parent.CreateKeyChanger(title, key);
                configure?.Invoke(keyChanger);
                _panel.Children.Add(keyChanger);
                return this;
            }

            public SectionBuilder AddSlider(string title, string label, double frequency, double buttonSteps,
                double min, double max, Action<ASlider>? configure = null, bool forAntiRecoil = false)
            {
                var slider = _parent.CreateSlider(title, label, frequency, buttonSteps, min, max, forAntiRecoil);
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

            public SectionBuilder AddColorChanger(string title, Action<AColorChanger>? configure = null)
            {
                var colorChanger = _parent.CreateColorChanger(title);
                configure?.Invoke(colorChanger);
                _panel.Children.Add(colorChanger);
                return this;
            }

            public SectionBuilder AddButton(string title, Action<APButton>? configure = null)
            {
                var button = new APButton(title);
                configure?.Invoke(button);
                _panel.Children.Add(button);
                return this;
            }

            public SectionBuilder AddFileLocator(string title, Action<AFileLocator>? configure = null,
                string filter = "All files (*.*)|*.*", string dlExtension = "")
            {
                var fileLocator = new AFileLocator(title, title, filter, dlExtension);
                configure?.Invoke(fileLocator);
                _panel.Children.Add(fileLocator);
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
            double min, double max, bool forAntiRecoil = false)
        {
            var slider = new ASlider(title, label, buttonSteps)
            {
                Slider = { Minimum = min, Maximum = max, TickFrequency = frequency }
            };

            var settings = forAntiRecoil ? Dictionary.AntiRecoilSettings : Dictionary.sliderSettings;
            slider.Slider.Value = settings.TryGetValue(title, out var value) ? value : min;
            slider.Slider.ValueChanged += (s, e) => settings[title] = slider.Slider.Value;

            return slider;
        }

        private ADropdown CreateDropdown(string title) => new(title, title);

        private AColorChanger CreateColorChanger(string title)
        {
            var colorChanger = new AColorChanger(title);
            colorChanger.ColorChangingBorder.Background =
                (Brush)new BrushConverter().ConvertFromString(Dictionary.colorState[title]);
            return colorChanger;
        }

        #endregion
    }
}