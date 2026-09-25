using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for PrintPreviewWindow.xaml
    /// </summary>
    public partial class PrintPreviewWindow : Window
    {
        private Package? _xpsPackage;
        private XpsDocument? _xpsDoc;
        private Uri? _packageUri;

        public PrintPreviewWindow(DocumentPaginator paginator)
        {
            InitializeComponent();

            documentViewer.Document = BuildPreviewDocument(paginator);

            Closed += PrintPreviewWindow_Closed;
        }

        private FixedDocumentSequence BuildPreviewDocument(DocumentPaginator paginator)
        {
            var stream = new MemoryStream();
            _xpsPackage = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);

            string packageUriString = "memorystream://" + Guid.NewGuid().ToString() + ".xps";
            _packageUri = new Uri(packageUriString);
            PackageStore.AddPackage(_packageUri, _xpsPackage);

            _xpsDoc = new XpsDocument(_xpsPackage, CompressionOption.NotCompressed, packageUriString);
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(_xpsDoc);
            writer.Write(paginator);

            return _xpsDoc.GetFixedDocumentSequence();
        }

        private void PrintPreviewWindow_Closed(object? sender, EventArgs e)
        {
            _xpsDoc?.Close();
            _xpsDoc = null;

            if (_xpsPackage != null)
            {
                if (_packageUri != null)
                    PackageStore.RemovePackage(_packageUri);
                _xpsPackage.Close();
                _xpsPackage = null;
            }
        }
    }
}
