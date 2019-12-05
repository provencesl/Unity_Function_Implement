using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/* A simple cutscene creator for Unity 2D. It allows a user to add text to a scene, change that text, and display various images in a cutscene that only transitions with fades. 
 * A cutscene is made up of frames. A frame is defined by when a background image changes. So even if there are multiple texts that will display, they will all count under the same frame.
 * Text is created in batches. A batch is whenever more than one text field will display on the screen simultaneously. As in, if you want some text to display in one position, but other
 * text to display in another position without deleting the previous text.
 * 
 * The CutsceneCreator is used in conjunction with the CutsceneCreatorInspector to create these scenes from the Inspector while in the Unity Editor. Saving will create a folder in the
 * StreamingAssets directory called "Cutscenes" and will save all cutscenes as json files.
 * 
 * Created by Steven Shing, 12/2018
 * OpenSource
 */

// require both a Canvas component and an Image component on this object
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(Image))]
[ExecuteInEditMode] // run the Awake function while in Editor mode
public class CutsceneCreator : MonoBehaviour {
    // we're saving the file to the preproduction folder in scripts as that folder cannot be built into a game
    public const string textDefaultFilePath = "Assets/Scripts/Preproduction/textDefaults.json"; // the file path for the textDefaults

    public GameObject cutsceneTextObjectPrefab;             // prefab for a cutsceneTextObject
    public Image frameImage;                                // the current image for this frame
    [HideInInspector] public CutsceneTextData textDefaults; // the default text options we've set up
    [HideInInspector] public int textCount = 0;             // number of texts active in a batch of texts
    [HideInInspector] public int frameCount = 0;            // total number of frames in this cutscene
    [HideInInspector] public float fadeInSpeed = 0.5f;      // float speed for fading into the current frame
    [HideInInspector] public float fadeOutSpeed = 0.5f;     // float speed for fading out of the current frame

    List<CutsceneFrameData> allFrames = new List<CutsceneFrameData>();                  // list that holds all of our frames
    List<CutsceneTextBatchData> currentBatchList = new List<CutsceneTextBatchData>();   // list that holds all of the text batches for the current frame

    [HideInInspector] public List<CutsceneTextData> currentTextDataList = new List<CutsceneTextData>();         // list that holds all of the CutsceneTextDatas in the active batch
    [HideInInspector] public List<Text> allActiveTexts = new List<Text>();                                      // list that holds all of the Text components currently active
    [HideInInspector] public Dictionary<int, EditorWindow> activeWindows = new Dictionary<int, EditorWindow>(); // dictionary that holds all active windows. Keys are their index. This is a dictionary as each pair is unique

    [HideInInspector] public int batchStep = 0; // used to keep track of what text batch is being looked at in the list
    [HideInInspector] public int frameStep = 0; // used to keep track of what frame is being looked at in the list

    // A public struct used to hold a text and rect transform
    // It exists so we can parse CutsceneTextData into a Text object and a RectTransform
    // It is public because it is used in CutsceneCreatorTextEditorWindow
    public struct TextAndRectTransform
    {
        public Text text;
        public RectTransform rt;
    }

    // grab the text defaults if they exist when CutsceneCreator is awoken
    void Awake()
    {
        if (File.Exists(textDefaultFilePath))
        {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(textDefaultFilePath);
            // Pass the json to JsonUtility, and tell it to create a GameData object from it
            textDefaults = JsonUtility.FromJson<CutsceneTextData>(dataAsJson);
        }
    }

    // Add a text object
    // Optional parameter: CutsceneTextData - used to preload values in to the text object
    public void AddText(CutsceneTextData ctd)
    {
        // instantiate a new text object from our prefab
        GameObject textGO = Instantiate(cutsceneTextObjectPrefab);

        textGO.name = "CutsceneText" + textCount;
        textGO.transform.SetParent(transform, false);

        // grab the rect transform and the text components for editing
        RectTransform rt = textGO.GetComponent<RectTransform>();
        Text text = textGO.GetComponent<Text>();

        // make an object for our struct
        TextAndRectTransform tart = new TextAndRectTransform();

        // set up a new text with default information
        if (ctd == null)
        {
            tart = ParseDataToText(textDefaults, text, rt);
        }
        // if given text, set up the text from that data
        else
        {
            tart = ParseDataToText(ctd, text, rt);
        }

        // set the text and the rect transform we're using
        text = tart.text;
        rt = tart.rt;

        textCount++;    // add to our count of active texts

        allActiveTexts.Add(text);   // add the text to our list
    }

