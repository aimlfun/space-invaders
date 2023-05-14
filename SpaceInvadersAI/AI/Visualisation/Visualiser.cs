using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.ExternalInterface;
using SpaceInvadersAI.AI.Utilities;
using System.Drawing.Drawing2D;

namespace SpaceInvadersAI.AI.Visualisation;

/// <summary>
/// Visualiser for "brains". Draws each neuron as a blob, with arrows between them.
/// </summary>
internal class Visualiser
{
    //  ████    ████      █      ███    █   █           █   █    ███     ███    █   █     █     █        ███     ███    █████   ████
    //  █   █   █   █    █ █      █     █   █           █   █     █     █   █   █   █    █ █    █         █     █   █   █       █   █
    //  █   █   █   █   █   █     █     ██  █           █   █     █     █       █   █   █   █   █         █     █       █       █   █
    //  ████    ████    █   █     █     █ █ █           █   █     █      ███    █   █   █   █   █         █      ███    ████    ████
    //  █   █   █ █     █████     █     █  ██           █   █     █         █   █   █   █████   █         █         █   █       █ █
    //  █   █   █  █    █   █     █     █   █            █ █      █     █   █   █   █   █   █   █         █     █   █   █       █  █
    //  ████    █   █   █   █    ███    █   █             █      ███     ███     ███    █   █   █████    ███     ███    █████   █   █

    /// <summary>
    /// The brain to visualise, from constructor.
    /// </summary>
    private readonly Brain brainToVisualise;

    /// <summary>
    /// Font used for rendering the visualiser.
    /// </summary>
    internal static readonly Font s_fontForVisualisation = new("Segoe UI", 7);

    /// <summary>
    /// Indicates whether visualisations are enabled. If false, no visualisations are rendered.
    /// </summary>
    internal static bool s_visualisationsEnabled = false;

    /// <summary>
    /// Where connections can attached to the cell perimeter?
    /// </summary>
    private readonly HashSet<Point> attachedPoints = new();

    /// <summary>
    /// Custom line cap for drawing the arrow between brain cells.
    /// </summary>
    private readonly CustomLineCap InOutBaseLineCap;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    internal Visualiser(Brain brain)
    {
        brainToVisualise = brain;

        // arrow
        Point[] arrowForVisualiser = new Point[9] { new Point(0, 0), new Point(-4, -8), new Point(4, -8), new Point(0, 0),
                                     new Point(-4, 0), new Point(4, 0), new Point(4, 2), new Point(-4,2), new Point(-4, 0)};

        GraphicsPath graphicsPath = new();
        graphicsPath.AddPolygon(arrowForVisualiser);
        graphicsPath.CloseAllFigures();

        InOutBaseLineCap = new CustomLineCap(null, graphicsPath);
    }

    /// <summary>
    /// Render a diagram of the brain, and save to a .png file. _if_ s_visualisationsEnabled= = true
    /// </summary>
    /// <param name="pngFileName"></param>
    internal void RenderAndSaveDiagramToPNG(string pngFileName)
    {
        if (!s_visualisationsEnabled) return;

        Dictionary<string, VisualiserOutputCell> cellsToDraw = new();

        Dictionary<string, List<string>> linksToDraw = new();
        List<string> idsProcessed = new();

        Size requiredCanvasSize = PerformLayout(ref cellsToDraw, ref linksToDraw, ref idsProcessed);

        using Bitmap visualisationPNG = new(requiredCanvasSize.Width, requiredCanvasSize.Height); // this is the image we are drawing on

        using Graphics visualisationGraphics = Graphics.FromImage(visualisationPNG);

        visualisationGraphics.Clear(Color.White);
        visualisationGraphics.CompositingQuality = CompositingQuality.HighQuality; // otherwise everything is pixelated
        visualisationGraphics.SmoothingMode = SmoothingMode.HighQuality; // otherwise everything is pixelated
        visualisationGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // arrow between lines needs to be a meaningful size, the default is barely noticeable
        using Pen penLineWithArrowBetweenBrainCells = new(Color.FromArgb(150, 0, 0, 0));
        penLineWithArrowBetweenBrainCells.DashStyle = DashStyle.Solid;
        penLineWithArrowBetweenBrainCells.CustomEndCap = new AdjustableArrowCap(4, 4);

        using Pen penLineWithForTransistorBase = new(Color.FromArgb(250, 0, 0, 100));
        penLineWithForTransistorBase.DashStyle = DashStyle.Dot;
        penLineWithForTransistorBase.CustomEndCap = InOutBaseLineCap;

        DrawConnectionsBetweenCells(cellsToDraw, linksToDraw, visualisationGraphics, penLineWithArrowBetweenBrainCells, penLineWithForTransistorBase);

        foreach (string id in cellsToDraw.Keys)
        {
            cellsToDraw[id].Draw(visualisationGraphics);
        }

        // add the brain name as a title to the top
        SizeF sizeTitle = visualisationGraphics.MeasureString(brainToVisualise.Name, s_fontForVisualisation);
        visualisationGraphics.DrawString(brainToVisualise.Name, s_fontForVisualisation, Brushes.Blue, new PointF(requiredCanvasSize.Width / 2 - sizeTitle.Width / 2, 5));

        visualisationGraphics.Flush();

        visualisationPNG.Save(pngFileName);
    }

