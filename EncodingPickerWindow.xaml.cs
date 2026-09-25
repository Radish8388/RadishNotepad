using System.Windows;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for EncodingPickerWindow.xaml
    /// </summary>
    public partial class EncodingPickerWindow : Window
    {
        private static readonly (string Key, string Label)[] EncodingItems =
{
            ("utf8",    "UTF-8"),
            ("utf8bom", "UTF-8 with BOM"),
            ("ansi",    "ANSI"),
            ("utf16le", "UTF-16 LE BOM"),
            ("utf16be", "UTF-16 BE BOM"),
            ("oem437",  "OEM 437 (US DOS)"),
            ("oem850",  "OEM 850 (W. European DOS)"),
        };

        private static readonly (string LineEnding, string Label)[] EolItems =
        {
            ("\r\n", "Windows (CRLF)"),
            ("\n",   "Unix (LF)"),
            ("\r",   "Macintosh (CR)"),
        };

        public string SelectedKey { get; private set; } = "utf8";
        public string SelectedLineEnding { get; private set; } = "\r\n";

        public EncodingPickerWindow(int currentCodePage, bool currentBom, string currentLineEnding)
        {
            InitializeComponent();

            int selectedEncoding = 0;
            for (int i = 0; i < EncodingItems.Length; i++)
            {
                EncodingCombo.Items.Add(EncodingItems[i].Label);
                var c = MainWindow.EncodingChoices[EncodingItems[i].Key];
                if (c.CodePage == currentCodePage &&
                    (c.CodePage != 65001 || c.Bom == currentBom))
                    selectedEncoding = i;
            }
            EncodingCombo.SelectedIndex = selectedEncoding;

            int selectedEol = 0;
            for (int i = 0; i < EolItems.Length; i++)
            {
                EolCombo.Items.Add(EolItems[i].Label);
                if (EolItems[i].LineEnding == currentLineEnding)
                    selectedEol = i;
            }
            EolCombo.SelectedIndex = selectedEol;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedKey = EncodingItems[EncodingCombo.SelectedIndex].Key;
            SelectedLineEnding = EolItems[EolCombo.SelectedIndex].LineEnding;
            DialogResult = true;
        }

    }
}