    // overload of AddText where we don't pass in any data
    public void AddText()
    {
        AddText(null);
    }

    // Add a new batch of text
    public void NewBatch()
    {
        // if we don't have any saved batches yet, then let the user know they're already starting a new batch
        if (currentBatchList.Count == 0)
        {
            Debug.LogWarning("Already starting a new batch.");
            return;
        }

        // clear relevant lists
        DestroyAllText();
        currentTextDataList.Clear();
        allActiveTexts.Clear();

        batchStep = currentBatchList.Count; // our batch step is now looking beyond our last index
        textCount = 0;                      // reset text count
    }

    // Add a new frame
    public void NewFrame()
    {
        // if we don't have any saved frames yet, then let the user know they're already starting a new frame
        if (allFrames.Count == 0)
        {
            Debug.LogWarning("Already starting a new frame.");
            return;
        }

        frameCount++;                   // increment the frame count
        frameStep = allFrames.Count;    // we're now looking beyond our last index at a potential new frame
        batchStep = 0;                  // reset the batch step
        textCount = 0;                  // reset the text count

        // clear relevant lists
        DestroyAllText();
        currentBatchList.Clear();
        currentTextDataList.Clear();
        allActiveTexts.Clear();
    }

    // Save a batch for the current frame
    public void SaveBatch()
    {
        // if there are no texts and no text data, then there is nothing to save
        if (allActiveTexts.Count == 0 || currentTextDataList.Count == 0)
        {
            Debug.Log("No texts available. Please add a text.");
            return;
        }

        // create a CutsceneTextBatchData and add the currentTextDataList to its cutsceneTextDatas string[]
        CutsceneTextBatchData ctbd = new CutsceneTextBatchData();
        ctbd.cutsceneTextDatas = currentTextDataList.ToArray();

        // if we're looking at a new batch, then add to the list
        if (batchStep == currentBatchList.Count)
        {
            currentBatchList.Add(ctbd); // add the new data

            // clear relevant lists
            DestroyAllText();
            currentTextDataList.Clear();
            allActiveTexts.Clear();

            batchStep++;    // increment batch as we're not looking at a new batch
            textCount = 0;  // reset text count

            Debug.Log("Batch saved at index " + batchStep + ". Now looking at new batch.");
        }
        // if our batchStep is not equal to the count (beyond the last index), then we want to update data instead of add data
        else
        {
            currentBatchList[batchStep] = ctbd;
            Debug.Log("Batch at index " + batchStep + "replaced.");
        }
    }

    // Save a frame for the current cutscene
    public void SaveFrame()
    {
        frameImage = GetComponent<Image>(); // set frameImage to the Image component
        
        // check if the sprite is null - if it is, then we cannot save the data
        if (frameImage.sprite == null)
        {
            Debug.LogError("Image sprite has not been set.");
            return;
        }

        // save the frame data - imageName, imageColor, fadeSpeeds, and cutsceneTextBatchDatas
        CutsceneFrameData cfd = new CutsceneFrameData();
        cfd.imageName = frameImage.sprite.name;
        cfd.imageColor = new float[] { frameImage.color.r, frameImage.color.g, frameImage.color.b, frameImage.color.a };
        cfd.fadeSpeeds = new float[] { fadeInSpeed, fadeOutSpeed };

        CutsceneTextBatchData[] temp = currentBatchList.ToArray();
        cfd.cutsceneTextBatchDatas = temp;

        // if we're looking beyond the last frame (making a new frame), then add to the list
        if (frameStep == allFrames.Count)
        {
            allFrames.Add(cfd);
            Debug.Log("Frame saved at index " + frameStep + ". Now looking at new frame.");
            NewFrame(); // after we add, start a new frame
        }
        // otherwise we want to replace frame data
        else
        {
            allFrames[frameStep] = cfd;
            Debug.Log("Frame at index " + frameStep + "replaced.");
        }
    }