    /// <summary>
    /// Works out depths, and sizes required, plus links between brain cells.
    /// </summary>
    /// <param name="cellsToDraw"></param>
    /// <param name="linksToDraw"></param>
    /// <param name="idsProcessed"></param>
    /// <returns></returns>
    private Size PerformLayout(
        ref Dictionary<string, VisualiserOutputCell> cellsToDraw,
        ref Dictionary<string, List<string>> linksToDraw,
        ref List<string> idsProcessed)
    {
        using Bitmap b = new(1, 1);
        using Graphics graphics = Graphics.FromImage(b);

        int depth = 0;

        foreach (NeuralNetwork n in brainToVisualise.Networks.Values)
        {
            foreach (OUTPUTCell output in n.Outputs.Values)
            {
                RecursivelyWorkOutNodesAndConnectionsToDrawLater(graphics, output, ref idsProcessed, ref cellsToDraw, ref linksToDraw, depth);
            }
        }

        int maxDepth = GetMaxInputDepth(cellsToDraw);
       
        return LayoutCells(cellsToDraw);
    }

    /// <summary>
    /// Draw arrows between interconnected cells.
    /// </summary>
    /// <param name="cellsToDraw"></param>
    /// <param name="linksToDraw"></param>
    /// <param name="visualisationGraphics"></param>
    /// <param name="penLineWithArrowBetweenBrainCells"></param>
    /// <param name="penLineWithForTransistorBase"></param>
    private static void DrawConnectionsBetweenCells(
        Dictionary<string, VisualiserOutputCell> cellsToDraw,
        Dictionary<string, List<string>> linksToDraw,
        Graphics visualisationGraphics,
        Pen penLineWithArrowBetweenBrainCells,
        Pen penLineWithForTransistorBase)
    {
        foreach (string id in linksToDraw.Keys)
        {
            foreach (string id2 in linksToDraw[id])
            {
                GetNearestPointOnCircumference(cellsToDraw[id].Position, cellsToDraw[id2].Position, out Point startPoint, out Point endPoint);

                Connection? con = GetConnection(cellsToDraw[id].Cell, cellsToDraw[id2].Cell);

                int icn = GetInboundConnectionNumber(cellsToDraw[id].Cell.Id, cellsToDraw[id2].Cell.InboundConnections);

                // self connection
                if (id == id2)
                {
                    DrawSelfConnection(visualisationGraphics, ref startPoint);
                }
                else
                {
                    visualisationGraphics.DrawLine(
                        GetArrow(cellsToDraw[id2].Cell, icn,
                        penLineWithForTransistorBase, penLineWithArrowBetweenBrainCells),
                        startPoint, endPoint);
                }

                visualisationGraphics.DrawString(ConnectorLabel(cellsToDraw[id2].Cell, icn), s_fontForVisualisation, Brushes.BlueViolet, endPoint);

                if (con != null) WriteWeightBetweenNeurons(visualisationGraphics, startPoint, endPoint, con.Weight);
            }
        }
    }

    /// <summary>
    /// Determines which pen applies based on the cell type and the connection number.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="icn"></param>
    /// <param name="pen1"></param>
    /// <param name="pen2"></param>
    /// <returns></returns>
    private static Pen GetArrow(BaseCell cell, int icn, Pen pen1, Pen pen2)
    {
        if (cell.Type == CellType.IF && icn == 0) return pen1;
        if (cell.Type == CellType.TRANSISTOR && icn == 0) return pen1;

        return pen2;
    }

