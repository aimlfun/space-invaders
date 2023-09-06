using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.Learning.Fitness;
using System.Runtime.InteropServices.Marshalling;

namespace SpaceInvadersAI.Learning.Forms;

/// <summary>
/// Form for configuring the AI.
/// </summary>
public partial class FormAIConfig : Form
{
    //   ███     ███    █   █   █████    ███     ████   █   █   ████      █     █████    ███     ███    █   █           █████    ███    ████    █   █
    //  █   █   █   █   █   █   █         █     █       █   █   █   █    █ █      █       █     █   █   █   █           █       █   █   █   █   ██ ██
    //  █       █   █   ██  █   █         █     █       █   █   █   █   █   █     █       █     █   █   ██  █           █       █   █   █   █   █ █ █
    //  █       █   █   █ █ █   ████      █     █       █   █   ████    █   █     █       █     █   █   █ █ █           ████    █   █   ████    █ █ █
    //  █       █   █   █  ██   █         █     █  ██   █   █   █ █     █████     █       █     █   █   █  ██           █       █   █   █ █     █   █
    //  █   █   █   █   █   █   █         █     █   █   █   █   █  █    █   █     █       █     █   █   █   █           █       █   █   █  █    █   █
    //   ███     ███    █   █   █        ███     ████    ███    █   █   █   █     █      ███     ███    █   █           █        ███    █   █   █   █

    /// <summary>
    /// This is the file in which the invader config is stored (location is EXE runtime directory).
    /// </summary>
    const string c_invadersConfig = @"Config\ai-config.json";

    /// <summary>
    /// This is the file in which the scoring config is stored (location is EXE runtime directory)./// </summary>
    const string c_invadersScore = @"Config\fitness-scoring.json";

    internal bool playGame = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    public FormAIConfig()
    {
        InitializeComponent();

        // seed them based on available options
        PopulateAllowedMutationsFromENUM();
        PopulateActivationFunctionsFromENUM();

        if (File.Exists(c_invadersConfig)) PersistentConfig.Load(c_invadersConfig);
        FitnessScoreMultipliers.Load(c_invadersScore);

        RetrieveConfigAndPopulateUI();
        ComboBoxSelectionType_SelectedIndexChanged(null, null);

        AddTemplatesToDropDown();
    }

    /// <summary>
    /// Templates are stored in the "Templates" subdirectory. This method adds them to the drop down.
    /// </summary>
    private void AddTemplatesToDropDown()
    {
        if (!Directory.Exists(@".\Templates")) Directory.CreateDirectory(@".\Templates");

        List<string> items = new();

        // The training goes with the AI input/output method, but more to the point, get it mixed up makes the code fall over.
        // That's because the number of inputs and outputs is different.
        string prefix = GetTemplatePrefixBasedOnAIInputMethod() + " " + GetTemplatePrefixBasedOnAIOutputMethod() + " ";
        IEnumerable<string> templates = Directory.EnumerateFiles(@".\Templates", prefix + "*.ai");
        items.Add("-none selected-");

        foreach (string filename in templates)
        {
            items.Add(filename);
        }

        // clear the combo boxes
        comboBoxListOfTemplates.Items.Clear();

        // template drop down
        comboBoxListOfTemplates.Items.AddRange(items.ToArray());

        // retrieve the rules + template
        ComboBox[] levelComboBoxes = new[] { comboBoxLevelBrain1, comboBoxLevelBrain2, comboBoxLevelBrain3, comboBoxLevelBrain4, comboBoxLevelBrain5,
                                             comboBoxLevelBrain6, comboBoxLevelBrain7, comboBoxLevelBrain8, comboBoxLevelBrain9, comboBoxLevelBrain10 };

        TextBox[] levelRuleInputs = new[] { textBoxLevelRule1, textBoxLevelRule2, textBoxLevelRule3, textBoxLevelRule4, textBoxLevelRule5,
                                            textBoxLevelRule6, textBoxLevelRule7, textBoxLevelRule8, textBoxLevelRule9, textBoxLevelRule10 };

        TextBox[] startScoreInputs = new[] { textBoxStartScore1, textBoxStartScore2, textBoxStartScore3, textBoxStartScore4, textBoxStartScore5,
                                            textBoxStartScore6, textBoxStartScore7, textBoxStartScore8, textBoxStartScore9, textBoxStartScore10 };

        for (int i = 0; i < levelComboBoxes.Length; i++)
        {
            levelComboBoxes[i].Items.Clear();
            levelComboBoxes[i].Items.AddRange(items.ToArray());

            if (i < PersistentConfig.Settings.BrainTemplates.Count)
            {
                // we have a setting in the config for this, so update the UI accordingly
                startScoreInputs[i].Text = PersistentConfig.Settings.BrainTemplates[i].StartingScore.ToString();
                levelRuleInputs[i].Text = PersistentConfig.Settings.BrainTemplates[i].LevelRule;
                SetSelectedIndexOfComboBoxByValue(PersistentConfig.Settings.BrainTemplates[i].BrainTemplateFileName, levelComboBoxes[i]);
            }
            else
            {
                // we not setting for this, so clear the UI
                startScoreInputs[i].Text = "0";
                levelRuleInputs[i].Text = "";
                SetSelectedIndexOfComboBoxByValue("", levelComboBoxes[i]);
            }
        }

        SetSelectedIndexOfComboBoxByValue(PersistentConfig.Settings.Template, comboBoxListOfTemplates);
    }

