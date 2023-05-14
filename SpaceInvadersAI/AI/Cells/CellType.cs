namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Supported cell types.
/// </summary>
internal enum CellType
{
    //   ███    █████   █       █               █████   █   █   ████    █████    ███
    //  █   █   █       █       █                 █     █   █   █   █   █       █   █
    //  █       █       █       █                 █      █ █    █   █   █       █
    //  █       ████    █       █                 █       █     ████    ████     ███
    //  █       █       █       █                 █       █     █       █           █
    //  █   █   █       █       █                 █       █     █       █       █   █
    //   ███    █████   █████   █████             █       █     █       █████    ███
    //
    
    INPUT,  // the cell is an input to the network
    OUTPUT, // the cell is an output from the network

    PERCEPTRON, // the cell is a perceptron
    TRANSISTOR, // the cell is a transistor - this operates as a switch, having an input that controls whether the other input is passed through or not
    IF, // the cell is an IF cell - this provides branching logic, with a condition input, and two outputs, one for true, and one for false
    AND, // the cell is an AND cell - this provides logic AND, with multiple inputs, and one output, the idea being that if all inputs are true, then the output is true
    MAX, // the cell is a MAX cell - this provides logic MAX, with multiple inputs, and one output, the idea being that the output is the highest input
    MIN // the cell is a MIN cell - this provides logic MIN, with multiple inputs, and one output, the idea being that the output is the lowest input
}