    // Navigate back one frame
    public void BackFrame()
    {
        // if there's no frames, then we can't go back
        if (allFrames.Count == 0)
        {
            Debug.LogWarning("There are no frames!");
            return;
        }

        frameStep--;    // decrement the index

        // if we're looking at the first frame, then we can't go back
        if (frameStep < 0)
        {
            Debug.LogError("Looking at first frame already.");
            frameStep++;    // increment the index to account for decrementing it before
            return;
        }

        GoToFrame(frameStep);   // call GoToFrame with the previous index (which was already decremented)
    }

    // Navigate back one batch of texts
    public void BackBatch()
    {
        // if there's no batches, then we can't go back
        if (currentBatchList.Count == 0)
        {
            Debug.LogWarning("There are no batchs!");
            return;
        }

        batchStep--;    // decrement the index

        // if we're looking at the first batch, then we can't go back
        if (batchStep < 0)
        {
            Debug.LogError("Looking at first batch already.");
            batchStep++;    // increment the index to account for decrementing it before
            return;
        }

        GoToBatch(batchStep);   // call GoToBatch with the previous index (which was already decremented)
    }

    public void NextFrame()
    {
        // if there's no frames, then we can't go forward
        if (allFrames.Count == 0)
        {
            Debug.LogWarning("There are no frames!");
            return;
        }

        frameStep++; // increment the index

        // if we're looking at the last frame, then we can't go forward
        if (frameStep >= allFrames.Count)
        {
            Debug.LogError("Looking at last frame already.");
            frameStep--;    // decrement the index to account for incrementing it before
            return;
        }

        GoToFrame(frameStep);    // call GoToFrame with the next index (which was already incremented)
    }

    public void NextBatch()
    {
        // if there's no batches, then we can't go forward
        if (currentBatchList.Count == 0)
        {
            Debug.LogWarning("There are no batchs!");
            return;
        }

        batchStep++;    // increment the index

        // if we're looking at the last batch, then we can't go forward
        if (batchStep >= currentBatchList.Count)
        {
            Debug.LogError("Looking at last batch already.");
            batchStep--;    // decrement the index to account for incrementing it before
            return;
        }

        GoToBatch(batchStep);   // call GoToBatch with the next index (which was already incremented)
    }

    // Go to the target frame
    public void GoToFrame(int index)
    {
        frameStep = index;  // update the frameStep to match the index

        // get the frame data at that index and set up the parameters to match it
        CutsceneFrameData cfd = allFrames[index];

        frameImage = GetComponent<Image>();

        frameImage.color = new Color(cfd.imageColor[0], cfd.imageColor[1], cfd.imageColor[2], cfd.imageColor[3]);
        frameImage.sprite = Resources.Load<Sprite>("CutsceneImages/" + cfd.imageName);
        fadeInSpeed = cfd.fadeSpeeds[0];
        fadeOutSpeed = cfd.fadeSpeeds[1];

        // update relevant lists
        DestroyAllText();
        currentBatchList.Clear();
        currentTextDataList.Clear();
        allActiveTexts.Clear();

        // set up the batches of the frame
        CutsceneTextBatchData[] temp = cfd.cutsceneTextBatchDatas;
        for (int i = 0; i < temp.Length; i++)
        {
            currentBatchList.Add(temp[i]);
        }

        // check if there are no batches in this frame
        if (currentBatchList.Count == 0)
        {
            Debug.Log("There are no text batches in this frame");
        }
        // but if there are
        else
        {
            // set the batch to the last one in the frame and go to that batch
            batchStep = currentBatchList.Count - 1;

            GoToBatch(batchStep);
        }
    }

