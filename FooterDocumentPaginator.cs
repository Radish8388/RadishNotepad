using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RadishNotepad
{
    public class FooterDocumentPaginator : DocumentPaginator
    {
        private readonly DocumentPaginator _inner;
        private readonly Size _pageSize;
        private const double MillimetersToDiu = 96.0 / 25.4;   // 96 units per inch, 25.4 mm per inch

        public FooterDocumentPaginator(DocumentPaginator inner, Size pageSize)
        {
            _inner = inner;
            _pageSize = pageSize;
            _inner.PageSize = pageSize;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage page = _inner.GetPage(pageNumber);

            var container = new ContainerVisual();
            container.Children.Add(page.Visual);

            string footer = $"Page {pageNumber + 1}";
            var formatted = new FormattedText(
                footer,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                Brushes.Black,
                VisualTreeHelper.GetDpi(page.Visual).PixelsPerDip);

            var footerVisual = new DrawingVisual();
            using (DrawingContext dc = footerVisual.RenderOpen())
            {
                double x = (_pageSize.Width - formatted.Width) / 2;
                double footerMarginDiu = 15 * MillimetersToDiu;   // same constant used elsewhere
                double y = _pageSize.Height - footerMarginDiu - formatted.Height;
                dc.DrawText(formatted, new Point(x, y));
            }
            container.Children.Add(footerVisual);

            return new DocumentPage(container, _pageSize, page.BleedBox, page.ContentBox);
        }

        public override bool IsPageCountValid => _inner.IsPageCountValid;
        public override int PageCount => _inner.PageCount;
        public override Size PageSize
        {
            get => _pageSize;
            set { /* fixed for this wrapper */ }
        }
        public override IDocumentPaginatorSource Source => _inner.Source;
    }
}