    /// <summary>
    /// Returns a specific label depending on the role the connection plays.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="icn"></param>
    /// <returns></returns>
    private static string ConnectorLabel(BaseCell cell, int icn)
    {
        if (cell.Type == CellType.TRANSISTOR)
        {
            if (icn == 0) return "B";

            return icn < 0 ? "E" : "C";
        }

        if (cell.Type == CellType.IF)
        {
            if (icn == 0) return "B";
            if (icn == 1) return "I1";
            if (icn == 2) return "I2";
            return "?";
        }

        return icn.ToString();
    }

    /// <summary>
    /// Determines the number of the connection from the id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="inboundConnections"></param>
    /// <returns></returns>
    private static int GetInboundConnectionNumber(string id, List<Connection> inboundConnections)
    {
        int i = 0;

        foreach (Connection connection in inboundConnections)
        {
            if (connection.From == id) return i;
            ++i;
        }

        return -1;
    }

    /// <summary>
    /// Draws a loop from the neuron to itself.
    /// </summary>
    /// <param name="visualisationGraphics"></param>
    /// <param name="locationStartInCenterOfCircle"></param>
    private static void DrawSelfConnection(Graphics visualisationGraphics, ref Point locationStartInCenterOfCircle)
    {
        int xPointOnCircumference = (int)(Math.Cos(0) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.X);
        int yPointOnCircumference = (int)(Math.Sin(0) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.Y);

        int xPointOnCircumference2 = (int)(Math.Cos(Math.PI / 4) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.X);
        int yPointOnCircumference2 = (int)(Math.Sin(Math.PI / 4) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.Y);

        int xPointOnCircumference3 = (int)(Math.Cos(Math.PI / 8) * VisualiserOutputCell.DiameterOfCirclePX + 15 + locationStartInCenterOfCircle.X);
        int yPointOnCircumference3 = (int)(Math.Sin(Math.PI / 8) * VisualiserOutputCell.DiameterOfCirclePX + 15 + locationStartInCenterOfCircle.Y);

        AdjustableArrowCap bigArrow = new(4, 4);

        using Pen p = new(Color.Purple);
        p.DashStyle = DashStyle.Dot;
        p.CustomEndCap = bigArrow;

        DrawArc(visualisationGraphics, p,
                                new Point(xPointOnCircumference, yPointOnCircumference),
                                new Point(xPointOnCircumference2, yPointOnCircumference2),
                                new Point(xPointOnCircumference3, yPointOnCircumference3));

        locationStartInCenterOfCircle = new Point(xPointOnCircumference, yPointOnCircumference - 10);
    }

    /// <summary>
    /// Draws an arc between the 3 points; used in the self-connection.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="p"></param>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <param name="Mid"></param>
    internal static void DrawArc(Graphics graphics, Pen p, Point Start, Point End, Point Mid)
    {
        int dx = Math.Abs(Mid.X - Start.X);
        int dy = Math.Abs(Mid.Y - End.Y);

        graphics.DrawArc(p, new Rectangle(Start.X - dx, Start.Y - dy, dx, dy), 135F, -270.0F);
    }