    // Go to target batch
    public void GoToBatch(int index)
    {
        batchStep = index;  // update batchStep to match the index

        // update relevant lists
        DestroyAllText();
        currentTextDataList.Clear();
        allActiveTexts.Clear();
        textCount = 0;       

        // get all of the CutsceneTextData in this batch
        CutsceneTextData[] temp = currentBatchList[index].cutsceneTextDatas;

        // add the text datas to the list and also AddText based on their parameters
        for (int i = 0; i < temp.Length; i++)
        {
            currentTextDataList.Add(temp[i]);
            AddText(temp[i]);
        }
    }

    // Delete a target frame or current frame if target is -1
    public void DeleteFrame(int target)
    {
        // a target below -1 is invalid
        if (target < -1)
        {
            Debug.LogWarning("Value must be greater than " + target + ". NOTE: -1 = delete current frame");
            return;
        }
        // check if the target frame is beyond what we have
        else if(target >= allFrames.Count)
        {
            Debug.LogWarning("Value must be less than " + target + " for there are not that many frames. NOTE: -1 = delete current frame");
            return;
        }

        // check if we have frames
        if (allFrames.Count == 0)
        {
            Debug.LogWarning("There are no frames!");
            return;
        }
        // if we only have one frame, remove it, then escape the function
        else if (allFrames.Count == 1)
        {
            Debug.Log("Only existing frame has been deleted");
            allFrames.Clear();
            return;
        }        

        // if the user puts in -1, then delete the current frame if it's a valid frame
        if(target == -1)
        {
            if (frameStep < allFrames.Count)
                target = frameStep;
            else
                Debug.LogWarning("Cannot delete current frame as we are looking beyond the last frame");
        }

        int targetBeforeOrAfter = -2;   // used to look at a remaining frame if possible

        // if our target is the last frame, then go to the previous frame after deleting
        if (target == allFrames.Count - 1)
        {
            targetBeforeOrAfter = target - 1;
            Debug.Log("Frame #" + target + " deleted. Now looking at frame #" + targetBeforeOrAfter);
            allFrames.RemoveAt(target);
            GoToFrame(targetBeforeOrAfter);
        }
        // otherwise always look at the next frame after deleting
        else
        {
            targetBeforeOrAfter = target + 1;
            Debug.Log("Frame #" + target + " deleted. Frame #" + targetBeforeOrAfter + " is now Frame #" + target + ". Looking at Frame #" + target);
            allFrames.RemoveAt(target);
            // since the target is removed, it is replaced by the next one. But it keeps the previous index
            GoToFrame(target);
        }
    }

    // Delete target text
    public void DeleteText(int target)
    {
        // a negative value is invalid to target
        if (target < 0)
        {
            Debug.LogWarning("Value must be greater than " + target + ".");
            return;
        }     
        // if the target is beyond the amount of texts we have, then display a warning
        else if (target >= allActiveTexts.Count)
        {
            Debug.LogWarning("Value must be less than " + target + " for there are not that many texts.");
            return;
        }
        // if there are no texts, then display a warning
        else if(allActiveTexts.Count == 0)
        {
            Debug.LogWarning("There are no texts!");
            return;
        }

        // close any open windows associated with the text
        if(activeWindows.ContainsKey(target))
        {
            activeWindows[target].Close();
        }

        Text temp = allActiveTexts[target]; // acquire the text and save it as a temporary object before we remove it from the list
        allActiveTexts.RemoveAt(target);    // remove the text from the active list

        DestroyImmediate(temp.gameObject);  // destroy the gameObject that held the text

        Debug.Log("Deleted Text #" + target + ".");

        // if there was data associated with the text in the list, then remove it from the data list
        if (currentTextDataList.Count > target && currentTextDataList[target] != null)
        {
            currentTextDataList.RemoveAt(target);
            Debug.Log("Found and deleted text data for text #" + target + "as well.");
        }
    }

