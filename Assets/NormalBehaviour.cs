using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class NormalBehaviour : MonoBehaviour, IButtonBehaviour {

    private int _presses = 0;
    private enum Positions { TL, TR, BL, BR };

    public Vector3 CalculateSize(float x, float y, float z) { return new Vector3(0.5f, 2f, 0.5f); }
    public Vector3 CalculatePosition(int cloneNumber, float y) { return new Vector3(widthDists[cloneNumber], y, heightDists[cloneNumber]); }

    public string AgainMessage(int cloneNumber) { return string.Format("You have just pressed the {0} button again. You should be proud of yourself :3", (Positions)cloneNumber); }
    public string ButtonMessage(int cloneNumber) { return string.Format("You pressed the {0} button. Congrats!", (Positions)cloneNumber); }

    public bool CheckSolve()
    {
        _presses++;
        return _presses >= 4;
    }

    //Constants:
    private const string _helpMessage =
        " Use <<!{0} (press|p|button|b) n>> to press the nth button (spaces are optional)." +
        " You can chain presses up to 2 buttons." +
        " Examples: !{0} press 4, !{0} 12, !{0} 1p3 b4button    2";
    public string HelpMessage { get { return _helpMessage; } }

    private Regex _commandRegex =
        new Regex(@"^((press|p|button|b)?\s*(?<number>[1-4]{1,3})\s*)+$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    public Regex CommandRegex { get { return _commandRegex; } }

    private const int _chainLimit = 2;
    public int ChainLimit { get { return _chainLimit; } }

    float[] widthDists = { -0.039f, 0.039f, -0.039f, 0.039f };
    float[] heightDists = { -0.0376f, -0.0376f, 0.0404f, 0.0404f };
}
