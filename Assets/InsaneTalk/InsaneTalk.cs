using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

public class InsaneTalk : MonoBehaviour
{
    public TextAsset flavorText;
    public Text textDisplay;
    public KMSelectable[] buttons;
    public MeshRenderer[] leds;
    public KMBombInfo bombInfo;

    public Material off;
    public Material green;
    public Material red;

    string textOption, condensedOption;
    Dictionary<string, string> textOptions = new Dictionary<string, string>();
    bool isActive = false;
    int[] buttonNumbers;
    bool[] buttonStates;
    int currentIndex;
    int stage = 0;
    int maxStageAmount = 1;
    static int _moduleIdCounter = 1;
    int _moduleId = 0;

    //Called by Unity
    void Start()
    {
        _moduleId = _moduleIdCounter++;
        textOptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(flavorText.text);
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    //Called by KTaNE (KMBombModule.OnActivate)
    void OnActivate()
    {
        stage = 0;
        isActive = true;

        for(int i = 0; i < buttons.Count(); i++)
        {
            //Delegates use the last value of i, create a new j value for each increase in i.
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }
        Randomize();
    }

    void Randomize()
    {
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        buttonStates = new[] { false, false, false, false };
        buttonNumbers = new[] { -1, -1, -1, -1 };
        for (int i = 0; i < buttonNumbers.Count(); i++)
        {
            var num = UnityEngine.Random.Range(0, 10);
            while (buttonNumbers.Contains(num))
                num = UnityEngine.Random.Range(0, 10);
            buttonNumbers[i] = num;
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = buttonNumbers[i].ToString();
        }

        //Select transfers the Dictionary options into an Ienumerable of key values, which is then made into a list so it can be indexed.
        textOption = textOptions.Select(x => x.Key).ToList()[UnityEngine.Random.Range(0, textOptions.Count)];
        string text = textOptions[textOption];
        textDisplay.text = text;
        Log("The chosen phrase was {0}", text);
        Log("The expected set of numbers is {0}", textOption);
        //Remove duplicate characters, make all of the characters ints, and only include values that appear in buttonNumbers
        var textOptionArray = textOption.Distinct().Select(x => x - '0').Where(x => buttonNumbers.Contains(x));
        condensedOption = string.Join("", Joiner(textOptionArray));
        Log("The expected sequence is {0}", condensedOption == "" ? "anything." : condensedOption + ", followed by any other values if applicable.");
        currentIndex = 0;
    }

    //To use different kinds of IEnumerables (such as arrays or lists) in string.Join()
    string[] Joiner<T>(IEnumerable<T> obj)
    {
        return obj.Select(x => x.ToString()).ToArray();
    }
    
    void OnPress(int pressedButton)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (isActive)
        {
            Log("You pressed {0}.", buttonNumbers[pressedButton]);
            if (currentIndex < condensedOption.Length)
                buttonStates[pressedButton] = buttonNumbers[pressedButton] == condensedOption[currentIndex] - '0';
            else
                buttonStates[pressedButton] = !buttonStates[pressedButton];
            bool buttonIsCorrect = buttonStates[pressedButton];
            if (buttonIsCorrect)
            {
                leds[pressedButton].material = green;
                currentIndex++;
 
                if (buttonStates[0] && buttonStates[1] && buttonStates[2] && buttonStates[3])
                {
                    stage++;
                    if (stage == maxStageAmount)
                    {
                        Log("Solved!");
                        textDisplay.text = "";
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                        GetComponent<KMBombModule>().HandlePass();
                        isActive = false;
                    }
                    else
                    {
                        Randomize();
                    }
                }
            }
            else
            {
                Log("Are you insane?");
                GetComponent<KMBombModule>().HandleStrike();
                StartCoroutine(RedLights());
            }
        }
    }
    
    IEnumerator RedLights()
    {
        isActive = false;
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = red;
        }
        yield return new WaitForSeconds(1f);
        stage = 0;
        isActive = true;
        Randomize();
    }

    void Log(string log, params object[] obj)
    {
        //Put together the log message before logging it
        log = string.Format(log, obj);
        Debug.LogFormat("[Insane Talk #{0}] {1}", _moduleId, log);
    }

    private string TwitchHelpMessage = "Use '!{0} label 1 2 3 4' to press button with label 1, 2, 3 and 4. Use '!{0} position 1 2 3 4' to press button in position 1, 2, 3 and 4. Buttons are numbered from 1 to 4 going from the top to the bottom.";
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var regex = Regex.Match(command, @"^(label|position|l|p|lab|pos|press) ((?:\d(?: |)){1,4})$");
        if (!regex.Success)
            yield break;
        var digitString = regex.Groups[2].Value.Replace(" ", "").Select(x => x - '0');
        //p can be used for press or position, at least that makes sense to me. It shouldn't matter much since it's not in the help message
        if (regex.Groups[1].Value.StartsWith("p"))
            if (digitString.All(x => x < 5))
            {
                yield return null;
                yield return digitString.Select(x => buttons[x - 1]).ToArray();
            }
            else
                yield break;
        else if (regex.Groups[1].Value.StartsWith("l"))
            if (digitString.All(x => buttonNumbers.Contains(x)))
            {
                yield return null;
                yield return digitString.Select(x => buttons[Array.IndexOf(buttonNumbers, x)]).ToArray();
            }
            else
                yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        var selectables = condensedOption.Select(x => x - '0').Concat(buttonNumbers).Distinct();
        foreach (int num in selectables)
        {
            var i = Array.IndexOf(buttonNumbers, num);
            yield return buttons[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
