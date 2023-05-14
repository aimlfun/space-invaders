using SpaceInvadersAI.AI.Cells;

namespace SpaceInvadersAI.AI
{
    internal class TwoBrainCellsInOneObject
    {
        internal BaseCell Cell1;
        internal BaseCell Cell2;

        internal TwoBrainCellsInOneObject(BaseCell cell1, BaseCell cell2)
        {
            Cell1 = cell1;
            Cell2 = cell2;
        }
    }
}