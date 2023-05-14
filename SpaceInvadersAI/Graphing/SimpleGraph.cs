using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.Graphing;

/// <summary>
/// This is a very *simple* graph class to show progress of the AI.
/// I could over engineer it and make it flexible for any scenario, but I won't.
/// If you want more fancy graphs, please feel free to use a prebuilt library and save a lot of effort.
/// </summary>
internal class SimpleGraph : IDisposable
{
    //   ███     ███    █   █   ████    █       █████            ████   ████      █     ████    █   █
    //  █   █     █     ██ ██   █   █   █       █               █       █   █    █ █    █   █   █   █
    //  █         █     █ █ █   █   █   █       █               █       █   █   █   █   █   █   █   █
    //   ███      █     █ █ █   ████    █       ████            █       ████    █   █   ████    █████
    //      █     █     █   █   █       █       █               █  ██   █ █     █████   █       █   █
    //  █   █     █     █   █   █       █       █               █   █   █  █    █   █   █       █   █
    //   ███     ███    █   █   █       █████   █████            ████   █   █   █   █   █       █   █

    #region DEFAULT PENS AND FONTS
    /// <summary>
    /// Pen used to draw the axis.
    /// </summary>
    private static readonly Pen s_axisPen = new(Color.FromArgb(60, 255, 255, 255));

    /// <summary>
    /// This is used for the axis and marks on the axis.
    /// </summary>
    private static readonly Pen s_outlinePen = new(Color.FromArgb(25, 200, 200, 200));

    /// <summary>
    /// This is used as a default font to label the y axis.
    /// </summary>
    internal static readonly Font s_defaultAxisLabelFont = new("Arial", 7);

    /// <summary>
    /// This is used as a default font for the title.
    /// </summary>
    private static readonly Font s_defaultTitleFont = new("Arial", 7, FontStyle.Bold);
    #endregion

    /// <summary>
    /// This space before the vertical (from left) and horizontal axis (from bottom).
    /// </summary>
    internal const int c_marginPXForLabels = 40;

    /// <summary>
    /// This defines the gap at the top before the graph starts.
    /// We do it so there is space for the top numbers
    /// </summary>
    internal const int c_vertMarginTopPX = 10;

    /// <summary>
    /// This is the size of the graduations on the vertical axis.
    /// </summary>
    private readonly int graduationSizeYAxis = 100;

    /// <summary>
    /// This is the value the top mark on the graph allows. We typically ensure this
    /// is not too big, but big enough for MAX() data point.
    /// </summary>
    private int topMarkPX;

    /// <summary>
    /// This contains how many pixels each mark represents.
    /// </summary>
    private float yScale;

    /// <summary>
    /// This contains the height of the graph Bitmap.
    /// </summary>
    private readonly int heightPX;

    /// <summary>
    /// This contains the width of the graph Bitmap.
    /// </summary>
    private readonly int widthPX;

    /// <summary>
    /// This contains the MIN(500, number of data points)
    /// </summary>
    private int numberOfDataPoints;

    /// <summary>
    /// The maximum score used to calculate the top mark.
    /// </summary>
    private double maxValue;

    /// <summary>
    /// This is used for the graph line(s).
    /// </summary>
    private readonly List<Pen> linesPen = new();

    /// <summary>
    /// This contains a list of data points to plot, arranged into separate lists (one element per dataset).
    /// </summary>
    private readonly List<List<float>> data = new();

    /// Labels for the datasets.
    /// </summary>
    private readonly List<string> dataSetLabels = new();

    /// <summary>
    /// The title to appear on the graph. If it's one line, it will be centred at the bottom, and if multiple stack upwards from bottom right.
    /// </summary>
    private readonly string? graphTitle = "";

    /// <summary>
    /// This is used to draw the axis.
    /// </summary>
    private readonly Font axisFont;

    /// <summary>
    /// This is used to draw the title.
    /// </summary>
    private readonly Font titleFont;

    /// <summary>
    /// Standard IDisposable pattern.
    /// </summary>
    private bool disposedValue;

    /// <summary>
    /// This is the bottom left X,Y of the graph.
    /// </summary>
    internal static Point s_origin;

    /// <summary>
    /// For every "generation" we move this many pixels to the right.
    /// </summary>
    internal static float s_xToGeneration;

    /// <summary>
    /// Constructor. A rudimentary graph engine that renders to a Bitmap.
    /// </summary>
    /// <param name="width">Width of graph to draw.</param>
    /// <param name="height">Height of graph to draw.</param>
    /// <param name="label">Label to appear on the graph.</param>
    /// <param name="graduationSize">What size each tick mark is.</param>
    /// <param name="overrideAxisFont">(optional) font for the axis.</param>
    /// <param name="overrideTitleFont">(optional) font for the title.</param>
    /// <returns></returns>
    internal SimpleGraph(int width, int height, string label, int graduationSize = 100, Font? overrideAxisFont = null, Font? overrideTitleFont = null)
    {
        // both lines are dotted
        s_outlinePen.DashStyle = DashStyle.Dash;
        s_axisPen.DashStyle = DashStyle.Dot;

        overrideAxisFont ??= s_defaultAxisLabelFont;
        overrideTitleFont ??= s_defaultTitleFont;

        axisFont = overrideAxisFont;
        titleFont = overrideTitleFont;

        heightPX = height;
        widthPX = width;
        graphTitle = label;
        graduationSizeYAxis = graduationSize;

        // we compute these values in the AddData method
        topMarkPX = -1;
        numberOfDataPoints = -1;
        yScale = int.MaxValue;
        maxValue = 0;
    }

