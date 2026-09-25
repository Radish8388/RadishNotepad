using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for EditControl.xaml
    /// </summary>
    public partial class EditControl : UserControl
    {
        public string TextFileName { get; set; } = "";
        public bool IsModified { get; set; } = false;
        public Encoding FileEncoding;
        public bool FileHasBom;
        public string FileLineEnding;
        public int Line = 1;
        public int Column = 1;
        public int CharacterCount = 0;
        public int ZoomSetting = 100;
        public bool HasSelection { get; set; } = false;
        public string TabName { get; set; } = "New 1";
        private bool _isReadOnlyMode = false;
        public bool IsReadOnlyMode
        {
            get => _isReadOnlyMode;
            set
            {
                _isReadOnlyMode = value;
                myTextBox.IsReadOnly = value;
            }
        }
        public bool IsEmptyTab =>
            string.IsNullOrEmpty(TextFileName) &&
            myTextBox.Text.Length == 0 &&
            !IsModified;

        private bool _wordWrap = false;
        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                _wordWrap = value;
                myTextBox.TextWrapping = value ? TextWrapping.Wrap : TextWrapping.NoWrap;
            }
        }
        public string SelectedText => myTextBox.SelectedText;

        public EditControl()
        {
            InitializeComponent();
            FileEncoding = Encoding.UTF8;
            FileHasBom = false;
            FileLineEnding = "\r\n";
            Line = 1;
            Column = 1;
            CharacterCount = 0;
            IsReadOnlyMode = false;
            IsModified = false;
            WordWrap = true;
        }

        private void myTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsModified = true;
            SetSaveState();
            CharacterCount = CountCharacters(myTextBox.Text);
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.UpdateStatusBar();
                mainWindow.UpdateTabHeader();
            }
        }

        private void myTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            HasSelection = myTextBox.SelectionLength > 0;
            (Line, Column) = GetLineAndColumn(myTextBox);
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.UpdateStatusBar();
                mainWindow.EnableDelete();
            }

        }

        #region file menu events
        public bool Open_Click(string filename)
        {
            try
            {
                // Read all contents from the file
                var file = ReadFile(filename);
                IsModified = false;
                SetSaveState();

                // copy file contents to TextBox
                myTextBox.IsUndoEnabled = false;
                myTextBox.Text = file.Text;
                myTextBox.IsUndoEnabled = true;
                TextFileName = filename;

                // store file.Encoding, file.HasBom, file.LineEnding on the tab for the status bar and for Save
                FileEncoding = file.Encoding;
                FileHasBom = file.HasBom;
                FileLineEnding = file.LineEnding;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
                return false;
            }
        }

        public void Save_Click()
        {
            if (TextFileName == "")
                SaveAs_Click();
            else
            {
                try
                {
                    WriteFile(TextFileName, myTextBox.Text, FileEncoding, FileHasBom, FileLineEnding);
                    IsModified = false;
                    SetSaveState();
                }
                catch (EncoderFallbackException)
                {
                    MessageBox.Show(
                        "This text contains characters that the current encoding can't store. " +
                        "Use Save As and choose a different encoding, such as UTF-8.",
                        "Radish Notepad", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}",
                                    "Radish Notepad", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveAs_Click()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text Files|*.txt";
            dlg.DefaultExt = ".txt";
            if (!string.IsNullOrEmpty(TextFileName))
            {
                dlg.FileName = System.IO.Path.GetFileName(TextFileName);
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(TextFileName);
            }
            if (dlg.ShowDialog() != true) return;

            // Ask which encoding to save with
            var picker = new EncodingPickerWindow(FileEncoding.CodePage, FileHasBom, FileLineEnding)
            {
                Owner = Window.GetWindow(this)
            };
            if (picker.ShowDialog() != true) return;

            var choice = MainWindow.EncodingChoices[picker.SelectedKey];
            Encoding enc = MainWindow.MakeEncoding(choice.CodePage);
            string eol = picker.SelectedLineEnding;

            try
            {
                WriteFile(dlg.FileName, myTextBox.Text, enc, choice.Bom, eol);
            }
            catch (EncoderFallbackException) { /* unchanged */ }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* unchanged */ }

            // Only after the write succeeded
            FileEncoding = enc;
            FileHasBom = choice.Bom;
            FileLineEnding = eol;
            TextFileName = dlg.FileName;
            IsModified = false;
            SetSaveState();

        }

        public void Print_Click()
        {
            PrintPreview_Click();
        }

        #endregion
        #region edit menu events
        public void Delete_Click()
        {
            if (myTextBox.IsReadOnly) return;
            myTextBox.SelectedText = "";
        }

        public void GoTo_Click()
        {
            GoToLine dialog = new GoToLine();
            if (Window.GetWindow(this) is MainWindow mainWindow)
                dialog.Owner = mainWindow;
            bool? result = dialog.ShowDialog(); // Modal dialog
            if (result == true)
            {
                GoToLine(dialog.LineNumber);
            }
        }
        #endregion
        #region view menu events
        #endregion

        public record LoadedFile(string Text, Encoding Encoding, bool HasBom, string LineEnding);

        public static LoadedFile ReadFile(string path, Encoding? forced = null)
        {
            byte[] bytes = File.ReadAllBytes(path);

            Encoding encoding;
            int bomLength = 0;

            if (forced != null)
            {
                encoding = forced;
                bomLength = encoding.CodePage switch
                {
                    65001 when bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF => 3,
                    1200 when bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE => 2,
                    1201 when bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF => 2,
                    _ => 0
                };
            }
            else
            {

                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    encoding = new UTF8Encoding(true);
                    bomLength = 3;
                }
                else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                {
                    encoding = Encoding.Unicode;              // UTF-16 LE
                    bomLength = 2;
                }
                else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                {
                    encoding = Encoding.BigEndianUnicode;     // UTF-16 BE
                    bomLength = 2;
                }
                else
                {
                    // No BOM: strict UTF-8 first, then fall back to ANSI
                    try
                    {
                        new UTF8Encoding(false, throwOnInvalidBytes: true)
                            .GetString(bytes);
                        encoding = new UTF8Encoding(false);
                    }
                    catch (DecoderFallbackException)
                    {
                        encoding = Encoding.GetEncoding(1252);
                    }
                }

            }

            string text = encoding.GetString(bytes, bomLength, bytes.Length - bomLength);
            return new LoadedFile(text, encoding, bomLength > 0, DetectLineEnding(text));

        }

        private static string DetectLineEnding(string text)
        {
            int crlf = 0, lf = 0, cr = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') { crlf++; i++; }
                    else cr++;
                }
                else if (text[i] == '\n') lf++;
            }

            if (crlf == 0 && lf == 0 && cr == 0) return "\r\n";   // no line breaks: use Windows default
            if (crlf >= lf && crlf >= cr) return "\r\n";
            return lf >= cr ? "\n" : "\r";
        }

        private void SetSaveState()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.EnableSave();
            }
        }

        public static void WriteFile(string path, string text,
                             Encoding encoding, bool hasBom, string lineEnding)
        {
            // Normalize whatever the TextBox returns, then apply the file's own style
            string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            if (lineEnding != "\n")
                normalized = normalized.Replace("\n", lineEnding);

            Encoding enc = encoding.CodePage switch
            {
                65001 => new UTF8Encoding(hasBom),
                1200 => new UnicodeEncoding(false, hasBom),
                1201 => new UnicodeEncoding(true, hasBom),
                _ => Encoding.GetEncoding(encoding.CodePage,
                             EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback)
            };

            byte[] body = enc.GetBytes(normalized);      // throws before the file is touched
            byte[] preamble = enc.GetPreamble();         // empty for the 8-bit code pages

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            fs.Write(preamble);
            fs.Write(body);
        }

        private static (int Line, int Col) GetLineAndColumn(TextBox textBox)
        {
            string text = textBox.Text;
            int caret = textBox.CaretIndex;

            int line = 1;
            int lineStart = 0;
            for (int i = 0; i < caret; i++)
            {
                char c = text[i];

                // A line break is: LF, or a CR that is not followed by LF.
                // (CRLF is therefore counted once, at its LF.)
                if (c == '\n' || (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                {
                    line++;
                    lineStart = i + 1;
                }
            }
            return (line, caret - lineStart + 1);
        }

        private static int CountCharacters(string text)
        {
            int count = text.Length;
            for (int i = 0; i + 1 < text.Length; i++)
            {
                if (text[i] == '\r' && text[i + 1] == '\n')
                {
                    count--;   // CRLF counts as one
                    i++;       // skip the LF
                }
            }
            return count;
        }

        public void OpenFile(string filename, Encoding enc)
        {
            try
            {
                // Read all contents from the file
                var file = ReadFile(filename, enc);

                // copy file contents to TextBox
                myTextBox.IsUndoEnabled = false;
                myTextBox.Text = file.Text;
                myTextBox.IsUndoEnabled = true;
                TextFileName = filename;

                // store file.Encoding, file.HasBom, file.LineEnding on the tab for the status bar and for Save
                FileEncoding = file.Encoding;
                FileHasBom = file.HasBom;
                FileLineEnding = file.LineEnding;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
            }
        }

        /// Returns true if it's okay to proceed (discard, save succeeded, or nothing to save).
        /// Returns false if the user cancelled.
        public bool ConfirmSaveIfModified()
        {
            if (!IsModified) return true;

            string name = string.IsNullOrEmpty(TextFileName)
                ? TabName
                : System.IO.Path.GetFileName(TextFileName);

            var answer = MessageBox.Show(
                $"Do you want to save changes to {name}?",
                "Radish Notepad", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Cancel) return false;
            if (answer == MessageBoxResult.No) return true;

            // Yes: save, using Save As if there's no file yet
            // Yes: save. A read-only tab can't overwrite its own file, so treat it like an
            // unsaved document and go through Save As instead.
            if (string.IsNullOrEmpty(TextFileName) || IsReadOnlyMode)
            {
                SaveAs_Click();
                return !IsModified;      // Save As sets IsModified = false only on success
            }
            else
            {
                Save_Click();            // however you name your existing Save method
                return !IsModified;
            }
        }

        public void SetZoom(int zoom)
        {
            ZoomSetting = zoom;
            myTextBox.FontSize = 16 * zoom / 100.0;
        }

        public void GoToLine(int targetLine)
        {
            string text = myTextBox.Text;
            int line = 1;
            int index = 0;

            if (targetLine <= 1)
            {
                myTextBox.CaretIndex = 0;
            }
            else
            {
                for (int i = 0; i < text.Length && line < targetLine; i++)
                {
                    char c = text[i];
                    if (c == '\n' || (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                    {
                        line++;
                        index = i + 1;
                    }
                }
                myTextBox.CaretIndex = index;
            }

            myTextBox.ScrollToLine(myTextBox.GetLineIndexFromCharacterIndex(myTextBox.CaretIndex));
            myTextBox.Focus();
        }

        public bool FindNext(string searchText, bool matchCase, bool wrapAround)
        {
            if (string.IsNullOrEmpty(searchText)) return false;

            string text = myTextBox.Text;
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int startAt = myTextBox.CaretIndex + myTextBox.SelectionLength;
            int found = text.IndexOf(searchText, startAt, comparison);

            if (found < 0 && wrapAround)
                found = text.IndexOf(searchText, 0, comparison);

            if (found < 0) return false;

            myTextBox.Select(found, searchText.Length);
            ScrollToShowSelection(found);
            myTextBox.Focus();
            return true;
        }

        public bool FindPrevious(string searchText, bool matchCase, bool wrapAround)
        {
            if (string.IsNullOrEmpty(searchText)) return false;

            string text = myTextBox.Text;
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int searchFrom = myTextBox.CaretIndex - 1;
            int found = searchFrom >= 0
                ? text.LastIndexOf(searchText, Math.Min(searchFrom, text.Length - 1), comparison)
                : -1;

            if (found < 0 && wrapAround)
                found = text.LastIndexOf(searchText, text.Length - 1, comparison);

            if (found < 0) return false;

            myTextBox.Select(found, searchText.Length);
            ScrollToShowSelection(found);
            myTextBox.Focus();
            return true;
        }

        public bool ReplaceCurrentSelection(string searchText, string replaceText, bool matchCase)
        {
            if (myTextBox.IsReadOnly) return false;

            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (myTextBox.SelectionLength == 0 ||
                !string.Equals(myTextBox.SelectedText, searchText, comparison))
                return false;   // current selection isn't a match; caller should Find first

            myTextBox.SelectedText = replaceText;
            myTextBox.CaretIndex += replaceText.Length;   // move past the replacement
            return true;
        }

        public int ReplaceAll(string searchText, string replaceText, bool matchCase)
        {
            if (myTextBox.IsReadOnly || string.IsNullOrEmpty(searchText)) return 0;

            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            string text = myTextBox.Text;
            int count = 0;
            int index = 0;
            var sb = new System.Text.StringBuilder();

            while (true)
            {
                int found = text.IndexOf(searchText, index, comparison);
                if (found < 0)
                {
                    sb.Append(text, index, text.Length - index);
                    break;
                }
                sb.Append(text, index, found - index);
                sb.Append(replaceText);
                index = found + searchText.Length;
                count++;
            }

            if (count > 0)
                myTextBox.Text = sb.ToString();   // single Text assignment: one undo step, not N

            return count;
        }

        private void ScrollToShowSelection(int foundIndex)
        {
            myTextBox.UpdateLayout();

            int lineIndex = myTextBox.GetLineIndexFromCharacterIndex(foundIndex);
            int targetLine = Math.Max(0, lineIndex - 3);

            myTextBox.ScrollToLine(targetLine);
            myTextBox.UpdateLayout();

            int guard = 0;
            while (lineIndex > myTextBox.GetLastVisibleLineIndex() && guard++ < 500)
            {
                myTextBox.LineDown();
                if (guard % 20 == 0) myTextBox.UpdateLayout();
            }
            while (lineIndex < myTextBox.GetFirstVisibleLineIndex() && guard++ < 500)
            {
                myTextBox.LineUp();
                if (guard % 20 == 0) myTextBox.UpdateLayout();
            }
        }

        private const double MillimetersToDiu = 96.0 / 25.4;   // 96 units per inch, 25.4 mm per inch

        public FlowDocument BuildPrintDocument(string text, double pageWidthDiu, double pageHeightDiu, double marginMm)
        {
            double margin = marginMm * MillimetersToDiu;

            var doc = new FlowDocument
            {
                PageWidth = pageWidthDiu,
                PageHeight = pageHeightDiu,
                PagePadding = new Thickness(margin),
                FontFamily = myTextBox.FontFamily,
                FontSize = myTextBox.FontSize,
                ColumnWidth = double.PositiveInfinity
            };
            double footerReserve = 10 * MillimetersToDiu;   // extra space above the footer's own 15mm zone
            doc.PagePadding = new Thickness(margin, margin, margin, margin + footerReserve);

            // Apply spacing via a Style, not per-instance properties — direct property
            // assignment on Paragraph is unreliable for LineHeight/LineStackingStrategy.
            var paragraphStyle = new Style(typeof(Paragraph));
            paragraphStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0)));
            paragraphStyle.Setters.Add(new Setter(Paragraph.LineHeightProperty, myTextBox.FontSize * 1.2));
            paragraphStyle.Setters.Add(new Setter(Paragraph.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight));
            paragraphStyle.Setters.Add(new Setter(Block.TextAlignmentProperty, TextAlignment.Left));
            doc.Resources.Add(typeof(Paragraph), paragraphStyle);

            string[] lines = SplitLogicalLines(text);
            Debug.WriteLine($"number of lines: {lines.Length}");

            foreach (string line in lines)
            {
                var paragraph = new Paragraph(new Run(line));
                doc.Blocks.Add(paragraph);
            }

            return doc;
        }

        private static string[] SplitLogicalLines(string text)
        {
            var lines = new List<string>();
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                {
                    int end = i;
                    if (c == '\n' && end > start && text[end - 1] == '\r')
                        end--;   // drop the \r that belongs to this \r\n pair

                    lines.Add(text.Substring(start, end - start));
                    start = i + 1;
                }
            }
            lines.Add(text.Substring(start));
            return lines.ToArray();
        }

        private void PrintPreview_Click()
        {
            double pageWidth = 8.5 * 96;
            double pageHeight = 11 * 96;

            var flowDoc = BuildPrintDocument(myTextBox.Text, pageWidth, pageHeight, marginMm: 15);
            var innerPaginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;
            var footerPaginator = new FooterDocumentPaginator(innerPaginator, new Size(pageWidth, pageHeight));

            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                var previewWindow = new PrintPreviewWindow(footerPaginator) { Owner = mainWindow };
                previewWindow.Show();
            }
        }
    }
}
