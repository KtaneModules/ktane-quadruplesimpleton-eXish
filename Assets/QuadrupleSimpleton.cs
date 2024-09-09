using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using random = UnityEngine.Random;

//note: all the fields/methods I'm not commenting should be auto-documentable
public class QuadrupleSimpleton : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule M;
    public KMSelectable Module;
    public KMSelectable RefButton;
    public GameObject StatusLight;
        public KMRuleSeedable RuleSeed;
        private int seed;

    private int side; //how many buttons will the NxN square have?
    private bool solved;
    private float originalY; //original y position of the button
    private bool muted = false;
    private GameObject workaround; //see WorkaroundTheWarning
    private IButtonBehaviour behaviour;
    private List<KMSelectable> buttons = new List<KMSelectable>();
        static int moduleIdCounter = 1;
        int moduleId;

    private void Awake()
    {
        moduleId = moduleIdCounter++; //every module has to have an ID number, starting from 1. Although... The LFA needs both moduleIdCounter AND moduleId... Oh well
        originalY = RefButton.transform.localPosition.y; //used in ButtonPush
        seed = RuleSeed.GetRNG().Seed; //note: GetRNG just returns a MonoRandom which has the property I need ("Seed")
        if (!IsRuleseed()) ModuleLog("T means Top, B means Bottom, L means Left, R means Right.");
        MakeBehaviourDecision();
        WorkaroundTheWarning();
        MakeButtons(); //put the neccessary number of buttons on the module
    }

    private void Start()
    {
        M.OnActivate += () => { if (TwitchPlaysActive) ModuleLog("TP KTANE IS ACTIVE."); };
        TwitchHelpMessage = behaviour.HelpMessage;
    }

    //no explanation needed
    private void ModuleLog(string message) {
        Debug.LogFormat("[Quadruple Simpleton #{0}] {1}", moduleId, message);
    }

    private bool IsRuleseed() { return seed != 1; }

    /* It does two things: (based off "seed")
     * - Sets "side" a value
     * - Modifies the status light (to be visible or not)
     */
    private void MakeBehaviourDecision()
    {
        if (!IsRuleseed())
            side = 2;
        else
        {
            side = RuleSeed.GetRNG().Next(9) % 9 + 3; //Interval: [3, 11]
            StatusLight.SetActive(false);
        }
    }

    //workaround for the warning: "You are trying to create a MonoBehaviour using the 'new' keyword. This is not allowed. MonoBehaviours can only be added using AddComponent()."
    //instanciates the appropiate IButtonBehaviour class without using the "new" keyword, but withing a GameObject
    private void WorkaroundTheWarning()
    {
        workaround = new GameObject();
        if (IsRuleseed())
        {
            behaviour = workaround.AddComponent<RandomBehaviour>(); //AddComponent<RandomBehaviour>() returns that RandomBehaviour component (as well)
            RandomBehaviour.PutConstructorPropierty(ref workaround, side);
        }
        else
            behaviour = workaround.AddComponent<NormalBehaviour>();
    }

    /* Mirrors the X axis, taking the middle as the center
     * Example:
     * -- Input:
     *   1 2 3
     *   4 5 6
     *   7 8 9
     * -- Output:
     *   7 8 9
     *   4 5 6
     *   1 2 3
     */
    private int MirrorX(int input)
    {
        int total = side * side; //total number of buttons
        int rowNumber = (input - 1) / side; //the row position the input is in the grid; top to bottom from 0
        int isRowAfterMiddle = Convert.ToInt32(rowNumber > side / 2 - 1); //true => 1; false => 0
        int i = isRowAfterMiddle * (side - 1) + ((int)Math.Floor(Math.Tan(-isRowAfterMiddle)) + 1) * rowNumber; //isRowAfterMiddle ? i = side - rowNumber - 1 : rowNumber
        int offset = total - side * (2 * (side - (side - i)) + 1); //i and offset are byproducts of the generalisation. Refer to testQSimp.txt
        return input + ((int)Math.Floor(Math.Tan(-isRowAfterMiddle)) + 1) * offset; //isRowAfterMiddle ? input - offset : input + offset
    }

    private void MakeButtons()
    {
        for (int i = 0; i < side * side; i++)
        {
            Transform clone = Instantiate(RefButton.transform, RefButton.transform.parent);
            clone.transform.localScale =
                behaviour.CalculateSize(
                    clone.transform.localScale.x,
                    clone.transform.localScale.y,
                    clone.transform.localScale.z);
            clone.transform.localPosition =
                behaviour.CalculatePosition(MirrorX(i + 1) - 1, originalY);

            buttons.Add(clone.GetComponent<KMSelectable>());
        }
        Module.Children = buttons.ToArray();
        Module.UpdateChildrenProperly();
        Destroy(RefButton.gameObject);
        HookButtons(buttons);
    }
    private void HookButtons(List<KMSelectable> buttons) {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].OnInteract += ButtonHandlerDelegateInstance(buttons[i], i);
    }
    KMSelectable.OnInteractHandler ButtonHandlerDelegateInstance(KMSelectable b, int p)
    {
        return delegate { ButtonHandler(b, p); return false; };
    }

    private void DoEasterEgg(bool forced = false)
    {
        Audio.PlaySoundAtTransform("Lo-hicimos", Module.transform);

        string[] extraStrings;
        if (forced) extraStrings = new string[] { "You", "didn't", "do", "it!", "You forced solved. Shame." };
        else extraStrings = new string[] { "We did", "it!", "¡Lo", "hicimos!", "You did it!! Congrats! :D" };

        for (int i = 0; i < 4; i++)
            buttons[i].GetComponentInChildren<TextMesh>().text = extraStrings[i];

        ModuleLog(extraStrings[4]);
    }

    private void ButtonHandler(KMSelectable button, int position)
    {
        bool alreadyPressed = button.GetComponentInChildren<TextMesh>().text != "PUSH IT!";

        if (alreadyPressed)
        {
            if (!muted) Audio.PlaySoundAtTransform("boing", button.transform);
            if (TwitchPlaysActive) button.AddInteractionPunch(100f);

            ModuleLog(behaviour.AgainMessage(position));
            //already been solved?
            if (solved) button.AddInteractionPunch(100f);
            else StartCoroutine(ButtonPush(button.transform));
        }
        else
        {
            if (!muted) Audio.PlaySoundAtTransform("Victory", Module.transform);
            if (TwitchPlaysActive) StartCoroutine(ButtonPush(button.transform));

            button.AddInteractionPunch();
            ModuleLog(behaviour.ButtonMessage(position));
            button.GetComponentInChildren<TextMesh>().text = "VICTORY!";
            //not solved? check
            solved = behaviour.CheckSolve();
            if (solved)
            {
                M.HandlePass();
                ModuleLog("SOLVED!");
                if (!IsRuleseed())
                {
                    if (random.Range(0, 50) == 0)
                        DoEasterEgg();
                }
                else
                {
                    if (!muted) Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Module.transform);
                    StartCoroutine(RandomSolved());
                }
            }
        }
    }

    private void ColorButtons(Color color)
    {
        foreach (KMSelectable button in buttons)
            button.GetComponentInChildren<TextMesh>().color = color;
    }
    IEnumerator RandomSolved()
    {
        for (int i = 0; i < 2; i++)
        {
            ColorButtons(Color.green);
            yield return new WaitForSeconds(0.4f);
            ColorButtons(Color.white);
            yield return new WaitForSeconds(0.4f);
        }
        ColorButtons(new Color(0, 0.8f, 0));
    }

    IEnumerator ButtonPush(Transform pressedButtonTransform)
    {
        pressedButtonTransform.localPosition =
            new Vector3(pressedButtonTransform.localPosition.x,
                        originalY - 0.002f,
                        pressedButtonTransform.localPosition.z);
        yield return new WaitForSeconds(0.2f);
        pressedButtonTransform.localPosition =
            new Vector3(pressedButtonTransform.localPosition.x,
                        originalY,
                        pressedButtonTransform.localPosition.z);
    }