    /// <summary>
    /// Add datasets enable plotting of multiple lines on the same graph.
    /// </summary>
    /// <param name="dataset">Data points.</param>
    /// <param name="lineColour">Colour of the lines.</param>
    /// <param name="datasetLabel">The label for the dataset. If too long, they will not all fit on the graph. It won't don't resize the bottom section to accommodate</param>
    /// <returns>Graph object to enable chaining of them.</returns>
    internal SimpleGraph AddDataSet(List<float> dataset, Color lineColour, string datasetLabel)
    {
        // store the pen and colour for this line
        linesPen.Add(new(lineColour));
        linesPen[^1].EndCap = LineCap.RoundAnchor;
        linesPen[^1].StartCap = LineCap.RoundAnchor;

        // 500 points spaced out, until we have more than 500 points, then we use all points
        numberOfDataPoints = dataset.Count;  //  Math.Max(numberOfDataPoints, dataset.Count <= 500 ? 500 : dataset.Count);

        // Data varies in max (all start at 0), so we want to size the graph accordingly.
        // Having a vertical axis 0..10,000 won't work well for "100".
        maxValue = Math.Max(maxValue, dataset.Max());

        data.Add(dataset);
        dataSetLabels.Add(datasetLabel);

        return this;
    }

    /// <summary>
    /// Draw graph to Bitmap sized widthPX by heightPX containing datasets with a title.
    /// </summary>
    /// <returns></returns>
    internal Bitmap Plot()
    {
        // To ensure we size accordingly, we divide the score into chunks of graduation size.
        maxValue = (Math.Floor(maxValue / graduationSizeYAxis) + 1) * graduationSizeYAxis;

        topMarkPX = Math.Max(topMarkPX, (int)maxValue);

        // this is used to scale the Y axis, taking into account the margin at the top and bottom
        yScale = Math.Min(yScale, (heightPX - c_marginPXForLabels - c_vertMarginTopPX) / (float)maxValue);

        Bitmap bitmapGraph = new(widthPX, heightPX);

        using Graphics graphics = Graphics.FromImage(bitmapGraph);

        // grey background
        graphics.Clear(Color.FromArgb(40, 40, 40));

        // good quality lines (not pixelated)
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        DrawGraphBackgroundOutline(graphics);

        // plot each set of data (as lines)
        int dataSetNumber = 0;

        foreach (List<float> scoresToPlot in data)
        {
            DrawPerformanceDataLinePoints(scoresToPlot.ToArray(), graphics, linesPen[dataSetNumber]);

            ++dataSetNumber;
        }

        string[] datasetLabels = dataSetLabels.ToArray();

        int xPositionOfLineAndLabel = widthPX;

        for (dataSetNumber = 0; dataSetNumber < dataSetLabels.Count; dataSetNumber++)
        {
            DrawDataSetLabel(ref xPositionOfLineAndLabel, datasetLabels[dataSetNumber], graphics, linesPen[dataSetNumber]);
        }

        // add the title to the bottom of the graph
        WriteTitleOnGraph(graphics);

        graphics.Flush();

        // used so a generation line can be accurately plotted against all graphs
        s_origin = GetPointOnGraph(0, 0);

        // for every "generation" we move this many pixels to the right
        s_xToGeneration = (widthPX - c_marginPXForLabels * 2f) / numberOfDataPoints;

        return bitmapGraph;
    }

    /// <summary>
    /// Draws a label for the dataset at the bottom of the graph.
    /// We go right to left.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="dataSetLabel"></param>
    /// <param name="graphics"></param>
    /// <param name="pen"></param>
    private void DrawDataSetLabel(ref int x, string dataSetLabel, Graphics graphics, Pen pen)
    {
        // o--o {label}..|
        // ^ line ^ label

        // how big is the label?
        Size sz = graphics.MeasureString(dataSetLabel, axisFont).ToSize();

        // move left by the size of the label plus a bit of padding, keeping it away from the previous label, and edge of screen.
        x -= sz.Width + 10;
        graphics.DrawString(dataSetLabel, axisFont, Brushes.Silver, x, heightPX - sz.Height - 5);

        // allow for the "small" line
        x -= 15;

        int yMiddleOfLabel = heightPX - sz.Height / 2 - 5;
        // draw a line near the label in the correct colour.
        graphics.DrawLine(pen, x, yMiddleOfLabel, x + 10, yMiddleOfLabel);
    }

