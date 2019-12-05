using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/* A custom Inspector Editor for creating simple cutscenes in 2D using the CutsceneCreator
 * NOTE: Clicking on the scene, objects on the hierarchy, or on assets in the assets folder will cause the editor to lose focus on the CutsceneCreator inspector and any unsaved work will be wiped out immediately
 * When handling the CutsceneCreator, handle everything from its inspector and the CutsceneCreatorTextEditorWindows that it can open
 * 
 * Created by Steven Shing, 12/2018
 * Open Source
 */

[CustomEditor(typeof(CutsceneCreator))]
public class CutsceneCreatorInspector : Editor {
    string sceneName = "";  // the string to use in the field for setting the name of the scene
    string sceneText;       // displays what we've named the scene or if that data is missing
    string frameCountText;  // displays the number of frames in the scene
    string batchCountText;  // displays the number of batches in the frame

    //text parameters
    bool openSingleTextWindowEnabled;   // a bool used to determine if we want to open a window for every text or just one
    Text targetText;                    // the text we target if we only want to open one window
    int targetTextIndex = -1;           // the index for what text we want to target for opening windows and deleting. Default -1 to avoid accidental deletions
    int targetBatchIndex = -2;          // the index for what batch we want to target for GoToBatch and delete. Default -2 as -1 is a special value

    // frame parameters
    Sprite imageSprite;             // the sprite for the background image
    Color imageColor = Color.white; // the color of the background image
    int targetFrameIndex = -2;      // the index for what frame we want to target for GoToFrame and delete. Default -2 as -1 is a special value

    Image targetImage;              // the background image

