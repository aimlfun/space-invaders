using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI;

/// <summary>
/// These are the methods that can be used to mutate a genome.
/// </summary>
internal enum MutationMethod
{
    //  █   █   █   █   █████     █     █████    ███     ███    █   █           █   █   █████   █████   █   █    ███    ████     ███
    //  ██ ██   █   █     █      █ █      █       █     █   █   █   █           ██ ██   █         █     █   █   █   █   █   █   █   █
    //  █ █ █   █   █     █     █   █     █       █     █   █   ██  █           █ █ █   █         █     █   █   █   █   █   █   █
    //  █ █ █   █   █     █     █   █     █       █     █   █   █ █ █           █ █ █   ████      █     █████   █   █   █   █    ███
    //  █   █   █   █     █     █████     █       █     █   █   █  ██           █   █   █         █     █   █   █   █   █   █       █
    //  █   █   █   █     █     █   █     █       █     █   █   █   █           █   █   █         █     █   █   █   █   █   █   █   █
    //  █   █    ███      █     █   █     █      ███     ███    █   █           █   █   █████     █     █   █    ███    ████     ███

    AddCell, RemoveCell,
    ModifyBias,
    ModifyWeight,
    ModifyCellType,
    ModifyActivationFunction,
    ModifyThreshold,
    AddSelfConnection, RemoveSelfConnection,
    AddConnection, RemoveConnection,
    SwapBiasAndActivationBetweenNodes
};
