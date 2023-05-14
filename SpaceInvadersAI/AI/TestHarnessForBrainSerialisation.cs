using SpaceInvadersAI.AI.Cells;
using Microsoft.VisualBasic.Devices;
using SpaceInvadersAI.Learning;
using SpaceInvadersAI.Learning.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI;

/// <summary>
/// There were some "quirks" or unintended screw ups in the serialisation & de-serialisation of the brain.
/// It turned out that despite the AI playing well, I had done some rather stupid things in general that was underlying the issue. The pattern I
/// chose was for a brain to have multiple neural networks that share the input / outputs. Somewhere along the line, I failed the implementation.
/// That has now been resolved, after a serious amount of refactoring/rewriting.
/// To ensure it functions as intended I created this test harness to help me find and fix them. It is not used in the game, nor is it a true unit test.
/// Simply call the PerformTest() method to run the tests.
/// </summary>
internal static class TestHarnessForBrainSerialisation
{
    //   ███    █████   ████     ███      █     █        ███     ███      █     █████    ███     ███    █   █           █████   █████    ███    █████    ███    █   █    ████
    //  █   █   █       █   █     █      █ █    █         █     █   █    █ █      █       █     █   █   █   █             █     █       █   █     █       █     █   █   █
    //  █       █       █   █     █     █   █   █         █     █       █   █     █       █     █   █   ██  █             █     █       █         █       █     ██  █   █
    //   ███    ████    ████      █     █   █   █         █      ███    █   █     █       █     █   █   █ █ █             █     ████     ███      █       █     █ █ █   █
    //      █   █       █ █       █     █████   █         █         █   █████     █       █     █   █   █  ██             █     █           █     █       █     █  ██   █  ██
    //  █   █   █       █  █      █     █   █   █         █     █   █   █   █     █       █     █   █   █   █             █     █       █   █     █       █     █   █   █   █
    //   ███    █████   █   █    ███    █   █   █████    ███     ███    █   █     █      ███     ███    █   █             █     █████    ███      █      ███    █   █    ████

    /// <summary>
    /// Runs a lot of tests to ensure that the serialisation and de-serialisation of the brain is working correctly.
    /// </summary>
    internal static void PerformTest()
    {
        Startup();

        int testsPerformed = 0;

        // provide the brain with inputs, outputs and activation functions
        // it doesn't matter how many inputs and outputs, nor functions. You can call them in/output what you like

        string[]? inputs = new string[] {
            $"player-position-x",
            $"player-bullet-y",
            $"alien-rolling-bullet-x",
            $"alien-rolling-bullet-y",

            $"alien-plunger-bullet-x",
            $"alien-plunger-bullet-y",

            $"alien-squiggly-bullet-x",
            $"alien-squiggly-bullet-y"
        };

        string[]? outputParameters = new string[] { "desired-position", "fire" }; // what we require "out" for controlling player

        ActivationFunction[] activationFunctions = new ActivationFunction[] { ActivationFunction.Sigmoid, ActivationFunction.TanH, ActivationFunction.ReLU, ActivationFunction.LeakyReLU,
                                                                              ActivationFunction.SeLU, ActivationFunction.PReLU, ActivationFunction.Logistic, ActivationFunction.Identity,
                                                                              ActivationFunction.Step, ActivationFunction.SoftSign, ActivationFunction.SoftPlus, ActivationFunction.Gaussian,
                                                                              ActivationFunction.BENTIdentity, ActivationFunction.Bipolar, ActivationFunction.BipolarSigmoid, ActivationFunction.HardTanH,
                                                                              ActivationFunction.Absolute, ActivationFunction.Not };
        
        Brain referenceBrainObject = SetupReferenceObject(inputs, outputParameters, activationFunctions);

        Debug.WriteLine("test started");

        Debug.WriteLine("Running a lot of tests! This will take a while.");

        // TEST: MUTATE THE BRAIN, SERIALISE IT, DESERIALISE IT, AND COMPARE THE TWO. REPEAT.

        // apply random inputs
        double[] inputsToBrain = new double[referenceBrainObject.BrainInputs.Count];

        // try a lot of random mutations to break it. Each mutation adds connections, cells and changes bias / weights / thresholds / activation functions.
        for (int testIteration = 0; testIteration < 15000; testIteration++)
        {
            if ((++testsPerformed) % 1000 == 0) Debug.WriteLine($"{testsPerformed} tests performed");


            // first time around loop is without mutation

            // STEP 1 - SERIALISE BRAIN OBJECT, THEN DESERIALISE IT, AND COMPARE THE TWO.
            CompareSerialisedDeserialisedObjectAgainstReferenceObject(out Brain brainCreatedFromTemplate, out string referenceBrainObjectAsTemplate, out string templateForBrainCreatedFromTemplate, referenceBrainObject, testIteration);

            // STEP 2 - PUT RANDOM DATA INTO NEURAL NETWORK AND CHECK OUTPUTS ARE THE SAME.
            // note: that doesn't mean the network works properly, input > hidden > output, but it does mean that the serialisation / de-serialisation worked.

            TestFunctioningOfReferenceBrainAndDeserialisedBrain(brainCreatedFromTemplate, referenceBrainObject, inputsToBrain, out Dictionary<string, double> outputs1, out Dictionary<string, double> outputs2);

            // ok, so templates match, but do the outputs match?

            // compare the outputs
            if (outputs1["desired-position"] != outputs2["desired-position"] ||
                outputs1["fire"] != outputs2["fire"])
            {
                Debug.WriteLine($"#1 outputs1[\"desired-position\"] = {outputs1["desired-position"]} outputs1[\"fire\"] = {outputs1["fire"]}");
                Debug.WriteLine($"#2 outputs2[\"desired-position\"] = {outputs2["desired-position"]} outputs2[\"fire\"] = {outputs2["fire"]}");
                DumpOutBrainBeforeAfter("test failed because outputs differed", brainCreatedFromTemplate, referenceBrainObjectAsTemplate, templateForBrainCreatedFromTemplate, referenceBrainObject);
                continue;
            }

            // STEP 3 - MUTATE THE BRAIN AND REPEAT THE ABOVE TESTS

            // pick a random mutation method
            var mutationMethod = PersistentConfig.Settings.AllowedMutationMethods[RandomNumberGenerator.GetInt32(0, PersistentConfig.Settings.AllowedMutationMethods.Length)];

            referenceBrainObject.Reset(); // important if we are to compare outputs
            referenceBrainObject.Mutate(mutationMethod);
        }

        // some time later...
        Debug.WriteLine($"{testsPerformed} tests performed");
    }

