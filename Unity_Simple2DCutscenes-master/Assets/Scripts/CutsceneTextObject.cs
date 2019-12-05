using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;

/* A object that stores, types letter by letter, and manipulates text for a cutscene
 * Fonts should be stored in directory Assets/Resources/Fonts
 * 
 * Created by Steven Shing, 12/2018
 * Open Source
 */

public class CutsceneTextObject : MonoBehaviour
{
    const float TEXT_FADE_SPEED = 1;         // text fade speed

    Text text;                          // the text this handler is editing
    string textToShow;                  // the string that the text will display
    float holdTime;                     // the amount of time that the text will delay spawning the next text or moving to the next batch or frame
    CutsceneManager cutsceneManager;    // the CutsceneManager handling this object
    float maxAlpha;                     // the max alpha the text will reach

    void Awake()
    {
        cutsceneManager = transform.parent.GetComponent<CutsceneManager>(); // get the CutsceneManager from this object's parent

        text = GetComponent<Text>();                        // grab the text component
        text.horizontalOverflow = HorizontalWrapMode.Wrap;  // set the horizontal wrap mode to wrap
        text.verticalOverflow = VerticalWrapMode.Truncate;  // set the vertical wrap mode to truncate (this way the user can know if their text would overflow when creating a cutscene
    }

    // Initialize this object given specific CutsceneTextData and the amount of time to delay typing each letter (the higher this value, the slower it types
    public void Init(CutsceneTextData ctd, float typeDelaySpeed)
    {

        // check for default font
        if (!ctd.font.Equals("Arial"))
        {
            // attempt to find the font in Assets/Resources/Fonts
            try
            {
                text.font = Resources.Load<Font>("Fonts/" + ctd.font);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not find font. Escaping CutsceneTextObject Init.\n" + e.Message);
                return;
            }
        }
        else
        {
            text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        }

        text.text = ""; // set the text to empty for now
        text.fontSize = ctd.fontSize;   // set the font size

        // attemp to parse the enum for the TextAnchor
        try
        {
            text.alignment = (TextAnchor)Enum.Parse(typeof(TextAnchor), ctd.textAnchor);
        }
        catch (Exception e)
        {
            Debug.LogError("TextAnchor string to enum parse failed. Escaping CutsceneTextObject Init.\n" + e.Message);
            return;
        }

        text.color = new Color(ctd.fontColor[0], ctd.fontColor[1], ctd.fontColor[2], 0); // set the text color - set alpha to 0 so we can fade the text in
        maxAlpha = ctd.fontColor[3];

        textToShow = ctd.textToShow;    // save the full text we're going to display in the end
        holdTime = ctd.holdTime;        // save the hold time we'll wait before displaying the next text

        RectTransform rectTransform = GetComponent<RectTransform>();    // grab the rect transform
        rectTransform.localPosition = new Vector3(ctd.position[0], ctd.position[1], ctd.position[2]);   // set the position of the text in the scene
        rectTransform.sizeDelta = new Vector2(ctd.sizeDelta[0], ctd.sizeDelta[1]);  // set the size of the text field in the scene

        if(cutsceneManager.typeText)
            TypeSentence(typeDelaySpeed);   // begin typing the sentence with the set typeDelaySpeed
        else
            DisplayAndFade();       // display the entire sentence, but fade in the text
    }

    // types out a new sentence
    // @typeDelaySpeed - how long to wait before typing a new letter - the higher this value, the slower it types
    public void TypeSentence(float typeDelaySpeed)
    {
        StopAllCoroutines();                        // stop all coroutines in case we force advance before any coroutines finish
        StartCoroutine(TypeText(typeDelaySpeed));   // start the text typing coroutine
    }

    // displays the whole sentence
    public void DisplayWholeSentence()
    {
        StopAllCoroutines();                    // stop all coroutines as the player force advanced past them
        text.text = textToShow;                 // reveal the entire sentence

        text.color = new Color(text.color.r, text.color.g, text.color.b, maxAlpha); // fully reveal the text including setting it's alpha

        cutsceneManager.TextComplete(holdTime); // let the manager know that this text is completed
    }

    // reset text to blank
    public void SetTextToBlank()
    {
        text.text = "";
    }

    // fully display the text, but fade it in
    void DisplayAndFade()
    {
        text.text = textToShow;
        StartCoroutine(FadeText(maxAlpha, TEXT_FADE_SPEED));
    }

    // Coroutine for typing out text letter by letter
    IEnumerator TypeText(float typeDelaySpeed)
    {
        text.text = "";              // set text of the current textArea in the pattern to empty

        // reveal each character letter by letter from the final string of textToShow
        foreach (char letter in textToShow.ToCharArray())
        {
            text.text += letter;    // add a single letter to the text
            yield return new WaitForSeconds(typeDelaySpeed);    // delay by typeDelaySpeed
        }

        cutsceneManager.TextComplete(holdTime); // when we're done, let the CutsceneManager know
    }

    // Coroutine for fading text - NOTE: Currently only being used to fade in text
    IEnumerator FadeText(float aValue, float fadeSpeed)
    {
        float alpha = text.color.a; // retrieve the current text alpha
        // slowly adjust the alpha over fadeSpeed seconds
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeSpeed)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Lerp(alpha, aValue, t)); // interpolate the alpha to the new value to make the change smooth
            yield return null;
        }

        // This next line is only here because this function is only being used to fade in text at the moment
        cutsceneManager.TextComplete(holdTime); // let the manager know that this text is completed and fully showing
    }
}
