using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/* TODO list
 * found text should remain selected when find dialog moved -- ignore for now
 * in very large files, find will not scroll exactly to the found text
 */

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int LastTabNumber = 1;
        private readonly RecentFilesStore _recentFiles = new();
        public EditControl? CurrentEditControl => tabControl.SelectedContent as EditControl;
        private string _lastFindText = "";
        private bool _lastMatchCase = false;
        private bool _lastWrapAround = true;
        public string LastFindText => _lastFindText;
        public bool LastMatchCase => _lastMatchCase;
        public bool LastWrapAround => _lastWrapAround;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load the properties from disk
            Properties.Settings.Default.Reload();

            // check for upgrade
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            this.Left = Properties.Settings.Default.WindowLeft;
            this.Top = Properties.Settings.Default.WindowTop;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;

            // load other properties here
            StatusBarMenuItem.IsChecked = Properties.Settings.Default.StatusBarOn;
            if (StatusBarMenuItem.IsChecked)
            {
                myStatusBar.Visibility = Visibility.Visible;
            }
            else
            {
                myStatusBar.Visibility = Visibility.Collapsed;
            }

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // ensure window size doesn't exceed screen size
            if (this.Width > screenWidth) this.Width = screenWidth;
            if (this.Height > screenHeight) this.Height = screenHeight;

            // ensure window is not off the left or top
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;

            // ensure window is not off the right or bottom
            if (this.Left + this.Width > screenWidth)
                this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height;

            if (Properties.Settings.Default.WindowState == "Maximized")
                this.WindowState = WindowState.Maximized;

            // do other initialization
            // Once at startup (needed on modern .NET; not needed on .NET Framework)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RefreshRecentFilesMenu();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (TabItem item in tabControl.Items)
            {
                if (item.Content is EditControl tab && !tab.ConfirmSaveIfModified())
                {
                    e.Cancel = true;
                    tabControl.SelectedItem = item;   // show the tab that needs attention
                    return;
                }
            }

            Properties.Settings.Default.WindowState = this.WindowState.ToString();
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowLeft = this.Left;
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }

            // save other properties here
            if (myStatusBar.Visibility == Visibility.Visible)
                Properties.Settings.Default.StatusBarOn = true;
            else
                Properties.Settings.Default.StatusBarOn = false;

            Properties.Settings.Default.Save();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // File Menu

            // Ctrl+N - New Tab
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                New_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+O - Open
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Open_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+S - Save
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (SaveMenuItem.IsEnabled)
                {
                    Save_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            // Ctrl+Shift+S - Save As
            else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SaveAs_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+Alt+S - Save All
            else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
            {
                SaveAll_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+P - Print
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Print_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+W - Close Tab
            else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Close_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }

            // Edit Menu

            // Ctrl+F - Find
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Find_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // F3 - Find Next
            else if (e.Key == Key.F3 && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                FindNext_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Shift+F3 - Find Previous
            else if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                FindPrevious_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+H - Replace
            else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ReplaceMenuItem.IsEnabled)
                {
                    Replace_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            // Ctrl+G - Go To
            else if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
            {
                GoTo_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+L - Read Only
            else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ReadOnly_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }

            // View Menu

            // Ctrl+Plus - Zoom In
            else if (e.Key == Key.OemPlus && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomIn_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Add && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomIn_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+Minus - Zoom Out
            else if (e.Key == Key.OemMinus && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomOut_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Subtract && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomOut_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+Zero - Zoom Default
            else if (e.Key == Key.D0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomDefault_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.NumPad0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomDefault_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                UpdateEncodingAndEol(tab);
                UpdateStatusBar();
                UpdateReadOnlyState(tab);
                _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
            }
        }

        #region file menu events
        private void New_Click(object sender, RoutedEventArgs e)
        {
            EditControl tab = new EditControl();
            TabItem tabItem = new TabItem();

            LastTabNumber++;
            tab.TabName = "New " + LastTabNumber;
            tabItem.Header = "Tab " + LastTabNumber;

            tabItem.Content = tab;
            tabControl.Items.Add(tabItem);
            tabControl.SelectedItem = tabItem;
            tab.IsModified = false;
            UpdateEncodingAndEol(tab);
            UpdateStatusBar();
            UpdateReadOnlyState(tab);
            _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.Title = "Open Text File";
            if (openFileDialog.ShowDialog() != true) return;

            OpenFilePath(openFileDialog.FileName);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                tab.Save_Click();
                if (tab.TextFileName != "")
                {
                    string name = System.IO.Path.GetFileName(tab.TextFileName);
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        selectedTab.Header = name;
                    }
                    UpdateRecentFiles(tab.TextFileName);
                    tab.IsModified = false;
                    UpdateTabHeader();
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                tab.SaveAs_Click();
                if (tab.TextFileName != "")
                {
                    string name = System.IO.Path.GetFileName(tab.TextFileName);
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        selectedTab.Header = name;
                    }
                    UpdateRecentFiles(tab.TextFileName);
                    UpdateEncodingAndEol(tab);
                    UpdateStatusBar();
                    UpdateReadOnlyState(tab);
                    _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
                    tab.IsModified = false;
                    UpdateTabHeader();
                }
            }
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TabItem item in tabControl.Items)
            {
                if (item.Content is EditControl tab && tab.IsModified && !tab.IsReadOnlyMode)
                {
                    tabControl.SelectedItem = item;   // so Save As (if needed) shows the right tab/dialog

                    if (string.IsNullOrEmpty(tab.TextFileName))
                        SaveAs_Click(sender, e);
                    else
                        Save_Click(sender, e);

                    // no early return — continue to the next tab regardless of outcome
                }
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is not EditControl tab) return;
            tab.Print_Click();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is EditControl tab)
            {
                if (!tab.ConfirmSaveIfModified()) return;   // user cancelled

                if (tabControl.Items.Count > 1)
                {
                    tabControl.Items.Remove(tabControl.SelectedItem);
                    if (tabControl.SelectedContent is EditControl tab2)
                    {
                        UpdateEncodingAndEol(tab2);
                        UpdateStatusBar();
                        UpdateReadOnlyState(tab2);
                        _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
                    }
                }
                else
                {
                    TabItem? oldTab = tabControl.SelectedItem as TabItem;
                    New_Click(sender, e);
                    tabControl.Items.Remove(oldTab);
                    if (tabControl.SelectedContent is EditControl tab3)
                    {
                        UpdateEncodingAndEol(tab3);
                        UpdateStatusBar();
                        UpdateReadOnlyState(tab3);
                        _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
                    }
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
        #region edit menu events
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is EditControl tab)
                tab.Delete_Click();
        }

        private FindReplaceWindow? _findReplaceWindow;

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            if (_findReplaceWindow == null || !_findReplaceWindow.IsLoaded)
                _findReplaceWindow = new FindReplaceWindow(this) { Owner = this };
            else
                _findReplaceWindow.UpdateReplaceAvailabilityIfOpen();

            _findReplaceWindow.PopulateFromSelectionOrLastSearch();
            _findReplaceWindow.Show();
            _findReplaceWindow.Activate();
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentEditControl;
            if (tab == null) return;

            if (string.IsNullOrEmpty(_lastFindText))
            {
                Find_Click(sender, e);   // nothing searched yet; open the dialog instead
                return;
            }

            bool found = tab.FindNext(_lastFindText, _lastMatchCase, _lastWrapAround);
            if (!found)
                MessageBox.Show("Not found.", "Radish Notepad", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FindPrevious_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentEditControl;
            if (tab == null) return;

            if (string.IsNullOrEmpty(_lastFindText))
            {
                Find_Click(sender, e);
                return;
            }

            bool found = tab.FindPrevious(_lastFindText, _lastMatchCase, _lastWrapAround);
            if (!found)
                MessageBox.Show("Not found.", "Radish Notepad", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            if (_findReplaceWindow == null || !_findReplaceWindow.IsLoaded)
            {
                _findReplaceWindow = new FindReplaceWindow(this) { Owner = this };
                _findReplaceWindow.ShowReplaceMode();
            }
            else
            {
                _findReplaceWindow.ShowReplaceMode();
                _findReplaceWindow.UpdateReplaceAvailabilityIfOpen();
            }

            _findReplaceWindow.PopulateFromSelectionOrLastSearch();
            _findReplaceWindow.Show();
            _findReplaceWindow.Activate();
        }

        private void GoTo_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                tab.GoTo_Click();
            }
        }

        #endregion
        #region view menu events
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                int zoom = tab.ZoomSetting;
                zoom = Math.Min(zoom + 10, 500);
                tab.SetZoom(zoom);
                UpdateStatusBar();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                int zoom = tab.ZoomSetting;
                zoom = Math.Max(zoom - 10, 10);
                tab.SetZoom(zoom);
                UpdateStatusBar();
            }
        }

        private void ZoomDefault_Click(object sender, RoutedEventArgs e)
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                tab.SetZoom(100);
                UpdateStatusBar();
            }
        }

        private void StatusBar_Click(object sender, RoutedEventArgs e)
        {
            if (StatusBarMenuItem.IsChecked)
                myStatusBar.Visibility = Visibility.Visible;
            else
                myStatusBar.Visibility = Visibility.Collapsed;
        }

        private void WordWrap_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is EditControl tab)
                tab.WordWrap = WordWrapMenuItem.IsChecked;
        }
        #endregion

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            var toolBar = (ToolBar)sender;

            // Hide the overflow arrow
            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
                overflowGrid.Visibility = Visibility.Collapsed;

            // Remove the space reserved for the arrow
            if (toolBar.Template.FindName("MainPanelBorder", toolBar) is FrameworkElement mainPanel)
                mainPanel.Margin = new Thickness(0);
        }

        public void UpdateRecentFiles(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            _recentFiles.Add(path);          // your Add() already dedupes, inserts at front, caps count
            RefreshRecentFilesMenu();
        }

        public void EnableSave()
        {
            if (tabControl.SelectedContent is EditControl tab)
            {
                if (tab.IsReadOnlyMode == false)
                {
                    SaveMenuItem.IsEnabled = tab.IsModified;
                    SaveButton.IsEnabled = tab.IsModified;
                }
                else // read-only is true
                {
                    SaveMenuItem.IsEnabled = false;
                    SaveButton.IsEnabled = false;
                }
            }
            else // no tab selected (should never happen)
            {
                SaveMenuItem.IsEnabled = false;
                SaveButton.IsEnabled = false;
            }
        }

        public void EnableDelete()
        {
            if (tabControl.SelectedContent is EditControl tab)
            {
                if (tab.IsReadOnlyMode == false)
                {
                    DeleteMenuItem.IsEnabled = tab.HasSelection;
                    DeleteButton.IsEnabled = tab.HasSelection;
                }
                else // read-only is true
                {
                    DeleteMenuItem.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                }
            }
            else // no tab selected (should never happen)
            {
                DeleteMenuItem.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        private static string EncodingLabel(Encoding enc, bool hasBom)
        {
            string name = enc.CodePage switch
            {
                65001 => "UTF-8",
                1200 => "UTF-16 LE",
                1201 => "UTF-16 BE",
                1252 => "ANSI",
                437 => "OEM 437",
                850 => "OEM 850",
                _ => $"CP {enc.CodePage}"
            };
            return hasBom ? name + " with BOM" : name;
        }

        private static string EolLabel(string lineEnding) => lineEnding switch
        {
            "\r\n" => "Windows (CRLF)",
            "\n" => "Unix (LF)",
            "\r" => "Macintosh (CR)",
            _ => "Windows (CRLF)"
        };

        private void UpdateEncodingAndEol(EditControl? tab)   // use your own per-tab class here
        {
            if (tab == null)
            {
                EncodingText.Text = "";
                EolText.Text = "";
                UpdateEncodingChecks2(null);      // clears all check marks
                return;
            }
            EncodingText.Text = EncodingLabel(tab.FileEncoding, tab.FileHasBom);
            EolText.Text = EolLabel(tab.FileLineEnding);
            UpdateEncodingChecks2(tab);
        }

        public void UpdateStatusBar()
        {
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                PositionText.Text = $"Ln {tab.Line}, Col {tab.Column}";
                CharactersText.Text = $"{tab.CharacterCount:N0} characters";
                ZoomText.Text = $"{tab.ZoomSetting}%";
            }
        }

        private void ReadOnly_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is EditControl tab)
            {
                tab.IsReadOnlyMode = !tab.IsReadOnlyMode;
                UpdateReadOnlyState(tab);
                _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
            }
        }

        private void UpdateReadOnlyState(EditControl? tab)
        {
            bool hasTab = tab != null;
            bool ro = tab?.IsReadOnlyMode ?? false;
            bool canEdit = hasTab && !ro;

            ReadOnlyMenuItem.IsChecked = ro;

            // Commands that change the document
            EnableSave();
            CutMenuItem.IsEnabled = canEdit;
            CutButton.IsEnabled = canEdit;
            PasteMenuItem.IsEnabled = canEdit;
            PasteButton.IsEnabled = canEdit;
            EnableDelete();
            UndoMenuItem.IsEnabled = canEdit;
            UndoButton.IsEnabled = canEdit;
            RedoMenuItem.IsEnabled = canEdit;
            RedoButton.IsEnabled = canEdit;
            ReplaceMenuItem.IsEnabled = canEdit;
            ReplaceButton.IsEnabled = canEdit;

            if (tab != null)
            {
                UpdateTabHeader();
                WordWrapMenuItem.IsChecked = tab?.WordWrap ?? false;
            }
        }

        private void Encoding_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedContent is not EditControl tab) return;
            if (sender is not MenuItem { Tag: string key } ||
                !EncodingChoices.TryGetValue(key, out var choice)) return;

            if (choice.CodePage == tab.FileEncoding.CodePage)
            {
                // Same code page (e.g. UTF-8 <-> UTF-8-BOM): only the BOM flag changes
                tab.FileHasBom = choice.Bom;
            }
            else if (!string.IsNullOrEmpty(tab.TextFileName))
            {
                // The tab has a file on disk
                if (tab.IsModified)
                {
                    var answer = MessageBox.Show(
                        "This tab has unsaved changes.\n\n" +
                        "Yes = reload the file from disk with the new encoding (discards your changes)\n" +
                        "No = keep your text and save it with the new encoding",
                        "Radish Notepad", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (answer == MessageBoxResult.Cancel)
                    {
                        UpdateEncodingAndEol(tab);      // put the check mark back
                        return;
                    }

                    if (answer == MessageBoxResult.No)
                    {
                        // Keep the text as it is; only the encoding used on the next save changes
                        tab.FileEncoding = MakeEncoding(choice.CodePage);
                        tab.FileHasBom = choice.Bom;
                        UpdateEncodingAndEol(tab);
                        return;
                    }
                    // Yes: fall through to the reload below
                }

                // Reload the file from disk, decoding it with the chosen encoding
                tab.OpenFile(tab.TextFileName, MakeEncoding(choice.CodePage));
                tab.IsModified = false;
                UpdateTabHeader();
            }
            else
            {
                // New, unsaved document: only the save encoding changes
                tab.FileEncoding = MakeEncoding(choice.CodePage);
                tab.FileHasBom = choice.Bom;
            }

            UpdateEncodingAndEol(tab);
            UpdateStatusBar();
            UpdateReadOnlyState(tab);
            _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
        }

        private IEnumerable<MenuItem> EncodingItems() =>
            EncodingMenu.Items.OfType<MenuItem>()
            .SelectMany(m => m.Items.Count > 0 ? m.Items.OfType<MenuItem>() : new[] { m });

        private void UpdateEncodingChecks1(EditControl? tab)
        {
            foreach (var item in EncodingItems())
                item.IsChecked = tab != null
                    && item.Tag is string key
                    && EncodingChoices.TryGetValue(key, out var c)
                    && c.CodePage == tab.FileEncoding.CodePage
                    && (c.CodePage != 65001 || c.Bom == tab.FileHasBom);
        }
        private void UpdateEncodingChecks2(EditControl? tab)
        {
            foreach (MenuItem item in EncodingMenu.Items.OfType<MenuItem>())
            {
                item.IsChecked = tab != null
                    && item.Tag is string key
                    && EncodingChoices.TryGetValue(key, out var c)
                    && c.CodePage == tab.FileEncoding.CodePage
                    && (c.CodePage != 65001 || c.Bom == tab.FileHasBom);
            }
        }


        internal static readonly Dictionary<string, (int CodePage, bool Bom)> EncodingChoices = new()
        {
            ["ansi"] = (1252, false),
            ["utf8"] = (65001, false),
            ["utf8bom"] = (65001, true),
            ["utf16be"] = (1201, true),
            ["utf16le"] = (1200, true),
            ["oem437"] = (437, false),
            ["oem850"] = (850, false),
        };

        internal static Encoding MakeEncoding(int codePage) => codePage switch
        {
            65001 => new UTF8Encoding(false),
            1200 => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            1201 => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
            _ => Encoding.GetEncoding(codePage)
        };

        public void UpdateTabHeader()
        {
            if (tabControl.SelectedItem is not TabItem item) return;
            EditControl? tab = tabControl.SelectedContent as EditControl;
            if (tab != null)
            {
                string name = string.IsNullOrEmpty(tab.TextFileName)
                ? tab.TabName
                : System.IO.Path.GetFileName(tab.TextFileName);

                name = tab.IsReadOnlyMode ? "🔒 " + name : name;
                item.Header = tab.IsModified ? name + " ●" : name;

                item.ToolTip = string.IsNullOrEmpty(tab.TextFileName)
                    ? null
                    : tab.TextFileName;
            }
        }

        private void RefreshRecentFilesMenu()
        {
            RecentFilesMenuItem.Items.Clear();

            if (_recentFiles.Files.Count == 0)
            {
                RecentFilesMenuItem.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
                return;
            }

            foreach (string path in _recentFiles.Files)
            {
                var item = new MenuItem { Header = path };
                item.Click += RecentFile_Click;
                RecentFilesMenuItem.Items.Add(item);
            }

            RecentFilesMenuItem.Items.Add(new Separator());
            var clearItem = new MenuItem { Header = "Clear Recent Files" };
            clearItem.Click += ClearRecentFiles_Click;
            RecentFilesMenuItem.Items.Add(clearItem);
        }

        private void RecentFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Header is not string path) return;

            if (!File.Exists(path))
            {
                var result = MessageBox.Show(
                    $"'{path}' could not be found. Remove it from Recent Files?",
                    "Radish Notepad", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _recentFiles.Files.Remove(path);
                    _recentFiles.Save();
                    RefreshRecentFilesMenu();
                }
                return;
            }

            OpenFilePath(path);   // your shared "load this path into a tab" method
        }

        private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            _recentFiles.Files.Clear();
            _recentFiles.Save();
            RefreshRecentFilesMenu();
        }

        private void OpenFilePath(string path)
        {
            EditControl? current = tabControl.SelectedContent as EditControl;

            if (current == null || !current.IsEmptyTab)
            {
                New_Click(this, new RoutedEventArgs());                       // creates and selects a new tab
                current = tabControl.SelectedContent as EditControl;
            }

            if (current == null) return;                    // shouldn't happen, but stay safe

            if (current.Open_Click(path) && current.TextFileName != "")
            {
                string name = System.IO.Path.GetFileName(current.TextFileName);
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    selectedTab.Header = name;
                }
                if (new FileInfo(current.TextFileName).IsReadOnly)
                    current.IsReadOnlyMode = true;
                UpdateRecentFiles(current.TextFileName);
                UpdateEncodingAndEol(current);
                UpdateStatusBar();
                UpdateReadOnlyState(current);
                _findReplaceWindow?.UpdateReplaceAvailabilityIfOpen();
                current.IsModified = false;
                UpdateTabHeader();
            }
        }

        public void SetLastSearch(string text, bool matchCase, bool wrapAround)
        {
            _lastFindText = text;
            _lastMatchCase = matchCase;
            _lastWrapAround = wrapAround;
        }


    }
}