    // Delete target batch of texts or current batch if target is -1
    public void DeleteBatch(int target)
    {
        // a target below -1 is invalid
        if (target < -1)
        {
            Debug.LogWarning("Value must be greater than " + target + ". NOTE: -1 = delete current batch");
            return;
        }
        // a target beyond the total number of batchs is invalid
        else if (target >= currentBatchList.Count)
        {
            Debug.LogWarning("Value must be less than " + target + " for there are not that many batches. NOTE: -1 = delete current batch");
            return;
        }

        // we can't delete anything if we have no batches
        if (currentBatchList.Count == 0)
        {
            Debug.LogWarning("There are no frames!");
            return;
        }
        // if we only have one batch, remove it, then escape the function
        else if (currentBatchList.Count == 1)
        {
            Debug.Log("Only existing batch has been deleted");
            currentBatchList.Clear();
            return;
        }

        // if the user puts in -1, then delete the current batch if it's a valid batch
        if (target == -1)
        {
            if (batchStep < currentBatchList.Count)
                target = batchStep;
            else
                Debug.LogWarning("Cannot delete current batch as we are looking beyond the last batch");
        }

        int targetBeforeOrAfter = -2;   // used to look at a remaining batch if possible

        // if our target is the last batch, then go to the previous batch after deleting
        if (target == currentBatchList.Count - 1)
        {
            targetBeforeOrAfter = target - 1;
            Debug.Log("Batch #" + target + " deleted. Now looking at batch #" + targetBeforeOrAfter);
            currentBatchList.RemoveAt(target);
            GoToBatch(targetBeforeOrAfter);
        }
        // otherwise always look at the next batch after deleting
        else
        {
            targetBeforeOrAfter = target + 1;
            Debug.Log("Batch #" + target + " deleted. Batch #" + targetBeforeOrAfter + " is now Batch #" + target + ". Looking at Batch #" + target);
            currentBatchList.RemoveAt(target);
            // since the target is removed, it is replaced by the next one. But it keeps the previous index
            GoToBatch(target);
        }
    }

    // Save the entire cutscene to a json file with @sceneName as its name
    public void Save(string sceneName)
    {
        // save the frame if it wasn't saved
        if (currentBatchList.Count > 0)
        {
            SaveFrame();
        }
        
        // set up the CutsceneData
        CutsceneData cd = new CutsceneData();
        cd.allCutsceneFrameData = allFrames.ToArray();

        CreateSaveDirectory("Cutscenes");   // create the Cutscenes directory if it doesn't exist

        // save the data to json
        string json = JsonUtility.ToJson(cd, true);

        string endOfPath = string.Concat("Cutscenes/", sceneName, ".json");         // append the file directory and file type to the path we want to save it in
        string filePath = Path.Combine(Application.streamingAssetsPath, endOfPath); // we're saving the file to the StreamingAssets folder

        File.WriteAllText(filePath, json);  // write and save the file

        AssetDatabase.Refresh();    // update the asset database
    }
    