    /// <summary>
    /// Writes the title to the graph.
    /// If it's multiple lines, it's right aligned stacked at the bottom of the graph.
    /// If it's a single line, it's centred at the bottom of the graph.
    /// </summary>
    /// <param name="graphics"></param>
    private void WriteTitleOnGraph(Graphics graphics)
    {
        if (string.IsNullOrEmpty(graphTitle)) return; // no title

        // position label in bottom right corner.
        Size sz = graphics.MeasureString(graphTitle, titleFont).ToSize();

        if (graphTitle.Contains('\n') || graphTitle.Contains('\r'))
        {
            // multiple lines, draw right aligned at bottom of graph
            graphics.DrawString(graphTitle, titleFont, Brushes.Silver, widthPX - 5 - sz.Width, heightPX - c_marginPXForLabels - sz.Height);
        }
        else
        {
            // draw centred at bottom of graph
            graphics.DrawString(graphTitle, titleFont, Brushes.Silver, widthPX / 2 - sz.Width / 2, heightPX - c_marginPXForLabels + 5); // slightly below the axis
        }
    }

    /// <summary>
    /// Draw the background: 2 axis, tickMarks with labels
    ///   ...
    ///   4|---------
    ///   3|---------
    ///   2|---------
    ///    +----------
    ///     
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawGraphBackgroundOutline(Graphics graphics)
    {
        int verticalOrigin = heightPX - c_marginPXForLabels;

        int distanceForLastTickMark = int.MaxValue;

        // draw the tick marks on the vertical axis, with corresponding label.
        for (float tickMark = 0; tickMark <= topMarkPX; tickMark += graduationSizeYAxis)
        {
            Point pointOfTickMark = GetPointOnGraph(0, (int)tickMark);
            string label = Math.Round(tickMark, 2).ToString();

            // we size the label, so we can center vertically on the tick mark, and right-align it (for aesthetics).
            SizeF labelSize = graphics.MeasureString(label, axisFont);

            // don't allow tick marks to be too close together
            if (distanceForLastTickMark - pointOfTickMark.Y < Math.Round(labelSize.Height * 1.8)) continue;

            distanceForLastTickMark = pointOfTickMark.Y; // closeness of tick marks derivation

            // draw horizontal line along each tick mark e.g.
            // 300 -|---------------------
            //      |
            // 200 -|---------------------
            //      |
            // 100 -|---------------------
            // ^   ^ and ^ line
            // label
            graphics.DrawLine(s_outlinePen,
                              c_marginPXForLabels - 5, pointOfTickMark.Y,
                              widthPX - 5, pointOfTickMark.Y); // horizontal line, 5px makes it provide tick mark

            graphics.DrawString(label, axisFont, Brushes.White, c_marginPXForLabels - 5 - labelSize.Width, pointOfTickMark.Y - labelSize.Height / 2);
        }

        // horizontal axis -----------------
        graphics.DrawLine(s_axisPen, c_marginPXForLabels, verticalOrigin, widthPX, verticalOrigin);

        // vertical axis  |
        //                |
        graphics.DrawLine(s_axisPen, c_marginPXForLabels, c_vertMarginTopPX, c_marginPXForLabels, verticalOrigin);
    }

    /// <summary>
    /// Gets the point on the graph for "x" and "y", taking into account the scaling.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private Point GetPointOnGraph(int x, float y)
    {
        // scale the data-points to fit on the graph

        // TODO: it's a little inefficient, we should store the X scaling
        int xPos = (int)Math.Round(c_marginPXForLabels + x * (widthPX - c_marginPXForLabels * 2f) / numberOfDataPoints); // *2 because we have a margin on both sides (right for the value label)
        int yPos = (int)Math.Round(heightPX - c_marginPXForLabels - (float)y * yScale);

        return new Point(xPos, yPos);
    }

    /// <summary>
    /// Plots the data-points on the graph.
    /// </summary>
    /// <param name="arrayOfScores"></param>
    /// <param name="graphics"></param>
    private void DrawPerformanceDataLinePoints(float[] arrayOfScores, Graphics graphics, Pen line)
    {
        List<Point> pointsToDraw = new();

        // add all the points so we can draw in 1 call to GDI

        int scale = Math.Max((int)Math.Round(arrayOfScores.Length / (widthPX - (float)c_marginPXForLabels - 10f)), 1);

        for (int generation = 0; generation < arrayOfScores.Length; generation += scale)
        {
            pointsToDraw.Add(GetPointOnGraph(generation, arrayOfScores[generation]));
        }

        // draw the graph of points. It won't do it for one data-point.
        if (pointsToDraw.Count > 1)
        {
            graphics.DrawLines(line, pointsToDraw.ToArray());

            // write label to the right of the last point
            Point lastPoint = pointsToDraw[^1];
            string label = arrayOfScores[^1].ToString();
            SizeF labelSize = graphics.MeasureString(label, axisFont);
            graphics.DrawString(label, axisFont, Brushes.White, lastPoint.X + 5, lastPoint.Y - labelSize.Height / 2);
        }
        else
        {
            graphics.FillEllipse(Brushes.White, pointsToDraw[0].X - 2, pointsToDraw[0].Y - 2, 4, 4); // draw a dot
        }
    }

    /// <summary>
    /// IDisposable mechanism for object destruction.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                data.Clear();

                // dispose of the pens
                foreach (Pen pen in linesPen)
                {
                    pen.Dispose();
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}