    // the CutsceneCreator that this Inspector is for
    public CutsceneCreator Current
    {
        get
        {
            return (CutsceneCreator)target;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        targetImage = Current.frameImage;   // set the background image to the one in the CutsceneCreator

        EditorGUILayout.Space();
        // display useful information about the current scene
        GUILayout.Label("SCENE PARAMETERS:", EditorStyles.boldLabel);

        sceneText = sceneName.Equals("") ? "Scene Name: MISSING NAME" : "Scene Name: " + sceneName;
        frameCountText = Current.GetFrameCount().Equals("0") ? "There are no frames!" : "Looking at Frame: " + (Current.frameStep + 1) + "/" + Current.GetFrameCount();
        batchCountText = Current.GetBatchCount().Equals("0") ? "There are no batches!" : "Looking at Batch: " + (Current.batchStep + 1) + "/" + Current.GetBatchCount();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(sceneText);
        GUILayout.Label("Total Frame Count: " + Current.frameCount);    // display the total number of frames 
        GUILayout.Label("Total Texts in Batch: " + Current.textCount);  // display the total number of text objects in the editor
        EditorGUILayout.EndHorizontal();

        // display the frameCountText and the batchCountText
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(frameCountText);
        GUILayout.Label(batchCountText);
        EditorGUILayout.EndHorizontal();

        // Area for setting frame data
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("FRAME SETUP", EditorStyles.boldLabel);
        GUILayout.Label("Frame Number: " + Current.frameCount, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // let the user set a sprite from Assets
        EditorGUI.BeginChangeCheck();
        imageSprite = (Sprite)EditorGUILayout.ObjectField("Image Sprite: ", imageSprite, typeof(Sprite), false);
        if(EditorGUI.EndChangeCheck())
        {
            targetImage.sprite = imageSprite;   // update the active background image if this is updated
        }

        // let the user set the color for the background image
        EditorGUI.BeginChangeCheck();
        imageColor = EditorGUILayout.ColorField("Image Color: ", imageColor);
        if (EditorGUI.EndChangeCheck())
        {
            targetImage.color = imageColor;     // update the active background image's color if this is updated
        }

        // set up the fadeInSpeed and fadeOutSpeed
        Current.fadeInSpeed = EditorGUILayout.FloatField("FadeIn Speed: ", Current.fadeInSpeed);
        Current.fadeOutSpeed = EditorGUILayout.FloatField("FadeOut Speed: ", Current.fadeOutSpeed);

        // check for negative values. If a negative value is placed, then default back to 0.5f
        if (Current.fadeInSpeed <= 0)
        {
            Debug.Log("FadeIn Speed must be greater than 0");
            Current.fadeInSpeed = 0.5f;
        }

        if (Current.fadeOutSpeed <= 0)
        {
            Debug.Log("FadeOut Speed must be greater than 0");
            Current.fadeOutSpeed = 0.5f;
        }

        // Navigation for frames: New, Save, Back, Next, Go to, Delete
        EditorGUILayout.Space();
        GUILayout.Label("NAVIGATION");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Frame"))
        {
            Current.NewFrame();
        }
        if (GUILayout.Button("Save Frame"))
        {
            Current.SaveFrame();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Back Frame"))
        {
            Current.BackFrame();
        }
        if (GUILayout.Button("Next Frame"))
        {
            Current.NextFrame();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Go To and Delete require an index
        targetFrameIndex = EditorGUILayout.IntField("Target Frame: ", targetFrameIndex);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Go to Frame"))
        {
            // make sure the value is positive
            if (targetFrameIndex < 0)
            {
                Debug.LogWarning("Value must be greater than 0. Cannot go to negative frame.");
            }
            else
            {
                Current.GoToFrame(targetFrameIndex);
            }

            targetFrameIndex = -2;  // restore back to default value of -2 when we go to a target frame
        }
        if(GUILayout.Button("Delete Frame"))
        {
            Current.DeleteFrame(targetFrameIndex);
            targetFrameIndex = -2;  // restore back to default value of -2 when we delete a frame to avoid accidents
        }
        EditorGUILayout.EndHorizontal();

        // Are for changing texts and their data
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("TEXT SETUP", EditorStyles.boldLabel);
        GUILayout.Label("Total Texts in Batch: " + Current.textCount, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // check if the user only wants to open the window of a specific text or all of the active texts instead
        openSingleTextWindowEnabled = EditorGUILayout.Toggle("Open Single Text Window", openSingleTextWindowEnabled);
        // the text we want to target
        targetTextIndex = EditorGUILayout.IntField("Target text: ", targetTextIndex);

        // Add and delete text
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Text"))
        {
            Current.AddText();
        }
        if (GUILayout.Button("Delete Text"))
        {
            Current.DeleteText(targetTextIndex);
        }
        EditorGUILayout.EndHorizontal();

        // open text editor windows
        if (GUILayout.Button("Open Text Editor"))
        {
            // make sure there are texts in the first place
            if (Current.allActiveTexts.Count == 0)
            {
                Debug.LogWarning("There are no active texts. Please add texts.");
            }
            else
            {
                // check if we want to open a single window or move
                if (openSingleTextWindowEnabled)
                {
                    // negative values are invalid
                    if (targetTextIndex < 0)
                    {
                        Debug.LogError("Invalid index. Please make sure the index is greater than 1.");
                    }
                    // make sure we're not beyond the max number of active texts
                    else if (targetTextIndex >= Current.allActiveTexts.Count)
                    {
                        int max = Current.allActiveTexts.Count - 1;
                        Debug.LogError("Invalid index. There are not that many texts. Max index can be " + max);
                    }
                    // open a text window targeting that specific text
                    else
                    {
                        targetText = Current.allActiveTexts[targetTextIndex];
                        CutsceneCreatorTextEditorWindow cctew = CreateInstance<CutsceneCreatorTextEditorWindow>();
                        cctew.Init(targetText, Current);
                    }
                }
                // if we're not targeting a specific text, then open a window for each text
                else
                {
                    for (int i = 0; i < Current.allActiveTexts.Count; i++)
                    {
                        CutsceneCreatorTextEditorWindow cctew = CreateInstance<CutsceneCreatorTextEditorWindow>();
                        cctew.Init(Current.allActiveTexts[i], Current);
                    }
                }
            }
        }

        // open a window to set text defaults
        if (GUILayout.Button("Set Text Defaults"))
        {
            DefaultTextOptionsWindow dtow = CreateInstance<DefaultTextOptionsWindow>();
            dtow.Init(Current);
        }

        // Navigation for Text Batches: New, Save, Back, Next, Go to, Delete
        EditorGUILayout.Space();
        GUILayout.Label("NAVIGATION");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Batch"))
        {
            Current.NewBatch();
        }
        if (GUILayout.Button("Save Batch"))
        {
            Current.SaveBatch();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Back Batch"))
        {
            Current.BackBatch();
        }
        if (GUILayout.Button("Next Batch"))
        {
            Current.NextBatch();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Go to and Delete require targets
        targetBatchIndex = EditorGUILayout.IntField("Target Batch: ", targetBatchIndex);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Go to Batch"))
        {
            // negative indexes are invalid
            if (targetBatchIndex < 0)
            {
                Debug.LogWarning("Value must be greater than " + targetBatchIndex + ". Cannot go to negative batch.");
            }
            else
            {
                Current.GoToBatch(targetBatchIndex);
            }

            targetBatchIndex = -2;  // restore back to default value of -2 when we go to a target batch
        }
        if (GUILayout.Button("Delete Batch"))
        {
            Current.DeleteBatch(targetBatchIndex);
            targetBatchIndex = -2;  // restore back to default value of -2 when we delete a batch to avoid accidents
        }
        EditorGUILayout.EndHorizontal();

        // Are for saving cutscenes, loading cutscenes, and clearing the editor
        EditorGUILayout.Space();
        GUILayout.Label("SAVE, LOAD, AND CLEAR", EditorStyles.boldLabel);
        sceneName = EditorGUILayout.TextField("Cutscene Name: ", sceneName);    // give a field for the user to set the scene's name
        // Save and Load require there be a scene name
        if (GUILayout.Button("Save"))
        {
            if (sceneName == "")
                Debug.LogWarning("Requires SCENE NAME before you can SAVE");
            else
                Current.Save(sceneName);
        }

        if (GUILayout.Button("Load"))
        {
            if (sceneName == "")
                Debug.LogWarning("Requires SCENE NAME before you can LOAD");
            else
                Current.Load(sceneName);
        }

        // reset all editor parameters
        if (GUILayout.Button("Clear"))
        {
            Current.Clear();
            imageSprite = null;
            imageColor = Color.white;
            sceneName = "";
            targetText = null;
            targetTextIndex = -1;
            targetBatchIndex = -2;
            targetFrameIndex = -2;
        }
    }

    void OnSceneGUI()
    {
        // Write helper handle for the user
        // Display the name of the scene, what frame/total frames they are looking at, and what text batch/total text bacthes they are looking at
        Handles.BeginGUI();

        GUILayout.Box("CUTSCENE EDIT MODE:\n" + sceneText + "\n" + frameCountText + "\n" + batchCountText);

        Handles.EndGUI();
    }
}