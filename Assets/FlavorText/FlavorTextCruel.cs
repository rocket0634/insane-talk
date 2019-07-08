using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
public class FlavorTextOption
{
    public int steam_id;
    public String text;
}
public class FlavorTextCruel : MonoBehaviour
{
    public TextAsset flavorTextJson;
    public Text textDisplay;
    public KMSelectable[] buttons;
    public MeshRenderer[] leds;
    public KMBombInfo bombInfo;

    public Material off;
    public Material green;
    public Material red;
    
    List<FlavorTextOption> textOptions;
    FlavorTextOption textOption;
    bool isActive = false;
    int[] buttonNumbers;
    bool[] buttonStates;
    List<int> moduleIds;
    int stage = 0;
    int maxStageAmount = 1;
    static int _moduleIdCounter = 1;
    int _moduleId = 0;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        textOptions = JsonConvert.DeserializeObject<List<FlavorTextOption>>(flavorTextJson.text);
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void OnActivate()
    {
        stage = 0;
        isActive = true;

        for(int i = 0; i < buttons.Count(); i++)
        {
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }
        Randomize();
    }

    void OnReactivate()
    {
        isActive = true;

        Randomize();
    }

    void Randomize()
    {
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        buttonStates = new[] { false, false, false, false };
        buttonNumbers = new[] { 0, 0, 0, 0 };
        buttonNumbers[0] = UnityEngine.Random.Range(0, 10);
        buttonNumbers[1] = UnityEngine.Random.Range(0, 10);
        do
        {
            buttonNumbers[1] = UnityEngine.Random.Range(0, 10);
        } while (buttonNumbers[1] == buttonNumbers[0]);
        buttonNumbers[2] = UnityEngine.Random.Range(0, 10);
        do
        {
            buttonNumbers[2] = UnityEngine.Random.Range(0, 10);
        } while (buttonNumbers[2] == buttonNumbers[0] || buttonNumbers[2] == buttonNumbers[1]);
        buttonNumbers[3] = UnityEngine.Random.Range(0, 10);
        do
        {
            buttonNumbers[3] = UnityEngine.Random.Range(0, 10);
        } while (buttonNumbers[3] == buttonNumbers[0] || buttonNumbers[3] == buttonNumbers[1] || buttonNumbers[3] == buttonNumbers[2]);
        string choice = "";
        for (int i = 0; i < buttons.Length; i++)
        {
            string label = buttonNumbers[i].ToString();
            choice += label;
            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
        }

        textOption = textOptions[UnityEngine.Random.Range(0, textOptions.Count)];
        textDisplay.text = textOption.text;
        moduleIds = new List<int>();
        for (int i = 0; i < textOptions.Count; i++)
        {
            if (textOptions[i].text == textOption.text && !moduleIds.Contains(textOptions[i].steam_id))
            {
                moduleIds.Add(textOptions[i].steam_id);
            }
        }
    }
    
    void OnPress(int pressedButton)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (isActive)
        {
            Debug.LogFormat("[Insane Talk #{0}] You pressed {1}.", _moduleId, buttonNumbers[pressedButton]);
            bool buttonIsCorrect = !buttonStates[pressedButton];
            List<int> moduleIdsCopy = new List<int>(moduleIds);
            foreach (int id in moduleIds) {
                string steamId = id.ToString();
                for (int i = 0; i < buttonNumbers.Count(); i++)
                {
                    if (!buttonStates[i] && (steamId.IndexOf(buttonNumbers[pressedButton].ToString()) > steamId.IndexOf(buttonNumbers[i].ToString()) || steamId.IndexOf(buttonNumbers[pressedButton].ToString()) < 0) && steamId.IndexOf(buttonNumbers[i].ToString()) >= 0 && textOption.steam_id > 0)
                    {
                        moduleIdsCopy.Remove(id);
                        break;
                    }
                }
            }
            moduleIds = moduleIdsCopy;
            if (moduleIds.Count() == 0)
            {
                buttonIsCorrect = false;
            }
            if (buttonIsCorrect)
            {
                buttonStates[pressedButton] = true;
                leds[pressedButton].material = green;
 
                if (buttonStates[0] && buttonStates[1] && buttonStates[2] && buttonStates[3])
                {
                    stage++;
                    if (stage == maxStageAmount)
                    {
                        Debug.LogFormat("[Insane Talk #{0}] Solved!.", _moduleId);
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
                Debug.LogFormat("[Insane Talk #{0}] Are you insane?", _moduleId);
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
        OnReactivate();
    }
    public string TwitchHelpMessage = "Use '!{0} press a1 a2 b1' to press button a1, a2 and b1! Don't use capitals!";
    IEnumerator ProcessTwitchCommand(string command)
    {
        if(command.Contains("label")){
            string commfinal=command.Replace("label ", "");
		    string[] digitstring = commfinal.Split(' ');
		    int tried;
		    int index =1;
            foreach(string digit in digitstring){
                if(int.TryParse(digit, out tried)){
                    if(index<=4){
                        tried=int.Parse(digit);
                        index+=1;
                        if(buttonNumbers.Contains(tried)){
                            yield return buttons[Array.IndexOf(buttonNumbers, tried)];
                        }
                        else{
                            yield return null;
                            yield return "sendtochaterror Label not found!";
                            yield break;
                        }
                        }
                        
                    else{
                        yield return null;
                        yield return "sendtochaterror Too many digits!";
                        yield break;
                    }
                }
                else{
                    yield return null;
                    yield return "sendtochaterror Digit not valid.";
                    yield break;
                }
            }
            }
        else{
            if(command.Contains("position")){
                string commfinal=command.Replace("position ", "");
            string[] digitstring = commfinal.Split(' ');
            int tried;
            int index =1;
            foreach(string digit in digitstring){
                if(int.TryParse(digit, out tried)){
                    if(index<=4){
                        tried=int.Parse(digit);
                        index+=1;
                        if(tried<5){
                            if(tried>0){
                        yield return buttons[tried-1];
                            }
                            else{
                                yield return null;
                                yield return "sendtochaterror Number too small!";
                                yield break;
                            }
                        }
                        else{
                            yield return null;
                            yield return "sendtochaterror Number too big!";
                            yield break;
                        }
                    }
                    else{
                        yield return null;
                        yield return "sendtochaterror Too many digits!";
                        yield break;
                    }
                }
                else{
                    yield return null;
                    yield return "sendtochaterror Digit not valid.";
                    yield break;
                }
            }
                }
        
        else{
                yield return null;
				yield return "sendtochaterror Invalid command.";
				yield break;
        }
    }
    }
}
