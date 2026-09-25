using System.Windows;
using System.Windows.Input;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for FindReplaceWindow.xaml
    /// </summary>
    public partial class FindReplaceWindow : Window
    {
        private readonly MainWindow _owner;

        public FindReplaceWindow(MainWindow owner)
        {
            InitializeComponent();
            _owner = owner;

            FindTextBox.Text = _owner.LastFindText;
            MatchCaseCheckBox.IsChecked = _owner.LastMatchCase;
            WrapAroundCheckBox.IsChecked = _owner.LastWrapAround;

            UpdateReplaceAvailability();

            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

        private EditControl? CurrentTab => _owner.CurrentEditControl;

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentTab;
            if (tab == null) return;

            _owner.SetLastSearch(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);

            bool found = tab.FindNext(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);
            StatusText.Text = found ? "" : "Not found.";
        }

        private void FindPrevious_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentTab;
            if (tab == null) return;

            _owner.SetLastSearch(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);

            bool found = tab.FindPrevious(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);
            StatusText.Text = found ? "" : "Not found.";
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentTab;
            if (tab == null) return;

            _owner.SetLastSearch(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);

            if (!tab.ReplaceCurrentSelection(FindTextBox.Text, ReplaceTextBox.Text, MatchCaseCheckBox.IsChecked == true))
            {
                // Current selection isn't a match; find the next one instead of replacing blindly.
                tab.FindNext(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);
                return;
            }

            // Advance to the next match after a successful replace.
            tab.FindNext(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            var tab = CurrentTab;
            if (tab == null) return;

            _owner.SetLastSearch(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true, WrapAroundCheckBox.IsChecked == true);

            int count = tab.ReplaceAll(FindTextBox.Text, ReplaceTextBox.Text, MatchCaseCheckBox.IsChecked == true);
            StatusText.Text = $"{count} replacement(s) made.";
        }

        private void ReplaceModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool replaceMode = ReplaceModeCheckBox.IsChecked == true;
            var vis = replaceMode ? Visibility.Visible : Visibility.Collapsed;
            ReplaceLabel.Visibility = vis;
            ReplaceTextBox.Visibility = vis;
            ReplaceButton.Visibility = vis;
            ReplaceAllButton.Visibility = vis;
        }

        public void ShowReplaceMode()
        {
            ReplaceModeCheckBox.IsChecked = true;   // triggers ReplaceModeCheckBox_Changed, reveals the Replace row
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

        private void UpdateReplaceAvailability()
        {
            bool readOnly = CurrentTab?.IsReadOnlyMode ?? false;

            ReplaceModeCheckBox.IsEnabled = !readOnly;

            if (readOnly && ReplaceModeCheckBox.IsChecked == true)
                ReplaceModeCheckBox.IsChecked = false;   // force back to Find-only mode
        }

        public void UpdateReplaceAvailabilityIfOpen()
        {
            UpdateReplaceAvailability();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext_Click(sender, e);
                e.Handled = true;
            }
        }

        public void PopulateFromSelectionOrLastSearch()
        {
            string? selected = CurrentTab?.SelectedText;

            if (!string.IsNullOrEmpty(selected))
            {
                FindTextBox.Text = selected;
            }
            else if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                FindTextBox.Text = _owner.LastFindText;
                MatchCaseCheckBox.IsChecked = _owner.LastMatchCase;
                WrapAroundCheckBox.IsChecked = _owner.LastWrapAround;
            }

            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

    }
}