#pragma warning disable 414 //created but not used
    private string TwitchHelpMessage;
#pragma warning restore 414
#pragma warning disable 649 //not assigned and will always have the default value
    bool TwitchPlaysActive;
#pragma warning disable 649

    private bool ValueObliesThreshold(string value, int threshold)
    {
        if (value.Length > 3) value = "999";
        return Convert.ToInt32(value) <= threshold;
    }

    //the idea is: for example, take this array [132, 1, 5] and the threshold of 10, so... 132 -> 13 (/< 10) | 2 (separate the 13 from the 2, and comparing 13 with 10) -> <1> (< 10) | 32 => [1, 321, 5] => 321 -> 32 | 1 -> <3> | 21 => [1, 3, 215] ...
    private string ParseCommandNumbers(List<string> input) //see "CurrentIdea.png"
    {
        int totalButtons = side * side;
        var output = new List<string>();
        for (int i = 0; i < input.Count; i++)
        {
            string buffer = input[i];
            for (int j = buffer.Length - 1; j >= 0; j--)
            {
                if (ValueObliesThreshold(buffer, totalButtons))
                {
                    output.Add(buffer);
                    break;
                }
                if (i == input.Count - 1) input.Add("");
                input[i + 1] = buffer.Substring(j) + input[i + 1];
                buffer = buffer.Substring(0, j);
            }
        }
        return string.Join(" ", output.ToArray());
    }

    //welp, this is what you get if you want to allow the users to chain commands (and actually not braking the TP system of your module)
    private string ParseChainCommand(string input)
    {
        if (new string[] {"nothing", "n", "mute", "m"}.Contains(input)) return input;
        Match commands = behaviour.CommandRegex.Match(input.Trim());
            
        if (commands.Success)
        {
            string presses = ParseCommandNumbers(commands.Groups["number"].Captures.Cast<Capture>().Join().Split().ToList()); //.Join() does magic things
            int numberOfPresses = presses.Split().Length;
            if (presses.RegexMatch(new string[] { "^(0 ?)+", @"( |^)(0+\d+ ?)+", " 0$" })) return "sx"; //since this method offers me to input a string, for readability purposes, I won't just mash up all the regexes
            else if (numberOfPresses > behaviour.ChainLimit) return string.Format("sendtochaterror Sorry! You exceeded the number of buttons you can press at a time, which in this case is {0} (you tried to press {1} buttons). I would've striked you, but I feel lazy.\tThe end? Question mark???", behaviour.ChainLimit, numberOfPresses);
            else return presses;
        }
        return "sx";
    }

    //example: "{1}{1}{0}{1}{1}{0}{0}{0}"
    private string GenerateSalt() {
        return Convert.ToString(random.Range(0, 256), 2) //8 bits
            .PadLeft(8, '0')
            .Select(n => "{"+n+"}")
            .ToArray()
            .Join()
            .Replace(" ", "");
    }