    /// <summary>
    /// Writes a label of "weight" between connections.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="weight"></param>
    private static void WriteWeightBetweenNeurons(Graphics graphics, Point startPoint, Point endPoint, double weight)
    {
        string label = weight.ToString();

        GraphicsState graphicsState = graphics.Save();

        double angle = Math.Atan(endPoint.X - startPoint.X == 0 ? 0 : (endPoint.Y - startPoint.Y) / (float)(endPoint.X - startPoint.X));
        graphics.TranslateTransform((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
        graphics.RotateTransform((float)(180 / Math.PI * angle));
        graphics.DrawString(label, s_fontForVisualisation, Brushes.Red, new PointF(0, 0));

        graphics.Restore(graphicsState);
    }

    /// <summary>
    /// Based on 2 cells, return the "connection" linking them.
    /// </summary>
    /// <param name="cell1"></param>
    /// <param name="cell2"></param>
    /// <returns></returns>
    private static Connection? GetConnection(BaseCell cell1, BaseCell cell2)
    {
        foreach (Connection con in cell1.InboundConnections)
        {
            if (con.To == cell2.Id) return con;
        }

        foreach (Connection con in cell1.OutboundConnections)
        {
            if (con.From == cell2.Id) return con;
        }

        return null;
    }

    /// <summary>
    /// We want the arrows to join edges of circles, not center of the circle.
    /// To do this, we walk around the circumference split into 20 points (arbitrary).
    /// Which ever of those points is closest to the other end of the line is the
    /// point we attach. We do for both ends.
    /// </summary>
    /// <param name="locationStartInCenterOfCircle"></param>
    /// <param name="locationEndInCenterOfCircle"></param>
    /// <param name="startPointForLine"></param>
    /// <param name="endPointForLineArrowIsDrawn"></param>
    private static void GetNearestPointOnCircumference(
        Point locationStartInCenterOfCircle,
        Point locationEndInCenterOfCircle,
        out Point startPointForLine,
        out Point endPointForLineArrowIsDrawn)
    {
        int pointsOnCircumference = 8;

        startPointForLine = locationStartInCenterOfCircle;
        endPointForLineArrowIsDrawn = locationEndInCenterOfCircle;

        double closestStartDistanceOfLineFromCircleCenter = int.MaxValue; // we haven't found a point on the circumference close to start 
        double closestEndDistanceOfLineFromCircleCenter = int.MaxValue; // we haven't found a point on the circumference close to end

        // around the circle in 18 degree steps
        for (double radians = 0; radians < 2 * Math.PI; radians += (double)(2 * Math.PI / pointsOnCircumference))
        {
            // determine a point on the circumference based on 18 degree step
            int xPointOnCircumference = (int)(Math.Cos(radians) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.X); // on circumference of start circle
            int yPointOnCircumference = (int)(Math.Sin(radians) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationStartInCenterOfCircle.Y);

            //if (attachedPoints.Contains(new Point(xPointOnCircumference, yPointOnCircumference))) continue;

            // how far is the point from the end point
            double distanceBetweenPoints = Utils.DistanceBetweenTwoPoints(new PointF(xPointOnCircumference, yPointOnCircumference), locationEndInCenterOfCircle);

            // is this point the closest to the end point?
            if (distanceBetweenPoints < closestStartDistanceOfLineFromCircleCenter)
            {
                // yes, store it.
                closestStartDistanceOfLineFromCircleCenter = distanceBetweenPoints;
                startPointForLine = new Point(xPointOnCircumference, yPointOnCircumference);
            }
        }

        for (double radians = 0; radians < 2 * Math.PI; radians += (double)(2 * Math.PI / pointsOnCircumference))
        {
            // determine a point on the circumference based on 18 degree step
            int xPointOnCircumference = (int)(Math.Cos(radians) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationEndInCenterOfCircle.X); // on circumference of end circle
            int yPointOnCircumference = (int)(Math.Sin(radians) * VisualiserOutputCell.DiameterOfCirclePX / 2 + locationEndInCenterOfCircle.Y);

            //if (attachedPoints.Contains(new Point(xPointOnCircumference, yPointOnCircumference))) continue;

            // how far is the point from the start point
            double distanceBetweenPoints = Utils.DistanceBetweenTwoPoints(new PointF(xPointOnCircumference, yPointOnCircumference), locationStartInCenterOfCircle);

            // is this point the closest to the start point?
            if (distanceBetweenPoints < closestEndDistanceOfLineFromCircleCenter)
            {
                // yes, store it.
                closestEndDistanceOfLineFromCircleCenter = distanceBetweenPoints;
                endPointForLineArrowIsDrawn = new Point(xPointOnCircumference, yPointOnCircumference);
            }
        }

        //attachedPoints.Add(startPointForLine);
        //attachedPoints.Add(endPointForLineArrowIsDrawn);
    }

    /// <summary>
    /// Determines the maximum depth for the inputs, then apply to all inputs.
    /// </summary>
    /// <param name="cellsToDraw"></param>
    /// <returns></returns>
    private static int GetMaxInputDepth(Dictionary<string, VisualiserOutputCell> cellsToDraw)
    {
        int depth = 0;

        List<VisualiserOutputCell> inputs = new();

        foreach (string id in cellsToDraw.Keys)
        {
            VisualiserOutputCell cell = cellsToDraw[id];

            if (cell.IsINPUT)
            {
                inputs.Add(cell);
                if (cell.depthWithinDiagram > depth) depth = cell.depthWithinDiagram;
            }
        }

        // ensure all inputs are same depth
        foreach (VisualiserOutputCell cell in inputs) cell.depthWithinDiagram = depth;

        return depth;
    }

    /// <summary>
    /// Works out the x,y of each brain cell, and assign to location. This is used prior to .Draw().
    /// </summary>
    /// <param name="cellsToDraw"></param>
    /// <returns>Size of canvas required.</returns>
    private static Size LayoutCells(Dictionary<string, VisualiserOutputCell> cellsToDraw)
    {
        const int SpaceBetweenPX = 30; // px

        Dictionary<int, int> depthHeights = new();
        Dictionary<int, int> depthWidth = new();
        Dictionary<int, List<VisualiserOutputCell>> cellsByDepth = new();

        // Collate largest height per blob + annotation. We associate it with the depth
        // of the blob.
        // We similarly work out how wide they are, and track that per depth so we can sum() to know centering and max width.
        foreach (string id in cellsToDraw.Keys)
        {
            VisualiserOutputCell cell = cellsToDraw[id];

            if (!depthHeights.ContainsKey(cell.depthWithinDiagram))
            {
                depthHeights.Add(cell.depthWithinDiagram, cell.depthWithinDiagram == 0 ? SpaceBetweenPX : 0);
                depthWidth.Add(cell.depthWithinDiagram, SpaceBetweenPX); // allow for border at left and right
                cellsByDepth.Add(cell.depthWithinDiagram, new());
            }

            if (cell.Size.Height + SpaceBetweenPX > depthHeights[cell.depthWithinDiagram]) depthHeights[cell.depthWithinDiagram] = cell.Size.Height + SpaceBetweenPX;

            depthWidth[cell.depthWithinDiagram] += cell.Size.Width + SpaceBetweenPX;

            cellsByDepth[cell.depthWithinDiagram].Add(cell);
        }

        // determine the max width for all depths (it varies per depth). i.e. the space horizontally the blobs will take
        int maxWidth = depthWidth[depthWidth.Keys.Min()];

        for (int i = 1; i < depthHeights.Count; i++)
        {
            if (!depthWidth.ContainsKey(i))
            {
                depthWidth.Add(i, 0);
                depthHeights.Add(i, 0);
                cellsByDepth.Add(i, new());
            }

            if (depthWidth[i] > maxWidth) maxWidth += depthWidth[i];
        }

        // reposition the "blobs" centered horizontally, and computer overall size required for canvas.
        int y = depthHeights[depthWidth.Keys.Min()] / 2 + SpaceBetweenPX;

        for (int i = depthWidth.Keys.Min(); i < depthHeights.Count; i++)
        {
            int offset = maxWidth / 2 - depthWidth[i] / 2; // center offset
            int x = offset;

            foreach (VisualiserOutputCell cell in cellsByDepth[i])
            {
                cell.Position = new Point(x + cell.Size.Width / 2, y + cell.Size.Height / 2);

                x += cell.Size.Width + SpaceBetweenPX;
            }

            y += depthHeights[i] + SpaceBetweenPX;
        }

        return new Size(maxWidth + SpaceBetweenPX, y + SpaceBetweenPX);
    }

    /// <summary>
    /// Return the cell colour depending on type and whether it is connected.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    private static Color TypeLabelToColour(BaseCell cell)
    {
        if (cell.Type == CellType.INPUT)
        {
            // none in, none out => silver (visible, but not prominent.
            if (cell.OutboundConnections.Count == 0) return Color.Silver;
        }
        else
        {
            // output or brain-cells
            if (cell.InboundConnections.Count == 0) return Color.Silver;
        }

        return cell.Type switch
        {
            CellType.INPUT => Color.Blue,
            CellType.OUTPUT => Color.Green,
            _ => Color.Orange
        };
    }

    /// <summary>
    /// Use recursion to spread out connections from the output to input.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="cell"></param>
    /// <param name="idsProcessed"></param>
    /// <param name="cellsToDraw"></param>
    /// <param name="linksToDraw"></param>
    void RecursivelyWorkOutNodesAndConnectionsToDrawLater(
        Graphics graphics,
        BaseCell cell,
        ref List<string> idsProcessed,
        ref Dictionary<string, VisualiserOutputCell> cellsToDraw,
        ref Dictionary<string, List<string>> linksToDraw,
        int depth)
    {
        if (idsProcessed.Contains(cell.Id)) return;

        idsProcessed.Add(cell.Id);

        cellsToDraw.Add(cell.Id, new(graphics, cell, TypeLabelToColour(cell),
            cell.ActivationFunction + "\n" +
            (cell.Type == CellType.INPUT || cell.Type == CellType.OUTPUT ? cell.Id : cell.Type) + "\n" + $"{cell.Activation:0.###}", cell.ParameterAnnotation, depth));

        foreach (Connection conn in cell.InboundConnections)
        {
            if (!linksToDraw.ContainsKey(conn.From)) linksToDraw.Add(conn.From, new());

            linksToDraw[conn.From].Add(conn.To);

            RecursivelyWorkOutNodesAndConnectionsToDrawLater(graphics, cell.Network.Neurons[conn.From], ref idsProcessed, ref cellsToDraw, ref linksToDraw, depth + 1);
        }
    }
}