    /// <summary>
    /// Used in the prefix when looking for templates.
    /// </summary>
    /// <returns></returns>
    private string GetTemplatePrefixBasedOnAIInputMethod()
    {
        if (radioButtonAIAccessInternalData.Checked)
        {
            return "internalData";
        }
        else if (radioButtonAISeesScreen.Checked)
        {
            return "videoScreen";
        }
        else
        {
            return "radar";
        }
    }

    /// <summary>
    /// Used in the prefix when looking for templates.
    /// </summary>
    /// <returns></returns>
    private string GetTemplatePrefixBasedOnAIOutputMethod()
    {
        return radioButtonAIChoosesAction.Checked ? "action" : "position";
    }

    /// <summary>
    /// Finds an item in a combo box, and sets the .SelectedIndex to that item.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="comboBox"></param>
    private static void SetSelectedIndexOfComboBoxByValue(string? value, ComboBox comboBox)
    {
        comboBox.SelectedIndex = 0;

        // populate .SelectedIndex where item matches Config.Template
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i].ToString() == value)
            {
                comboBox.SelectedIndex = i;
                break;
            }
        }

        comboBox.Enabled = (comboBox.Items.Count > 0);
    }

    /// <summary>
    /// Adds a list of available activation functions as a checklist.
    /// </summary>
    private void PopulateActivationFunctionsFromENUM()
    {
        // obtain a list of available activation functions. We use the "enum" as the basis,
        // because we don't have to update the list as we add or remove activation functions.

        List<ActivationFunction> functions = Enum.GetValues(typeof(ActivationFunction)).Cast<ActivationFunction>().ToList();

        // remove "None", this makes no sense except for the "Input" / "Output"
        functions.RemoveAt(0);

        // clear the current list, and add all the "enums".
        checkedListBoxActivationFunctions.Items.Clear();

        foreach (var x in functions) checkedListBoxActivationFunctions.Items.Add(x);

        // some make less sense, so we tick the "standard" ones.
        List<string> defaults = new string[] { "Sigmoid", "TanH", "ReLU", "LeakyReLU", "SeLU", "Logistic", /* "Identity", */ "Step",
                                                "SoftSign", "Gaussian", "BENTIdentity", "Bipolar", "BipolarSigmoid", "HardTanH", "Absolute", "Not" }.ToList();

        for (int i = 0; i < checkedListBoxActivationFunctions.Items.Count; i++)
        {
            string? item = checkedListBoxActivationFunctions.Items[i].ToString();

            if (item is not null && defaults.Contains(item)) checkedListBoxActivationFunctions.SetItemChecked(i, true);
        }
    }

    /// <summary>
    /// Adds a list of available activation functions as a checklist.
    /// </summary>
    private void PopulateAllowedMutationsFromENUM()
    {
        // obtain a list of available mutation methods. We use the "enum" as the basis,
        // because we don't have to update the list as we add or remove mutation methods.

        List<MutationMethod> methods = Enum.GetValues(typeof(MutationMethod)).Cast<MutationMethod>().ToList();

        // clear the current list, and add all the "enums"
        checkedListBoxAllowedMutations.Items.Clear();

        foreach (var x in methods) checkedListBoxAllowedMutations.Items.Add(x);

        // some make less sense, so we tick the "standard" ones.
        for (int i = 0; i < checkedListBoxAllowedMutations.Items.Count; i++)
        {
            checkedListBoxAllowedMutations.SetItemChecked(i, true);
        }
    }

    /// <summary>
    /// User clicked [Start]. Use the value provided, and start the AI.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonStart_Click(object sender, EventArgs e)
    {
        PersistentConfig.Settings.Mode = PersistentConfig.FrameworkMode.learning;

        StoreConfig();

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Applies the Config object to populate the UI (so the user gets same config as last time).
    /// </summary>
    private void RetrieveConfigAndPopulateUI()
    {
        checkBoxAIPlaysWithShields.Checked = PersistentConfig.Settings.AIPlaysWithShields;
        checkBoxOneLevelOnly.Checked = PersistentConfig.Settings.AIOneLevelOnly;
        numericUpDownStartingLevel.Value = (int)PersistentConfig.Settings.AIStartLevel;
        numericUpDownDeathPct.Value = (decimal)(PersistentConfig.Settings.PercentageOfDeadScoreThreshold * 100f); // % to 0..100
        numericUpDownStartingScore.Value = (int)PersistentConfig.Settings.AIStartScore;
        radioButtonRandomNeurons.Checked = PersistentConfig.Settings.CreatingARandomNetwork;
        checkBoxEndGameIfLifeLost.Checked = PersistentConfig.Settings.EndGameIfLifeLost;

        // one enum => 3 radio buttons
        switch (PersistentConfig.Settings.InputToAI)
        {
            case PersistentConfig.AIInputMode.videoScreen:
                radioButtonAISeesScreen.Checked = true;
                break;

            case PersistentConfig.AIInputMode.radar:
                radioButtonAISeesRadar.Checked = true;
                break;

            case PersistentConfig.AIInputMode.internalData:
                radioButtonAIAccessInternalData.Checked = true;
                break;
        }

        radioButtonPerceptron.Checked = !radioButtonRandomNeurons.Checked;
        radioButtonAIChoosesAction.Checked = PersistentConfig.Settings.UseActionFireApproach;
        radioButtonAIChoosesPosition.Checked = !PersistentConfig.Settings.UseActionFireApproach;

        comboBoxSelectionType.SelectedIndex = PersistentConfig.Settings.SelectionType switch
        {
            LearningFramework.SelectionType.POWER => 0,
            LearningFramework.SelectionType.FITNESS_PROPORTIONATE => 1,
            LearningFramework.SelectionType.TOURNAMENT => 2,
            _ => throw new ApplicationException("unknown selection type in combo-box"),
        };

        radioButtonRandomNeurons.Checked = PersistentConfig.Settings.CreatingARandomNetwork;
        numericUpDownNeurons.Value = PersistentConfig.Settings.DesiredRandomNeurons;

        numericUpDownRandomAdditions.Value = PersistentConfig.Settings.PercentOfBrainsToCreateAsNewRandomDuringMutation;
        numericUpDownPreserved.Value = PersistentConfig.Settings.PercentOfBrainsPreservedDuringMutation;
        numericUpDownTemplated.Value = PersistentConfig.Settings.PercentOfBrainsToCreateFromTemplate;

        numericUpDownMutationTimes.Value = PersistentConfig.Settings.NumberOfTimesASingleBrainIsMutatedInOneGeneration;
        numericUpDownPctChance.Value = (decimal)PersistentConfig.Settings.PercentChanceABrainIsPickedForMutation;
        numericUpDownMaxNeurons.Value = PersistentConfig.Settings.MaximumNumberOfNeurons;
        numericUpDownMinNeurons.Value = PersistentConfig.Settings.MinimumNumberOfNeurons;
        numericUpDownFirstMutationMoves.Value = PersistentConfig.Settings.MovesBeforeMutation;
        numericUpDownConcurrentGames.Value = PersistentConfig.Settings.ConcurrentGames;

        PopulateUIForActivationFunctionsFromConfig();

        RetrieveAndPopulateUIForAllowedMutations();

        RetrieveAndPopulateUIConfigCellTypes();

        textBoxPerceptronLayers.Text = string.Join(",", PersistentConfig.Settings.AIHiddenLayers);

        numericUpDownAccuracy.Value = (decimal)FitnessScoreMultipliers.Settings.AccuracyMultiplier;
        numericUpDownAdditionalLevel.Value = (decimal)FitnessScoreMultipliers.Settings.LevelMultiplier;
        numericUpDownLives.Value = (decimal)FitnessScoreMultipliers.Settings.LivesMultiplier;
        numericUpDownKillsAvoided.Value = (decimal)FitnessScoreMultipliers.Settings.KillsAvoidedMultiplier;
        numericUpDownHitSaucer.Value = (decimal)FitnessScoreMultipliers.Settings.SaucerMultiplier;
        numericUpDownShieldsShot.Value = (decimal)FitnessScoreMultipliers.Settings.ShieldsShotMultiplier;
        numericUpDownScore.Value = (decimal)FitnessScoreMultipliers.Settings.ScoreMultiplier;
        numericUpDownHitInvader.Value = (decimal)FitnessScoreMultipliers.Settings.InvaderMultiplier;
        numericUpDownGroundPunishment.Value = (decimal)FitnessScoreMultipliers.Settings.PunishmentForInvadersReachingBottom;
    }

    /// <summary>
    /// Retrieves the config ActivationFunctions, and updates the checkbox controls.
    /// </summary>
    private void PopulateUIForActivationFunctionsFromConfig()
    {
        // set the activation functions based on saved config
        for (int i = 0; i < checkedListBoxActivationFunctions.Items.Count; i++)
        {
            string? item = checkedListBoxActivationFunctions.Items[i].ToString();

            bool checkedItem = (item is not null && PersistentConfig.Settings.AllowedActivationFunctions.Contains((ActivationFunction)Enum.Parse(typeof(ActivationFunction), item)));

            checkedListBoxActivationFunctions.SetItemChecked(i, checkedItem);
        }
    }

    /// <summary>
    /// Retrieves the config AllowedMutations, and updates the checkbox controls.
    /// </summary>
    private void RetrieveAndPopulateUIForAllowedMutations()
    {
        // set the checkedListBoxAllowedMutations based on Config.AllowedMutationMethods
        for (int i = 0; i < checkedListBoxAllowedMutations.Items.Count; i++)
        {
            string? item = checkedListBoxAllowedMutations.Items[i].ToString();
            if (item is not null && PersistentConfig.Settings.AllowedMutationMethods.Contains((MutationMethod)Enum.Parse(typeof(MutationMethod), item)))
            {
                checkedListBoxAllowedMutations.SetItemChecked(i, true);
            }
            else
            {
                checkedListBoxAllowedMutations.SetItemChecked(i, false);
            }
        }
    }

    /// <summary>
    /// Retrieves the config CellTypes, and updates the checkbox + numeric up/down controls.
    /// </summary>
    private void RetrieveAndPopulateUIConfigCellTypes()
    {
        Dictionary<CellType, NumericUpDown> mapTypeToNumericUpDown = new()
        {
            { CellType.PERCEPTRON, numericUpDownPERCEPTRON },
            { CellType.AND, numericUpDownAND },
            { CellType.TRANSISTOR, numericUpDownTRANSISTOR },
            { CellType.IF, numericUpDownIF },
            { CellType.MAX, numericUpDownMAX },
            { CellType.MIN, numericUpDownMIN }
        };

        Dictionary<CellType, CheckBox> mapTypeToCheckbox = new()
        {
            { CellType.PERCEPTRON, checkBoxPERCEPTRON },
            { CellType.AND, checkBoxAND },
            { CellType.TRANSISTOR, checkBoxTRANSISTOR },
            { CellType.IF, checkBoxIF },
            { CellType.MAX, checkBoxMAX },
            { CellType.MIN, checkBoxMIN }
        };

        // parse Config.CellTypeRatios and populate numericUpDownPERCEPTRON.Value checkboxes
        foreach (var x in PersistentConfig.Settings.CellTypeRatios.Keys)
        {
            mapTypeToNumericUpDown[x].Value = PersistentConfig.Settings.CellTypeRatios[x];
            mapTypeToCheckbox[x].Checked = PersistentConfig.Settings.CellTypeRatios[x] > 0;
        }
    }

    /// <summary>
    /// Stores the AI settings to the config object, and also to disk.
    /// </summary>
    private void StoreConfig()
    {
        buttonLearn.Enabled = false;

        if (comboBoxListOfTemplates.SelectedIndex < 1)
        {
            // none selected, templated is disabled.
            numericUpDownTemplated.Value = 0;
        }

        PersistentConfig.Settings.SelectionType = GetSelectionTypeFromComboBox();
        PersistentConfig.Settings.CreatingARandomNetwork = radioButtonRandomNeurons.Checked;
        PersistentConfig.Settings.DesiredRandomNeurons = (int)numericUpDownNeurons.Value;
        PersistentConfig.Settings.PercentOfBrainsToCreateAsNewRandomDuringMutation = (int)numericUpDownRandomAdditions.Value;
        PersistentConfig.Settings.PercentOfBrainsPreservedDuringMutation = (int)numericUpDownPreserved.Value;
        PersistentConfig.Settings.NumberOfTimesASingleBrainIsMutatedInOneGeneration = (int)numericUpDownMutationTimes.Value;
        PersistentConfig.Settings.PercentChanceABrainIsPickedForMutation = (int)numericUpDownPctChance.Value;
        PersistentConfig.Settings.PercentOfBrainsToCreateFromTemplate = (int)numericUpDownTemplated.Value;

        if (radioButtonAIAccessInternalData.Checked)
        {
            PersistentConfig.Settings.InputToAI = PersistentConfig.AIInputMode.internalData;
        }
        else if (radioButtonAISeesScreen.Checked)
        {
            PersistentConfig.Settings.InputToAI = PersistentConfig.AIInputMode.videoScreen;
        }
        else
        {
            PersistentConfig.Settings.InputToAI = PersistentConfig.AIInputMode.radar;
        }

        PersistentConfig.Settings.AIStartScore = (int)numericUpDownStartingScore.Value;

        PersistentConfig.Settings.AllowedActivationFunctions = GetSelectedActivationFunctions();
        PersistentConfig.Settings.AllowedMutationMethods = GetSelectedMutationMethods();

        PersistentConfig.Settings.MaximumNumberOfNeurons = (int)numericUpDownMaxNeurons.Value;
        PersistentConfig.Settings.MinimumNumberOfNeurons = (int)numericUpDownMinNeurons.Value;
        PersistentConfig.Settings.MovesBeforeMutation = (int)numericUpDownFirstMutationMoves.Value;
        PersistentConfig.Settings.ConcurrentGames = (int)numericUpDownConcurrentGames.Value;

        PersistentConfig.Settings.AIPlaysWithShields = checkBoxAIPlaysWithShields.Checked;
        PersistentConfig.Settings.AIOneLevelOnly = checkBoxOneLevelOnly.Checked;
        PersistentConfig.Settings.AIStartLevel = (int)numericUpDownStartingLevel.Value;
        PersistentConfig.Settings.PercentageOfDeadScoreThreshold = (float)numericUpDownDeathPct.Value / 100;
        PersistentConfig.Settings.UseActionFireApproach = radioButtonAIChoosesAction.Checked;
        PersistentConfig.Settings.EndGameIfLifeLost = checkBoxEndGameIfLifeLost.Checked;

        // an initial score only applies on levels 2 upwards
        if (PersistentConfig.Settings.AIStartLevel == 1) PersistentConfig.Settings.AIStartScore = 0;

        PersistentConfig.Settings.CellTypeRatios.Clear();
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.PERCEPTRON, (int)numericUpDownPERCEPTRON.Value);
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.AND, (int)numericUpDownAND.Value);
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.TRANSISTOR, (int)numericUpDownTRANSISTOR.Value);
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.IF, (int)numericUpDownIF.Value);
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.MAX, (int)numericUpDownMAX.Value);
        PersistentConfig.Settings.CellTypeRatios.Add(CellType.MIN, (int)numericUpDownMIN.Value);

        FitnessScoreMultipliers.Settings.AccuracyMultiplier = (float)numericUpDownAccuracy.Value;
        FitnessScoreMultipliers.Settings.LevelMultiplier = (float)numericUpDownAdditionalLevel.Value;
        FitnessScoreMultipliers.Settings.LivesMultiplier = (float)numericUpDownLives.Value;
        FitnessScoreMultipliers.Settings.KillsAvoidedMultiplier = (float)numericUpDownKillsAvoided.Value;
        FitnessScoreMultipliers.Settings.SaucerMultiplier = (float)numericUpDownHitSaucer.Value;
        FitnessScoreMultipliers.Settings.ShieldsShotMultiplier = (float)numericUpDownShieldsShot.Value;
        FitnessScoreMultipliers.Settings.ScoreMultiplier = (float)numericUpDownScore.Value;
        FitnessScoreMultipliers.Settings.InvaderMultiplier = (float)numericUpDownHitInvader.Value;
        FitnessScoreMultipliers.Settings.PunishmentForInvadersReachingBottom = (float)numericUpDownGroundPunishment.Value;

        // retrieve the rules + template
        ComboBox[] levelComboBoxes = new[] { comboBoxLevelBrain1, comboBoxLevelBrain2, comboBoxLevelBrain3, comboBoxLevelBrain4, comboBoxLevelBrain5,
                                             comboBoxLevelBrain6, comboBoxLevelBrain7, comboBoxLevelBrain8, comboBoxLevelBrain9, comboBoxLevelBrain10 };

        TextBox[] levelRuleInputs = new[] { textBoxLevelRule1, textBoxLevelRule2, textBoxLevelRule3, textBoxLevelRule4, textBoxLevelRule5,
                                            textBoxLevelRule6, textBoxLevelRule7, textBoxLevelRule8, textBoxLevelRule9, textBoxLevelRule10 };

        TextBox[] startScoreInputs = new[] { textBoxStartScore1, textBoxStartScore2, textBoxStartScore3, textBoxStartScore4, textBoxStartScore5,
                                            textBoxStartScore6, textBoxStartScore7, textBoxStartScore8, textBoxStartScore9, textBoxStartScore10 };


        // store the rules + templates
        PersistentConfig.Settings.BrainTemplates.Clear();

        for (int i = 0; i < 10; i++)
        {
            string level = levelRuleInputs[i].Text;
            string? template = "";

            if (levelComboBoxes[i].SelectedIndex > 0) template = levelComboBoxes[i].SelectedItem.ToString();

            template ??= "";

            RuleLevelBrain rb = new()
            {
                StartingScore = string.IsNullOrWhiteSpace(startScoreInputs[i].Text) ? 0 : int.Parse(startScoreInputs[i].Text),
                LevelRule = level,
                BrainTemplateFileName = template
            };

            PersistentConfig.Settings.BrainTemplates.Add(i, rb);
        }

        // 0="-none selected", so we need to exclude that.
        if (comboBoxListOfTemplates.SelectedIndex > 0)
        {
            PersistentConfig.Settings.Template = comboBoxListOfTemplates.SelectedItem.ToString();
        }
        else
        {
            PersistentConfig.Settings.Template = "";
        }

        // some basic validation is required to protect the application
        if (!IsValid()) return;

        // store the hidden layers
        if (radioButtonPerceptron.Checked)
        {
            PersistentConfig.Settings.AIHiddenLayers = LayersFromText(textBoxPerceptronLayers.Text);
        }

        // automatically persists config for next time
        PersistentConfig.Save(c_invadersConfig);
        FitnessScoreMultipliers.Save(c_invadersScore);
    }

    /// <summary>
    /// Validation.
    /// </summary>
    /// <returns></returns>
    private bool IsValid()
    {
        int total = 0;

        foreach (var x in PersistentConfig.Settings.CellTypeRatios.Values) total += x;

        // VALIDATION
        if (total < 1)
        {
            buttonLearn.Enabled = true;
            MessageBox.Show("cell type ratios must be at least 1.");
            return false;
        }

        if (PersistentConfig.Settings.PercentOfBrainsPreservedDuringMutation + PersistentConfig.Settings.PercentOfBrainsToCreateAsNewRandomDuringMutation + PersistentConfig.Settings.PercentOfBrainsToCreateFromTemplate > 100)
        {
            buttonLearn.Enabled = true;
            MessageBox.Show("Percentage of brains preserved + percentage of brains + percentage of brains created from a template as new random must be less than 100.");
            return false;
        }

        // if we are creating a random network, validate the number of neurons
        if (PersistentConfig.Settings.CreatingARandomNetwork)
        {
            if (PersistentConfig.Settings.MaximumNumberOfNeurons < PersistentConfig.Settings.MinimumNumberOfNeurons)
            {
                buttonLearn.Enabled = true;
                MessageBox.Show("Maximum number of neurons must be greater than or equal to minimum number of neurons.");
                return false;
            }

            if (PersistentConfig.Settings.MinimumNumberOfNeurons > PersistentConfig.Settings.DesiredRandomNeurons)
            {
                buttonLearn.Enabled = true;
                MessageBox.Show("The minimum number of neurons must be less than or equal to the desired number of neurons.");
                return false;
            }

            if (PersistentConfig.Settings.MaximumNumberOfNeurons < PersistentConfig.Settings.DesiredRandomNeurons)
            {
                buttonLearn.Enabled = true;
                MessageBox.Show("The maximum number of neurons must be greater less than or equal to the desired number of neurons.");
                return false;
            }
        }

        if (PersistentConfig.Settings.AllowedMutationMethods.Contains(MutationMethod.ModifyActivationFunction) && PersistentConfig.Settings.AllowedActivationFunctions.Length == 1)
        {
            buttonLearn.Enabled = true;
            MessageBox.Show("You cannot have only one activation function and allow mutation to modify the activation function.  Please select more than one activation function or remove the mutation method 'Modify Activation Function'.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the list of mutation methods that are checked/selected.
    /// </summary>
    /// <returns></returns>
    private MutationMethod[] GetSelectedMutationMethods()
    {
        List<MutationMethod> selectedMutations = new();

        foreach (var x in checkedListBoxAllowedMutations.CheckedItems)
        {
            selectedMutations.Add((MutationMethod)x);
        }

        return selectedMutations.ToArray();
    }

    /// <summary>
    /// Retrieves the list of activation functions that are checked/selected.
    /// </summary>
    /// <returns></returns>
    private ActivationFunction[] GetSelectedActivationFunctions()
    {
        List<ActivationFunction> selectedFunctions = new();

        foreach (var x in checkedListBoxActivationFunctions.CheckedItems)
        {
            selectedFunctions.Add((ActivationFunction)x);
        }

        return selectedFunctions.ToArray();
    }

    /// <summary>
    /// Parses the text in the hidden layers textbox and returns an array of int.
    /// Any that don't parse are ignore.
    /// 0 does not = hidden layers, it means use the number of inputs.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static int[] LayersFromText(string text)
    {
        // layers look like: 100,50,10.
        string[] layerStrings = text.Replace(" ", "").Split(',');
        List<int> layers = new();

        foreach (string layer in layerStrings)
        {
            if (int.TryParse(layer, out int layerInt) && layerInt >= 0)
            {
                layers.Add(layerInt);
            }
        }

        // arbitrary "20" if none are specified. No particular reason.
        if (layers.Count < 1) layers.Add(20);

        return layers.ToArray();
    }

    /// <summary>
    /// Turns the selected value from the drop down of selection types into a corresponding enum.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private LearningFramework.SelectionType GetSelectionTypeFromComboBox()
    {
        return comboBoxSelectionType.SelectedIndex switch
        {
            0 => LearningFramework.SelectionType.POWER,
            1 => LearningFramework.SelectionType.FITNESS_PROPORTIONATE,
            2 => LearningFramework.SelectionType.TOURNAMENT,
            _ => throw new ApplicationException("unknown selection type in combo-box"),
        };
    }

    /// <summary>
    /// When the form loads, reduce the height because only one panel is visible at once, and at design time, there are 2.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_Load(object sender, EventArgs e)
    {
        Height -= panelRandom.Height;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RadioButtonPerceptron_CheckedChanged(object sender, EventArgs e)
    {
        panelRandom.Visible = false;
        panelPerceptron.Visible = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RadioButtonRandomNeurons_CheckedChanged(object sender, EventArgs e)
    {
        panelRandom.Visible = true;
        panelPerceptron.Visible = false;
    }

    private static void CheckboxChanged(CheckBox checkBox, NumericUpDown numericUpDown)
    {
        if (!checkBox.Checked)
        {
            numericUpDown.Value = 0;
        }
        else
        {
            if (numericUpDown.Value == 0) numericUpDown.Value = 1;
        }
    }

    /// <summary>
    /// Automatically checks/unchecks the checkbox when the numeric up-down is changed.
    /// </summary>
    /// <param name="nud"></param>
    /// <param name="cb"></param>
    private static void ValueChanged(NumericUpDown nud, CheckBox cb)
    {
        if (nud.Value == 0)
            cb.Checked = false;
        else
            cb.Checked = true;
    }

    private void NumericUpDownPERCEPTRON_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownPERCEPTRON, checkBoxPERCEPTRON);
    }

    private void NumericUpDownAND_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownAND, checkBoxAND);
    }

    private void NumericUpDownMAX_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownMAX, checkBoxMAX);
    }

    private void NumericUpDownMIN_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownMIN, checkBoxMIN);
    }

    private void NumericUpDownIF_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownIF, checkBoxIF);
    }

    private void NumericUpDownTRANSISTOR_ValueChanged(object sender, EventArgs e)
    {
        ValueChanged(numericUpDownTRANSISTOR, checkBoxTRANSISTOR);
    }

    private void CheckBoxPERCEPTRON_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxPERCEPTRON, numericUpDownPERCEPTRON);
    }

    private void CheckBoxAND_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxAND, numericUpDownAND);
    }

    private void CheckBoxMAX_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxMAX, numericUpDownMAX);
    }

    private void CheckBoxMIN_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxMIN, numericUpDownMIN);
    }

    private void CheckBoxIF_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxIF, numericUpDownIF);
    }

    private void CheckBoxTRANSISTOR_CheckedChanged(object sender, EventArgs e)
    {
        CheckboxChanged(checkBoxTRANSISTOR, numericUpDownTRANSISTOR);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ComboBoxListOfTemplates_SelectedIndexChanged(object sender, EventArgs e)
    {
        // if they select a no template, make the template % 0.
        if (comboBoxListOfTemplates.SelectedIndex < 1)
        {
            // none selected, templated is disabled.
            numericUpDownTemplated.Value = 0;
            return;
        }
        else
        {
            // default to 5 if 0.
            if (numericUpDownTemplated.Value == 0) numericUpDownTemplated.Value = 5;
        }
    }

    /// <summary>
    /// Plays using assigned template(s).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonPlay_Click(object sender, EventArgs e)
    {
        PersistentConfig.Settings.Mode = PersistentConfig.FrameworkMode.playing;

        StoreConfig();

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Provide an explanation of the selection type.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ComboBoxSelectionType_SelectedIndexChanged(object? sender, EventArgs? e)
    {
        labelSelectionTypeDescription.Text = comboBoxSelectionType.SelectedIndex switch
        {
            0 => "selects a random genome from the population, where the chance to pick a genome is proportional to its rank (favouring higher scoring).",
            1 => "Selects a random genome from the population, where the chance to pick a genome is proportional to its fitness.",
            2 => "Makes an entirely random selection of brains from the population (up to the selection size), sorts them in descending order, stepping in order best to worst it stops on a brain if the random number generator picks it.",
            _ => "Unknown selection type.",
        };
    }

    /// <summary>
    /// I accidentally crashed the app by changing this, and forgetting that the neuron mismatch between input/out and template definition are incompatible.
    /// Thus when this changes, it refreshes the list of templates to match suitable brains; and informs the user.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RadioButtonAIInputOrOutputMethodChanged_Click(object sender, EventArgs e)
    {
        if (comboBoxLevelBrain1.SelectedIndex > 0)
        {
            MessageBox.Show("Changing the input/output method requires use of different brain templates.\n\nSelected templates for level rules have been reset.", "Space Invader AI - Information", MessageBoxButtons.OK);
        }

        AddTemplatesToDropDown();
    }
}