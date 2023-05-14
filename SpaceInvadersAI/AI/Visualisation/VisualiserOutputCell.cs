using SpaceInvadersAI.AI.Cells;
using System.Drawing;

namespace SpaceInvadersAI.AI.Visualisation;

/// <summary>
/// Represents a cell, with a label. It has methods to measure and draw itself.
///       ___
///      /   \   xxx xxx 
///     | xox |  xxx xxxx
///      \___/   xx  xx x
///        .
/// </summary>
internal class VisualiserOutputCell
{
    //  █   █    ███     ███    █   █     █     █        ███     ███    █████   ████             ███    █████   █       █
    //  █   █     █     █   █   █   █    █ █    █         █     █   █   █       █   █           █   █   █       █       █
    //  █   █     █     █       █   █   █   █   █         █     █       █       █   █           █       █       █       █
    //  █   █     █      ███    █   █   █   █   █         █      ███    ████    ████            █       ████    █       █
    //  █   █     █         █   █   █   █████   █         █         █   █       █ █             █       █       █       █
    //   █ █      █     █   █   █   █   █   █   █         █     █   █   █       █  █            █   █   █       █       █
    //    █      ███     ███     ███    █   █   █████    ███     ███    █████   █   █            ███    █████   █████   █████

    /// <summary>
    /// Used for "space" around the cells.
    /// </summary>
    private const int BorderSpacingPX = 20;

    /// <summary>
    /// Colour of the cell (it differs depending on type).
    /// </summary>
    private readonly Color colourToPaintCell;

    /// <summary>
    /// Text to display on circle of cell.
    /// </summary>
    private readonly string label;

    /// <summary>
    /// Text to display to the right of the cell.
    /// </summary>
    private readonly string cellLabelTextToRightOfCell;

    /// <summary>
    /// Size of the "parameters" text for this cell.
    /// </summary>
    private SizeF sizeParameters;

    /// <summary>
    /// Size of the "label" for this cell.
    /// </summary>
    private SizeF sizeLabel;

    /// <summary>
    /// Where to draw the cell.
    /// </summary>
    internal Point Position;

    /// <summary>
    /// The cell being visualised.
    /// </summary>
    internal BaseCell Cell;

    /// <summary>
    /// Size of the cell circle.
    /// </summary>
    internal const int DiameterOfCirclePX = 70;

    /// <summary>
    /// Entire size the cell requires to draw itself.
    /// </summary>
    internal Size Size;

    /// <summary>
    /// Used to determines vertical position of the cell on the diagram.
    /// </summary>
    internal int depthWithinDiagram = 0;

    /// <summary>
    /// 
    /// </summary>
    internal bool IsINPUT
    {
        get
        {
            return Cell.Type == CellType.INPUT;
        }
    }

    internal bool IsOUTPUT
    {
        get
        {
            return Cell.Type == CellType.OUTPUT;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="basicCell"></param>
    /// <param name="colour"></param>
    /// <param name="label"></param>
    /// <param name="parameters"></param>
    /// <param name="depth"></param>
    internal VisualiserOutputCell(Graphics graphics, BaseCell basicCell, Color colour, string label, string parameters, int depth)
    {
        colourToPaintCell = colour;
        Position = new();
        this.label = label;
        cellLabelTextToRightOfCell = parameters;
        Size = Measure(graphics);
        depthWithinDiagram = depth;
        Cell = basicCell;
    }

    /// <summary>
    /// Calculates the size of the cell, based on the label and parameters.
    /// </summary>
    /// <param name="g"></param>
    /// <returns></returns>
    internal Size Measure(Graphics graphics)
    {
        sizeParameters = graphics.MeasureString(cellLabelTextToRightOfCell, Visualiser.s_fontForVisualisation);
        sizeLabel = graphics.MeasureString(label, Visualiser.s_fontForVisualisation);

        //       ___
        //      /   \   xxx xxx 
        // Y - | xox |  xxx xxxx
        //      \___/   xx  xx x
        //        .
        //        ^ Location.X
        //   |---------------------|
        //             width
        int width = BorderSpacingPX + DiameterOfCirclePX + BorderSpacingPX + (int)sizeParameters.Width + BorderSpacingPX;
        int height = BorderSpacingPX + Math.Max(DiameterOfCirclePX, (int)sizeParameters.Height) + BorderSpacingPX;

        return new Size(width, height);
    }

    /// <summary>
    /// Draw a blob with outline, a label in the middle of it, and parameters at the side.
    /// </summary>
    /// <param name="g"></param>
    internal void Draw(Graphics g)
    {
        //              |-------| parameters
        //       ___
        //      /   \   xxx xxx 
        // Y - | xox |  xxx xxxx
        //      \___/   xx  xx x
        //        .
        //        ^ Location.X

        // blob inner
        using SolidBrush brushFilledEllipse = new(Color.FromArgb(150, colourToPaintCell.R, colourToPaintCell.G, colourToPaintCell.B));
        g.FillEllipse(brushFilledEllipse, new Rectangle(Position.X - DiameterOfCirclePX / 2, Position.Y - DiameterOfCirclePX / 2, DiameterOfCirclePX, DiameterOfCirclePX));

        // blob outer
        using Pen penPerimeterOfEllipse = new(colourToPaintCell);
        g.DrawEllipse(penPerimeterOfEllipse, new Rectangle(Position.X - DiameterOfCirclePX / 2, Position.Y - DiameterOfCirclePX / 2, DiameterOfCirclePX, DiameterOfCirclePX));

        // label centered on blob "xox"
        using StringFormat stringFormat = new();
        stringFormat.Alignment = StringAlignment.Center;

        g.DrawString(label, Visualiser.s_fontForVisualisation, Brushes.Black, Position.X, Position.Y - sizeLabel.Height / 2, stringFormat);

        // parameters right of blob "xxx xxx"
        g.DrawString(cellLabelTextToRightOfCell, Visualiser.s_fontForVisualisation, Brushes.Black, (float)Position.X + DiameterOfCirclePX / 2 + BorderSpacingPX, Position.Y - sizeParameters.Height / 2);
    }

}