    /// <summary>
    /// We test the de-serialised brain provides the same output to the reference brain.
    /// </summary>
    /// <param name="brainCreatedFromTemplate"></param>
    /// <param name="referenceBrainObject"></param>
    /// <param name="inputsToBrain"></param>
    /// <param name="outputs1"></param>
    /// <param name="outputs2"></param>
    private static void TestFunctioningOfReferenceBrainAndDeserialisedBrain(Brain brainCreatedFromTemplate, Brain referenceBrainObject, double[] inputsToBrain, out Dictionary<string, double> outputs1, out Dictionary<string, double> outputs2)
    {
        // provide some random inputs other than zeros

        for (int inputIndex = 0; inputIndex < referenceBrainObject.BrainInputs.Count; inputIndex++)
        {
            inputsToBrain[inputIndex] = RandomNumberGenerator.GetInt32(-100000, 100000) / 100000f;
        }

        // assign those values to BOTH brains
        referenceBrainObject.SetInputValues(inputsToBrain);
        brainCreatedFromTemplate.SetInputValues(inputsToBrain);

        referenceBrainObject.Reset(); // important if we are to compare outputs, as existing state affects self connection
        brainCreatedFromTemplate.Reset();

        Debug.WriteLine("feed-forward out reference brain");
        outputs1 = referenceBrainObject.FeedForward();
        
        Debug.WriteLine("feed-forward out templated brain");
        outputs2 = brainCreatedFromTemplate.FeedForward();

        // compare is done outside this function
    }

    /// <summary>
    /// Serialises the reference brain object, de-serialises it, and compares the two.
    /// Logic states if we take object A, serialise it to text, and deserialise it, we should get object A back.
    /// It's tricky to compare two objects, so we serialise both to text and compare the text.
    /// </summary>
    /// <param name="brainCreatedFromTemplate"></param>
    /// <param name="referenceBrainObjectAsTemplate"></param>
    /// <param name="templateForBrainCreatedFromTemplate"></param>
    /// <param name="referenceBrainObject"></param>
    /// <param name="testIteration"></param>
    private static void CompareSerialisedDeserialisedObjectAgainstReferenceObject(out Brain brainCreatedFromTemplate, out string referenceBrainObjectAsTemplate, out string templateForBrainCreatedFromTemplate, Brain referenceBrainObject, int testIteration)
    {
        referenceBrainObjectAsTemplate = referenceBrainObject.GetAsTemplate(); // serialise it to text

        // attempt to recreate the brain from the serialised text (i.e. deserialise)
        brainCreatedFromTemplate = Brain.CreateFromTemplate(referenceBrainObjectAsTemplate);

        // to know if the recreation was successful, we serialise the recreated brain to text and compare it to the original
        templateForBrainCreatedFromTemplate = brainCreatedFromTemplate.GetAsTemplate();

        if (!TemplatesMatch(referenceBrainObjectAsTemplate, templateForBrainCreatedFromTemplate))
        {
            DumpOutBrainBeforeAfter($"mutate test {testIteration} failed - serialise > deserialise resulted in a different object",
                                    brainCreatedFromTemplate,
                                    referenceBrainObjectAsTemplate,
                                    templateForBrainCreatedFromTemplate,
                                    referenceBrainObject);
        }
    }

