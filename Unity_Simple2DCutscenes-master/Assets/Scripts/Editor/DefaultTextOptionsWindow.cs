using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public class DefaultTextOptionsWindow : EditorWindow
{
    string filePath; // the file path to the textDefaults json

    CutsceneTextData defaultTextOptions; // our default text options data

    int fontSize;       // font size
    float holdTime;     // the hold time before the text disappears
    Font font;          // the font
    Color color;        // the color of the font
    TextAnchor anchor;  // the alignment of the text
    Vector3 pos;        // the position of the text object's rect transform
    Vector2 sizeDelta;  // the width and height of the text object's rect transform

    CutsceneCreator cutsceneCreator;

    public CutsceneTextData DefaultTextOptions
    {
        get
        {
            return defaultTextOptions;
        }
    }

    // create the window - we need the cutscene creator that made this window
    public void Init(CutsceneCreator cc)
    {
        DefaultTextOptionsWindow window = (DefaultTextOptionsWindow)GetWindow(typeof(DefaultTextOptionsWindow), true);
        window.Show();

        cutsceneCreator = cc;
        filePath = CutsceneCreator.textDefaultFilePath;

        LoadFromJson(); // load any existing text defaults
    }

    void OnGUI()
    {
        // set up fields to manipulate based on the defaults we want to set for new texts
        GUILayout.Label("Default Text Options", EditorStyles.boldLabel);
        font = (Font)EditorGUILayout.ObjectField("Font: ", font, typeof(Font), false);
        fontSize = EditorGUILayout.IntField("Size: ", fontSize);
        color = EditorGUILayout.ColorField("Color: ", color);
        anchor = (TextAnchor)EditorGUILayout.EnumPopup("Alignment: ", anchor);

        pos = EditorGUILayout.Vector3Field("Position: ", pos);
        sizeDelta = EditorGUILayout.Vector2Field("Width and Height: ", sizeDelta);

        holdTime = EditorGUILayout.FloatField("HoldTime: ", holdTime);

        EditorGUILayout.Space();
        // save the data
        if (GUILayout.Button("Save"))
        {
            defaultTextOptions = new CutsceneTextData();

            defaultTextOptions.textToShow = "Enter your text here.";
            defaultTextOptions.fontSize = fontSize;
            defaultTextOptions.holdTime = holdTime;
            defaultTextOptions.font = font.name;
            defaultTextOptions.textAnchor = anchor.ToString();
            defaultTextOptions.fontColor = new float[] { color.r, color.g, color.b, color.a };
            defaultTextOptions.position = new float[] { pos.x, pos.y, pos.z };
            defaultTextOptions.sizeDelta = new float[] { sizeDelta.x, sizeDelta.y };

            Save();

            cutsceneCreator.textDefaults = defaultTextOptions; // set the text defaults in the cutsceneCreator

            Close();    // close the window when we save
        }
    }

    // load any existing preset defaults
    void LoadFromJson()
    {
        if (File.Exists(filePath))
        {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);
            // Pass the json to JsonUtility, and tell it to create a GameData object from it
            CutsceneTextData loadedData = JsonUtility.FromJson<CutsceneTextData>(dataAsJson);

            // Set parameters based on data from loadedData
            // check for default font
            if (!loadedData.font.Equals("Arial"))
            {
                // attempt to find the font in Assets/Resources/Fonts
                try
                {
                    font = Resources.Load<Font>("Fonts/" + loadedData.font);
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not find font. Escaping CutsceneTextObject Init.\n" + e.Message);
                    return;
                }
            }
            else
            {
                font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            }

            // attempt to parse the enum for the TextAnchor
            try
            {
                anchor = (TextAnchor)Enum.Parse(typeof(TextAnchor), loadedData.textAnchor);
            }
            catch (Exception e)
            {
                Debug.LogError("TextAnchor string to enum parse failed. Escaping CutsceneTextObject Init.\n" + e.Message);
                return;
            }

            // set up font size, hold time, and font color
            fontSize = loadedData.fontSize;
            holdTime = loadedData.holdTime;
            color = new Color(loadedData.fontColor[0], loadedData.fontColor[1], loadedData.fontColor[2], loadedData.fontColor[3]);

            // set up the rect transform's default position and sizeDelta
            pos = new Vector3(loadedData.position[0], loadedData.position[1], loadedData.position[2]);
            sizeDelta = new Vector2(loadedData.sizeDelta[0], loadedData.sizeDelta[1]);
        }
        // These are the defaults set by Unity with the exception of holdTime, which we'll default to the default value of a float
        else
        {
            fontSize = 14;
            color = Color.black;
            anchor = TextAnchor.UpperLeft;
            font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            sizeDelta = new Vector2(160, 30);
            holdTime = 0;
        }
    }

    // save the defaults to a json file
    void Save()
    {
        string json = JsonUtility.ToJson(defaultTextOptions, true);

        File.WriteAllText(filePath, json);  // write and save the file

        AssetDatabase.Refresh();    // update the asset database
    }
}
