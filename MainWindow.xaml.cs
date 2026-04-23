using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace ImageSlicer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Image img;
        private SliceDrawing sliceDrawing = new SliceDrawing();
        public MainWindow()
        {
            InitializeComponent();
            sliceButton.IsEnabled = false;
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
            // 1. Create a BitmapImage from a URI
            BitmapImage bitmap = new BitmapImage(new Uri("C:\\Users\\pedro\\Pictures\\paris.jpg"));

            // 2. Create the Image control
            img = new Image
            {
                Source = bitmap,
                Width = 482,
                Height = 350
            };

            mainCanvas.Children.Add(img);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(img, mainCanvas.ActualWidth / 2 - img.ActualWidth / 2);
            Canvas.SetTop(img, mainCanvas.ActualHeight / 2 - img.ActualHeight / 2);
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

        
    }

    public class SliceDrawing
    {
        public List<Point> previewPoints = new List<Point>();
        public bool isFinishedDrawing = false;

        private const double DISTANCE_TO_CLOSE_SLICE = 10;
        private List<Line> previewLines = new List<Line>();
        private Line mouseLine = new Line();
        private Polygon slicePoly = new Polygon();

        private readonly Brush PREVIEW_COLOR = Brushes.Black;
        private readonly Brush MOUSE_LINE_COLOR = Brushes.Black;
        private readonly Brush MOUSE_LINE_TO_CLOSE_COLOR = Brushes.Green;
        private readonly Brush SLICE_COLOR = Brushes.Blue;

        public void CreateMouseLine(Canvas canvas, Point mousePos)
        {
            if (previewPoints.Count <= 1)
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
    }
}