using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/* An editor window that allows a user using a CutsceneCreator to manipulate the texts in the scene without having to click on them in the scene or the hiearchy and lose focus from the
 * CutsceneCreatorInspector
 * NOTE: When opening multiple windows at once, they will open on top of each and with the last one in the stack on top. At the time of creation, I don't have the need to edit more than 2
 * texts at a time, so I did not spend the time to address this inconvenient issue. If the need arises or if desired, I will address this.
 * 
 * Created by Steven Shing, 12/2018
 * Open Source
 */

public class CutsceneCreatorTextEditorWindow : EditorWindow {
    Text text;                          // the text we are manipulating
    RectTransform rt;                   // the rect transform of the text we're targeting
    CutsceneTextData ctd;               // the data we are editing
    int targetIndex;                    // the index in the list in CutsceneCreator we will target
    float holdTime;                     // the hold time value for the data
    CutsceneCreator cutsceneCreator;    // the CutsceneCreator we are using

    // Initialize the window
    // requires a text we will edit and a CutsceneCreator we will manipulate the values of
    public void Init(Text textToEdit, CutsceneCreator cc)
    {
        // set up and show the window
        CutsceneCreatorTextEditorWindow window = (CutsceneCreatorTextEditorWindow)GetWindow(typeof(CutsceneCreatorTextEditorWindow), true);
        window.Show();

        text = textToEdit;
        rt = text.gameObject.GetComponent<RectTransform>();
        cutsceneCreator = cc;
        targetIndex = text.transform.GetSiblingIndex(); // the index we target will be whatever the text's transform's index is in the hierarchy
        // check if there is text data in the CutsceneCreator already and if we're editing something existing rather than something new
        if (cutsceneCreator.currentTextDataList.Count > targetIndex && cutsceneCreator.currentTextDataList[targetIndex] != null)
        {
            try
            {
                ctd = cutsceneCreator.currentTextDataList[targetIndex];
                holdTime = ctd.holdTime;    // set the hold time
            }
            catch (Exception e)
            {
                Debug.LogError("Could not find textData of index " + targetIndex + ". Further details: " + e.Message);
            }
        }
        else
        // otherwise, set up the defaults for a new text object and make a new data and add the window to the list
        {
            // Call upon the CutsceneCreator's struct and method to set up the default parameters
            CutsceneCreator.TextAndRectTransform tart = new CutsceneCreator.TextAndRectTransform();

            tart = cc.ParseDataToText(cc.textDefaults, text, rt);

            // set the parameters for the objects this class is using via the struct in CutsceneCreator
            text = tart.text;
            rt = tart.rt;

            // set up holdTime separately as this is not included in the CutsceneCreator's method
            holdTime = cc.textDefaults.holdTime;

            // add the new cutscene text data to the window
            ctd = new CutsceneTextData();
            cutsceneCreator.activeWindows.Add(targetIndex, this);
        }
    }

    void OnGUI()
    {
        // set up fields to manipulate based on the data we want to have in out text data
        // have these fields directly change the text we associated with this window
        GUILayout.Label("Text Edit Options for : " + text.name, EditorStyles.boldLabel);
        text.text = EditorGUILayout.TextField("Text: ", text.text);
        text.font = (Font)EditorGUILayout.ObjectField("Font: ", text.font, typeof(Font), false);
        text.fontSize = EditorGUILayout.IntField("Size: ", text.fontSize);
        text.color = EditorGUILayout.ColorField("Color: ", text.color);
        text.alignment = (TextAnchor)EditorGUILayout.EnumPopup("Alignment: ", text.alignment);

        rt.localPosition = EditorGUILayout.Vector3Field("Position: ", rt.localPosition);
        rt.sizeDelta = EditorGUILayout.Vector2Field("Width and Height: ", rt.sizeDelta);

        holdTime = EditorGUILayout.FloatField("HoldTime: ", holdTime);

        EditorGUILayout.Space();
        // save the data
        if(GUILayout.Button("Save"))
        {
            ctd.textToShow = text.text;
            ctd.font = text.font.name;
            ctd.fontSize = text.fontSize;
            ctd.textAnchor = text.alignment.ToString();
            ctd.fontColor = new float[] { text.color.r, text.color.g, text.color.b, text.color.a };

            ctd.position = new float[] { rt.localPosition.x, rt.localPosition.y, rt.localPosition.z };
            ctd.sizeDelta = new float[] { rt.sizeDelta.x, rt.sizeDelta.y };

            ctd.holdTime = holdTime;

            cutsceneCreator.UpdateTextData(targetIndex, ctd);   // update the data in the CutsceneCreator with the target index

            Close();    // close the window when we save
        }
    }

    // when we close the window, remove it from the dictionary in CutsceneCreator
    void OnDestroy()
    {
        cutsceneCreator.activeWindows.Remove(targetIndex);
    }
}
