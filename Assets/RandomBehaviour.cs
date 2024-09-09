using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class RandomBehaviour : MonoBehaviour, IButtonBehaviour {

    private int _presses = 0;
    private int _n; //no "readonly" here due to the warning; Unity forces me to have the default constructor
    private int _chainLimit;
    public int ChainLimit { get { return _chainLimit; } }

    public static void PutConstructorPropierty(ref GameObject workaround, int sideLength) //static method for the workaround: no constructors allowed here
    {
        RandomBehaviour instance = workaround.GetComponent<RandomBehaviour>(); //GetComponent<RandomBehaviour>() returns that RandomBehaviour component
        instance._n = sideLength;
        instance._chainLimit = Mathf.CeilToInt(21f / 31 * sideLength);
    }

    public Vector3 CalculateSize(float x, float y, float z) { return new Vector3(x / _n, y, z / _n); }

    public Vector3 CalculatePosition(int cloneNumber, float y) //widthDist. and heightDist. are different for the sake of applying two different solutions
    { //module boundaries: [0.1, -0.1]

        float margin = 0.2f / _n + 0.02f + _n / 1000f;

        float widthDistribution =
            Mathf.LerpUnclamped( //unclamped because makes any error visual
                -0.1f + margin / 2,
                 0.1f - margin / 2,
                 cloneNumber % _n / (_n - 1f)) + 2 / _n * 0.0014f;

        float distance = 0.2f - margin;
        float heightDistribution = cloneNumber / _n * distance / (_n - 1) - 0.1f + margin / 2; //magic formula :)

        return new Vector3(widthDistribution, y, heightDistribution);
    }

    public string AgainMessage(int cloneNumber) { return string.Format("You pressed button {0} and you changed absolutely nothing. Hooray!", cloneNumber + 1); }
    public string ButtonMessage(int cloneNumber) { return string.Format("You pressed {0} button{1}.", _presses + 1, _presses + 1 != 1 ? "s" : ""); }

    public bool CheckSolve()
    {
        _presses++;
        return _presses >= _n * _n;
    }

    //Constants:

    private const string _helpMessage =
        "Use <<!{0} (press|p|button|b) n>> to press the nth button (spaces are optional) and <<!{0} m|mute>> to mute the module." +
        " You can chain commands up to p buttons, where p is ⌈(21/31)√b⌉, and b is the number of buttons on the module." +
        " Example: !{0} press 143. Weird example: !{0} 1p3 b4button    2";
    public string HelpMessage { get { return _helpMessage; } }
    //to be fair, I could've put "((p)ress or (b)utton)", but people can understand that as "only p or b"
    //alternatively, I could put (p(ress) or b(utton)), which is the one that makes the most sense, but that would confuse people

    private Regex _commandRegex =
        new Regex(@"^((press|p|button|b)?\s*(?<number>\d{1,3})\s*)+$", //love those highlights
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture); //flags: 101
    public Regex CommandRegex { get { return _commandRegex; } }

    /* Magic formula explained:
     * i => index (0 to n) of the button
     * n => height/width of the distribution. Recall n*n = total number of buttons
     * 
     * Notice: <<foreach i in array2D: array2D[i%height,i/width]>> will display all the elements in array2D
     * 
     * i%n is the index in the row. If you do index/(n-1), you will get all the distances
     * you need to evenly distribute n elements in an area.
     * Since this relationship is a unitary distance, I will multiply it by the distance
     * between the left side and the rigth side of the module, because that is the total
     * length my row will be in.
     * I am assuming the starting point (the left part of the module) is 0, so to get the buttons
     * inside the actual module I will subtract 0.1 from the result.
     * Finally, since I took up a bit of "distance" for making margin, I need to put it back into
     * the calculation so my starting point remains at -0.1, hence margin/2.
     */
}
