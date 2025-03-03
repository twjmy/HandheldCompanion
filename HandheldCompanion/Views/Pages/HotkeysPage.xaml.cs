﻿using ControllerCommon.Utils;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for HotkeysPage.xaml
    /// </summary>
    public partial class HotkeysPage : Page
    {
        public HotkeysPage()
        {
            InitializeComponent();

            HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
            HotkeysManager.HotkeyTypeCreated += HotkeysManager_HotkeyTypeCreated;
        }

        public HotkeysPage(string Tag) : this()
        {
            this.Tag = Tag;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        private void HotkeysManager_HotkeyTypeCreated(InputsHotkey.InputsHotkeyType type)
        {
            // These are special shortcut keys with no related events
            if (type == InputsHotkey.InputsHotkeyType.Embedded)
                return;

            // UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                SimpleStackPanel stackPanel = new()
                {
                    Tag = type,
                    Spacing = 6
                };

                TextBlock textBlock = new TextBlock() { Text = EnumUtils.GetDescriptionFromEnumValue(type) };
                textBlock.SetResourceReference(Control.StyleProperty, "BaseTextBlockStyle");

                stackPanel.Children.Add(textBlock);
                HotkeysPanel.Children.Add(stackPanel);
            });
        }

        private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
        {
            // These are special shortcut keys with no related events
            if (hotkey.inputsHotkey.hotkeyType == InputsHotkey.InputsHotkeyType.Embedded)
                return;

            Type DeviceType = hotkey.inputsHotkey.DeviceType;
            if (DeviceType is not null && DeviceType != MainWindow.CurrentDevice.GetType())
                return;

            // UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                HotkeyControl control = hotkey.GetControl();

                ushort idx = (ushort)hotkey.inputsHotkey.hotkeyType;

                SimpleStackPanel stackPanel = (SimpleStackPanel)HotkeysPanel.Children[idx];
                stackPanel.Children.Add(control);
            });
        }
    }
}
