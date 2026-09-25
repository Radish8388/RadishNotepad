using System.Windows;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for GoToLine.xaml
    /// </summary>
    public partial class GoToLine : Window
    {
        public int LineNumber { get; set; }
        public GoToLine()
        {
            InitializeComponent();
            LineNumber = 0;
            LineNumberText.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string input = LineNumberText.Text;

            if (int.TryParse(input, out int number) && number > 0)
            {
                // Valid integer
                LineNumber = number;
                DialogResult = true;   // closes dialog if you're using ShowDialog()
            }
            else
            {
                MessageBox.Show("Please enter a valid number.",
                                "Invalid Input",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                LineNumberText.Focus();
                LineNumberText.SelectAll();
            }
        }
    }
}