    /// <summary>
    /// Create our reference brain and perform some initial tests before we spend ages exercising it fully.
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="outputParameters"></param>
    /// <param name="activationFunctions"></param>
    /// <returns></returns>
    private static Brain SetupReferenceObject(string[] inputs, string[] outputParameters, ActivationFunction[] activationFunctions)
    {
        // this is our object that we're going to repeatedly mutate, serialise and de-serialise
        Brain referenceBrainObject = new(Brain.NextUniqueBrainID.ToString(), activationFunctions, inputs, outputParameters);

        // output the object in readable form to confirm it looks correct
        File.WriteAllText(@"c:\temp\referenceBrainObject-after-creation.txt", referenceBrainObject.GetBrainAsText());

        // add a network to it with input neurons and connections
        referenceBrainObject.AddNetworkWithConnectedInputOutputs("network", activationFunctions);

        // output the object in readable form to confirm it looks correct
        File.WriteAllText(@"c:\temp\referenceBrainObject-after-network-create.txt", referenceBrainObject.GetBrainAsText());

        // tell it what cell types are allowed for mutations
        referenceBrainObject.AllowedCellTypes = PersistentConfig.Settings.CellTypeRatios;
        referenceBrainObject.Name = "TEST1";
        return referenceBrainObject;
    }

    /// <summary>
    /// Dumps the object in readable and serialised form to a file, and breaks into the debugger.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="templatedBrainObject"></param>
    /// <param name="referenceBrainSerialised"></param>
    /// <param name="templatedBrainSerialised"></param>
    /// <param name="referenceBrainObject"></param>
    private static void DumpOutBrainBeforeAfter(string message, Brain templatedBrainObject, string referenceBrainSerialised, string templatedBrainSerialised, Brain referenceBrainObject)
    {
        Debug.WriteLine(message);

        File.WriteAllText(@"c:\temp\original-object-serialised.txt", referenceBrainSerialised);
        File.WriteAllText(@"c:\temp\templated-object-serialised.txt", templatedBrainSerialised);

        File.WriteAllText(@"c:\temp\original-object.txt", referenceBrainObject.GetBrainAsText());
        File.WriteAllText(@"c:\temp\templated-object.txt", templatedBrainObject.GetBrainAsText());

        Debugger.Break();
    }

    /// <summary>
    /// Ensure we have min / max neurons sufficient to test serialisation
    /// </summary>
    private static void Startup()
    {
        // we don't initialise PersistentConfig.Settings.CellTypeRatios, because the static constructor does so
        PersistentConfig.Settings.MaximumNumberOfNeurons = 100; // the more, the harder we test the serialisation
        PersistentConfig.Settings.MinimumNumberOfNeurons = 3;
    }

    /// <summary>
    /// Compares two templates to see if they match. 
    /// </summary>
    /// <param name="template1"></param>
    /// <param name="template2"></param>
    /// <returns></returns>
    private static bool TemplatesMatch(string template1, string template2)
    {
        template1 = DropNoComparablePartsOfSerialisedText(template1);
        template2 = DropNoComparablePartsOfSerialisedText(template2);

        // do the templates match?
        return template1 == template2;
    }

    /// <summary>
    /// Drops the first line of text. 
    /// The first line is dropped because it contains the score, which we do not serialise. We also drop comments in the first 5 lines, and the "NAME" declaration.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string DropNoComparablePartsOfSerialisedText(string text)
    {
        string[] textLines = text.Split(Environment.NewLine);

        List<string> list1 = textLines.ToList();

        for (int i = 0; i < 5; i++)
        {
            if (list1[0].StartsWith('#') || list1[0].StartsWith("NAME ")) // the 2nd line can contain a comment, which we do not serialise
            {
                list1.RemoveAt(0);
            }
        }
        // same text, minus the first line or two.
        return string.Join(Environment.NewLine, list1);
    }
}