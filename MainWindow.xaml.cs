using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageSlicer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string workingFolder = "";
        private System.Windows.Controls.Image img;
        private SliceDrawing sliceDrawing = new SliceDrawing();

        private List<SlicedItem> itemSlices = new List<SlicedItem>();
        private Geometry currentGeometry;

        public MainWindow()
        {
            InitializeComponent();
            sliceButton.IsEnabled = false;
            saveButton.IsEnabled = false;
            this.Loaded += Window_Loaded;
            this.KeyDown += Window_KeyDown;
            this.MouseMove += Window_MouseMove;
            LoadCanvas();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LoadCanvas()
        {   
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                sliceDrawing.ClearSlicePreview(mainCanvas);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (sliceDrawing.previewPoints.Count < 1)
                return;

            Point mousePos = e.GetPosition(mainCanvas);

            sliceDrawing.CreateMouseLine(mainCanvas, mousePos);
        }

        private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(mainCanvas);

            if (sliceDrawing.isFinishedDrawing)
            {
                sliceDrawing.ClearSliceDrawing(mainCanvas);
                sliceDrawing.AddPreviewPoint(mainCanvas, mousePos);
                sliceButton.IsEnabled = false;
            }
            else
            {
                if (sliceDrawing.IsMouseCloseToSliceBegin(mousePos))
                {
                    sliceDrawing.FinishSliceDrawing(mainCanvas);
                    sliceButton.IsEnabled = true;
                }
                else
                {
                    sliceDrawing.AddPreviewPoint(mainCanvas, mousePos);
                }
            }
        }

        private void sliceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!sliceDrawing.isFinishedDrawing)
                return;

            var sliceGeometry = sliceDrawing.GetSliceGeometry(mainCanvas, img);

            var rtb = RenderSlice(sliceGeometry);

            AddNewSliceItem(rtb, sliceGeometry);

            CarveSliceIntoCurrentGeometry(sliceGeometry);
            img.Clip = currentGeometry;

            sliceDrawing.ClearSliceDrawing(mainCanvas);

            saveButton.IsEnabled = true;
        }
        private void CarveSliceIntoCurrentGeometry(PathGeometry sliceGeometry)
        {
            if (currentGeometry == null)
            {
                currentGeometry = new RectangleGeometry(new Rect(0, 0, img.ActualWidth, img.ActualHeight));
            }
            var excludeGeometry = Geometry.Combine(
                currentGeometry,
                sliceGeometry,
                GeometryCombineMode.Exclude,
                null);
            currentGeometry = excludeGeometry;
        }

        private RenderTargetBitmap RenderSlice(PathGeometry sliceGeometry)
        {
            Rect sliceBounds = sliceGeometry.Bounds;
            var rtb = new RenderTargetBitmap(
                (int)Math.Ceiling(sliceBounds.Width),
                (int)Math.Ceiling(sliceBounds.Height),
                96, 96,
                PixelFormats.Pbgra32);

            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                // in this context, the slice will be rendered at 0,0
                // so everything must be shifted by the slice bounds
                var to_slice_space = new TranslateTransform(-sliceBounds.X, -sliceBounds.Y);
                var shiftedSliceGeometry = sliceGeometry.Clone();
                shiftedSliceGeometry.Transform = to_slice_space;

                var renderGeometry = shiftedSliceGeometry;
                if (currentGeometry != null)
                {
                    var shiftedCurrentGeometry = currentGeometry.Clone();
                    shiftedCurrentGeometry.Transform = to_slice_space;
                    renderGeometry = Geometry.Combine(
                        shiftedCurrentGeometry,
                        shiftedSliceGeometry,
                        GeometryCombineMode.Intersect,
                        null);
                }
                dc.PushClip(renderGeometry);

                dc.DrawImage(
                    img.Source,
                    new Rect(
                        -sliceBounds.X,
                        -sliceBounds.Y,
                        img.ActualWidth,
                        img.ActualHeight));

                dc.Pop();
            }

            rtb.Render(visual);

            return rtb;
        }

        private void AddNewSliceItem(ImageSource source, PathGeometry sliceGeometry)
        {
            SlicedItem newSlice = new SlicedItem();
            newSlice.sliceImage.Source = source;
            newSlice.snapPoint = GetRectCenter(sliceGeometry.Bounds);
            newSlice.text.Text = "[" + itemSlices.Count + "] : (" + newSlice.snapPoint.X + ", " + newSlice.snapPoint.Y + ")";
            newSlice.Index = itemSlices.Count;

            SlicedItemsBox.Items.Add(newSlice);
            itemSlices.Add(newSlice);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (itemSlices.Count == 0)
                return;

            foreach (var slice in itemSlices)
            {
                string path = workingFolder + "\\" + slice.Index + ".png";
                BitmapSource bitmapSource = (BitmapSource)slice.sliceImage.Source;
                SaveImageToFile(bitmapSource, path);
            }

            OpenFolder(workingFolder);
        }

        public void SaveImageToFile(BitmapSource bitmapSource, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        public void OpenFolder(string folderPath)
        {
            Process.Start("explorer.exe", folderPath);
        }

        public Point GetRectCenter(Rect r) => new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2));

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            saveButton.IsEnabled = false;

            var filePath = PickFile();
            if (filePath == null || filePath == "") return;

            sliceDrawing.ClearEverything();
            RemoveItemSlices();

            workingFolder = System.IO.Path.GetDirectoryName(filePath);    

            BitmapImage bitmap = new BitmapImage(new Uri(filePath));

            img = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Width = 482,
                Height = 350
            };

            Canvas.SetLeft(img, mainCanvas.ActualWidth / 2 - img.Width / 2);
            Canvas.SetTop(img, mainCanvas.ActualHeight / 2 - img.Height / 2);

            mainCanvas.Children.Add(img);
        }

        private string PickFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Images (*.png;*.jpg)|*.png;*.jpg"; // Filter by extension
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return "";
        }

        private void RemoveItemSlices()
        {
            mainCanvas.Children.Clear();
            itemSlices.Clear();
            SlicedItemsBox.Items.Clear();
        }
    }

    public class SliceDrawing
    {
        public List<Point> previewPoints = new List<Point>();
        public bool isFinishedDrawing = false;

        private const double DISTANCE_TO_CLOSE_SLICE = 10;
        private List<Line> previewLines = new List<Line>();
        private Line mouseLine = new Line();
        public Polygon slicePoly = new Polygon();

        private readonly Brush PREVIEW_COLOR = Brushes.Black;
        private readonly Brush MOUSE_LINE_COLOR = Brushes.Black;
        private readonly Brush MOUSE_LINE_TO_CLOSE_COLOR = Brushes.Green;
        private readonly Brush SLICE_COLOR = Brushes.Blue;

        public void ClearEverything()
        {
            previewPoints.Clear();
            isFinishedDrawing = false;
            previewLines.Clear();
            mouseLine = new Line();
            slicePoly = new Polygon();
        }

        public void CreateMouseLine(Canvas canvas, Point mousePos)
        {
            if (previewPoints.Count < 1)
                return;

            Point lastSlicePos = previewPoints[previewPoints.Count - 1];

            mouseLine.Stroke = IsMouseCloseToSliceBegin(mousePos) ? MOUSE_LINE_TO_CLOSE_COLOR : MOUSE_LINE_COLOR;
            mouseLine.X1 = lastSlicePos.X;
            mouseLine.Y1 = lastSlicePos.Y;
            mouseLine.X2 = mousePos.X;
            mouseLine.Y2 = mousePos.Y;
            mouseLine.StrokeThickness = 2;
            mouseLine.StrokeDashArray = [4, 2];

            if (!canvas.Children.Contains(mouseLine))
            {
                canvas.Children.Add(mouseLine);
            }
        }

        public void AddPreviewPoint(Canvas canvas, Point point)
        {
            previewPoints.Add(point);
            AddPreviewLine(canvas);
        }

        public void AddPreviewLine(Canvas canvas)
        {
            if (previewPoints.Count <= 1)
                return;

            Line newLine = new Line();
            newLine.Stroke = PREVIEW_COLOR;
            newLine.X1 = previewPoints[previewPoints.Count - 2].X;
            newLine.Y1 = previewPoints[previewPoints.Count - 2].Y;
            newLine.X2 = previewPoints[previewPoints.Count - 1].X;
            newLine.Y2 = previewPoints[previewPoints.Count - 1].Y;
            newLine.StrokeThickness = 2;

            canvas.Children.Add(newLine);

            previewLines.Add(newLine);
        }

        public void ClearSlicePreview(Canvas canvas)
        {
            previewPoints.Clear();
            foreach (var line in previewLines)
            {
                canvas.Children.Remove(line);
            }
            previewLines.Clear();
            canvas.Children.Remove(mouseLine);
        }

        public void FinishSliceDrawing(Canvas canvas)
        {
            isFinishedDrawing = true;
            slicePoly.Stroke = SLICE_COLOR;
            slicePoly.StrokeThickness = 2;
            slicePoly.Points = new PointCollection(previewPoints);
            canvas.Children.Add(slicePoly);
            ClearSlicePreview(canvas);
        }

        public void ClearSliceDrawing(Canvas canvas)
        {
            isFinishedDrawing = false;
            canvas.Children.Remove(slicePoly);
        }

        public bool IsMouseCloseToSliceBegin(Point mousePos)
        {
            if (previewPoints.Count < 1)
                return false;

            Point firstSlicePos = previewPoints[0];
            double distance = (mousePos - firstSlicePos).Length;

            return distance < DISTANCE_TO_CLOSE_SLICE;
        }

        public PathGeometry GetSliceGeometry(Canvas canvas, Image image)
        {
            if (!isFinishedDrawing)
                return null;

            // Translate points from canvas space to image space
            PointCollection slicePoints = new PointCollection();
            var to_image_space = canvas.TransformToVisual(image);
            for (int i = 0; i < slicePoly.Points.Count; i++)
            {
                slicePoints.Add(to_image_space.Transform(slicePoly.Points[i]));
            }

            var segment = new PolyLineSegment
            {
                Points = new PointCollection(slicePoints.Skip(1))
            };

            var figure = new PathFigure
            {
                StartPoint = slicePoints[0],
                IsClosed = true
            };
            figure.Segments.Add(segment);

            var sliceGeometry = new PathGeometry();
            sliceGeometry.Figures.Add(figure);

            return sliceGeometry;
        }
    }
}