    // Load an entire cutscene from a json file give its name with @sceneName
    public void Load(string sceneName)
    {
        string extension = string.Concat("Cutscenes/", sceneName, ".json"); // append the folder directory name and .json to our cutscene file as they are all json files

        // Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.streamingAssetsPath, extension);

        // check to see if the file exists
        if (File.Exists(filePath))
        {
            Clear();    // reset all values in the editor

            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);
            // Pass the json to JsonUtility, and tell it to create a GameData object from it
            CutsceneData loadedData = JsonUtility.FromJson<CutsceneData>(dataAsJson);

            // Retrieve the allFrames data from loadedData - must add the elements of the array to the list
            for(int i = 0; i < loadedData.allCutsceneFrameData.Length; i++)
            {
                allFrames.Add(loadedData.allCutsceneFrameData[i]);
            }

            GoToFrame(allFrames.Count - 1); // look at the last possible frame 

            Debug.Log("Load successful");
        }
        // let the user know the file doesn't exist
        else
        {
            Debug.LogError("There is no cutscene named " + sceneName);
        }
    }


    // Reset the editor
    public void Clear()
    {
        DestroyAllText();

        allFrames.Clear();
        currentTextDataList.Clear();
        currentBatchList.Clear();
        allActiveTexts.Clear();

        textCount = 0;
        frameCount = 0;
        fadeInSpeed = 0.5f;
        fadeOutSpeed = 0.5f;

        batchStep = 0;
        frameStep = 0;

        Debug.Log("Cleared");
    }

    // Called by CutsceneCreatorTextEditorWindow
    // updates text data in our list or adds the data if it did not previously exist
    public void UpdateTextData(int index, CutsceneTextData ctd)
    {
        // check if there is already data at the target index in the list
        if (currentTextDataList.Count > index && currentTextDataList[index] != null)
        {
            currentTextDataList[index] = ctd;
            Debug.Log("Text " + index + "overridden");
        }
        else
        {
            currentTextDataList.Add(ctd);
            Debug.Log("Text " + index + " saved");
        }
    }

    // returns the number of objects in the allFrames list
    public string GetFrameCount()
    {
        return allFrames.Count.ToString();
    }

    // returns the number of objects in the currentBatchList list
    public string GetBatchCount()
    {
        return currentBatchList.Count.ToString();
    }

    // Parses CutsceneTextData into the components for a Text object and a RectTransform, then returns them as a TextAndRectTransform struct object
    public TextAndRectTransform ParseDataToText(CutsceneTextData ctd, Text text, RectTransform rt)
    {
        TextAndRectTransform tart = new TextAndRectTransform();

        text.text = ctd.textToShow; // fill in the text field

        // try to find and set the font
        // check for default font
        if (!ctd.font.Equals("Arial"))
        {
            try
            {
                // search from Assets/Resouces/Fonts/
                text.font = Resources.Load<Font>("Fonts/" + ctd.font);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Could not find font named " + ctd.font + ". Escaping CutsceneTextObject Init.\n" + e.Message);
                return tart;
            }
        }
        else
        {
            text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        }

        text.fontSize = ctd.fontSize;   // set font size

        // attempt to set the text alignment
        try
        {
            text.alignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), ctd.textAnchor);
        }
        catch (System.Exception e)
        {
            Debug.LogError("TextAnchor string to enum parse failed. Cannot parse " + ctd.textAnchor + ". Escaping CutsceneTextObject Init.\n" + e.Message);
            return tart;
        }

        // set the color of the text
        text.color = new Color(ctd.fontColor[0], ctd.fontColor[1], ctd.fontColor[2], ctd.fontColor[3]);

        // set the position of the text and the size of the rectangle
        rt.localPosition = new Vector3(ctd.position[0], ctd.position[1], ctd.position[2]);
        rt.sizeDelta = new Vector2(ctd.sizeDelta[0], ctd.sizeDelta[1]);

        // set the components of the TextAndRectTransform
        tart.text = text;
        tart.rt = rt;

        return tart;
    }

    // creates a save directory of @directoryName
    private void CreateSaveDirectory(string directoryName)
    {
        string filePath = Path.Combine(Application.dataPath, "StreamingAssets");
        // if StreamingAssets doesn't exist, make the folder
        if (!Directory.Exists(filePath))
            AssetDatabase.CreateFolder(Application.dataPath, "StreamingAssets");
        // if Cutscenes doesnt exist in StreamingAssets, make the folder
        if (!Directory.Exists(filePath))
            AssetDatabase.CreateFolder(filePath, directoryName);
        // update our changes
        AssetDatabase.Refresh();
    }

    // Destroy all children objects (texts in this case) of this object
    private void DestroyAllText()
    {
        ClearActiveWindows();

        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        // for some reason, destroying all of the children text is unreliable, so recursively call this function until the cutscene creator no longer has any children
        while (transform.childCount > 0)
        {
            DestroyAllText();
        }
    }

    // Close any open text editor windows
    private void ClearActiveWindows()
    {
        foreach (KeyValuePair<int, EditorWindow> entry in activeWindows)
        {
            entry.Value.Close();
        }

        activeWindows.Clear();
    }
}