#pragma warning disable 414 //created but not used
    IEnumerator ProcessTwitchCommand(string input)
#pragma warning restore 414
    {
        string logMessage = string.Format("You did the command: <<{0}>>. ", input);
        string chainReturnValue = ParseChainCommand(input);
        if (chainReturnValue[0] != 's')
        {
            logMessage += "(valid)";
            ModuleLog(logMessage);
            switch (char.ToLowerInvariant(chainReturnValue[0]))
            {
                case 'n':
                    yield return "antitroll Refused to execute the easter egg: troll commands are disabled.";
                    ModuleLog("You did the \"nothing\" command. u funni person eh");
                    Audio.PlaySoundAtTransform("boing", M.transform);
                    yield return string.Format("sendtochat YES! YOU DID NOTHING! WOOHOO!! {0} (well, as if you were actually doing something to solve the module, huh)", GenerateSalt());
                    break;
                case 'm':
                    if (muted)
                    {
                        ModuleLog("Already muted.");
                        yield return "sendtochaterror Already muted.";
                    }
                    else if (IsRuleseed())
                    {
                        muted = true;
                        ModuleLog("Module muted.");
                        yield return "sendtochat Okay. Fine. TTwTT." + Environment.NewLine + "There's no turning back, eh?";
                    }
                    else
                    {
                        ModuleLog("Tried to mute in non-ruleseed mode.");
                        yield return "sendtochaterror You need to be in ruleseed in order to mute the module.";
                    }
                    break;
                default:
                    int[] numbers = Array.ConvertAll(chainReturnValue.Split(), Convert.ToInt32);
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        buttons[numbers[i] - 1].OnInteract();
                        if (i != numbers.Length - 1) yield return new WaitForSeconds(0.3f);
                    }
                    yield return null;
                    break;
            }
        }
        else
        {
            logMessage += "(wrong: ";
            if (chainReturnValue[1] == 'e')
            {
                yield return chainReturnValue;
                logMessage += "too many buttons chained, ";
            }
            logMessage += "not executed)";
            ModuleLog(logMessage);
        }

        yield break;
    }
#pragma warning disable 414 //created but not used
    IEnumerator TwitchHandleForcedSolve()
#pragma warning restore 414
    {
        if (solved) yield break;
        if (!IsRuleseed())
        {
            DoEasterEgg(true);
            yield return new WaitForSeconds(7f);
        }
        else
        {
            //all fun and jokes until you realise you actually can't solve too many buttons with sound; otherwise, just, earrape
            float[] interactDelays = new float[] { 0.2f, 0.045f, 0.004f }; //careful with these values
            float[] afterInteractDelays = new float[] { 2f, 2.5f, 2.6f };
            bool[] mutes = new bool[] { false, false, true }; //specially these
            int[] sidesThresholds = new int[] { 5, 7 };
            int i;

            for (i = 0; i < 2; i++)
                if (side < sidesThresholds[i]) break; //<<else if>> generalisation
            muted = mutes[i];

            Audio.PlaySoundAtTransform("Lo-hicimos", Module.transform);
            yield return true;

            for (int j = 0; j < 2; j++)
                yield return StartCoroutine(RandomSolved());

            foreach (KMSelectable button in buttons)
            {
                button.OnInteract();
                yield return new WaitForSeconds(interactDelays[i]);
            }
            yield return new WaitForSeconds(afterInteractDelays[i]);

            foreach (KMSelectable button in buttons)
                button.GetComponentInChildren<TextMesh>().color = Color.red;
        }
        foreach (KMSelectable button in buttons)
            button.GetComponentInChildren<TextMesh>().text = "Shame.";

        ModuleLog("Forced solving compelete.");
        M.HandlePass();
    }
}