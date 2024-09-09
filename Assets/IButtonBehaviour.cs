using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public interface IButtonBehaviour {

    Vector3 CalculateSize(float x, float y, float z);
    Vector3 CalculatePosition(int cloneNumber, float y);
    string AgainMessage(int cloneNumber);
    string ButtonMessage(int cloneNumber);
    bool CheckSolve();
    string HelpMessage { get; }
    Regex CommandRegex { get; }
    int ChainLimit { get; }
}
