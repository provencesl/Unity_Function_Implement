using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/* A simple cutscene manager for Unity 2D. It allows a user to add text to a scene, change that text, and display various images in a cutscene that only transitions with fades. 
 * A cutscene is made up of frames. A frame is defined by when a background image changes. So even if there are multiple texts that will display, they will all count under the same frame.
 * Text is created in batches. A batch is whenever more than one text field will display on the screen simultaneously. As in, if you want some text to display in one position, but other
 * text to display in another position without deleting the previous text.
 * 
 * Cutscenes should be stored in a folder in the StreamingAssets directory called "Cutscenes" and as json files.
 * Cutscenes can be created from the Unity Editor with the CutsceneCreator
 * 
 * These cutscenes are made in the Unity UI and not as gameObjects. They are affected by screen sizes and resolutions. In addition, when creating the CutsceneManager, always put its on object as a
 * child of the Canvas. Never put it directly on the canvas as it will at some point, destroy children objects and if there are any other children of your Canvas, they will be destroyed.
 * 
 * Created by Steven Shing, 12/2018
 * OpenSource
 */

[RequireComponent(typeof(Image))]   // the object this is on must have an Image component
public class CutsceneManager : MonoBehaviour {
    public Text clickText;                          // the helper text for letting you know you can click. THIS IS ONLY FOR DEBUG AND NOT NEEDED FOR THE ACTUAL CUTSCENE MANAGER
    public string cutsceneFileName = "test";        // name of the cutscene file to open WITHOUT .json attached
    public bool typeText = false;                   // do we want the cutscene to type text or display the text fully with fades
    public float typeDelaySpeed = 0.1f;             // how long do we delay the typing speed - the higher the value, the slower the letters type

    GameObject cutsceneTextHandlerPrefab;           // prefab for texts

    CutsceneFrameData[] allFrames;                  // holds all frames of the cutscene
    Queue<CutsceneTextBatchData> activeTextBatches; // holds all active text batches for the active frame
    Queue<CutsceneTextData> activeTextData;         // holds all text data for the active text batch
    List<CutsceneTextObject> textHandlers;          // holds all of the text handlers
    List<GameObject> textHandlerObjects;            // holds all of the text handlers' gameobjects
    Fading fader;                                   // our fader
    CutsceneTextBatchData currentBatch;

    Image background;                               // the background image
    int frameCount;                                 // counter for what frame we're on
    int batchStep;                                  // counter for stepping through a batch
    float fadeSpeed;                                // the speed at which we'll fade

    bool waiting;                                   // true when waiting for the next text to begin

    // grab the prefab and load json before doing anything else
    void Awake()
    {
        cutsceneTextHandlerPrefab = Resources.Load<GameObject>("Prefabs/CutsceneTextObject");   // find the cutsceneTextObject prefab from Resources/Prefabs
        LoadFromJson(); // load the cutscene data
    }

    void Start() {
        frameCount = 0;     // begin at frame 0
        batchStep = 0;      // begin batch step at 0
        waiting = false;    // we are not waiting by default

        // grab the image component for the background and set up the lists
        background = GetComponent<Image>();
        textHandlerObjects = new List<GameObject>();
        textHandlers = new List<CutsceneTextObject>();
        activeTextData = new Queue<CutsceneTextData>();

        // grab the Fading component and load a texture called "Black Background"
        fader = gameObject.AddComponent<Fading>();
        fader.LoadTexture("Black Background");

        SetUpCurrentFrame();    // set up the current frame (in this case, 0)
    }

    // check if the user wants to auto complete text or start the next text
    void Update()
    {
        // first make sure there are texts and also that our batchStep is above 0 to avoid trying to access a negative index
        // check for enter button or left mouse click
        if (textHandlers.Count > 0 && batchStep > 0 && (Input.GetKeyUp(KeyCode.Return) || Input.GetMouseButtonUp(0)))
        {
            // if we're waiting for a new text to start
            if (waiting)
            {
                // stop waiting and go to the next text
                StopAllCoroutines();
                waiting = false;
                NextText();
            }
            // otherwise we're typing
            else
            {
                // complete the text
                textHandlers[batchStep - 1].DisplayWholeSentence();
            }
            Debug.Log("Advancing");
        }

        if (Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0))
            ChangeTextOnClick(true);
        else
            ChangeTextOnClick(false);
    }

    // Sets up a single frame
    void SetUpCurrentFrame()
    {
        // make sure there is no invalid frame data
        string check = ExceptionChecker();
        bool invalid = check.Equals("") ? false : true; // if the check returns an empty string, then there are no errors in the json

        if (invalid)
        {
            Debug.Log("Invalid data found in json file.");
            Debug.Log(check);
            return;
        }

        // empty the strings for any texts currently in the scene
        foreach (CutsceneTextObject cto in textHandlers)
        {
            cto.SetTextToBlank();
        }

        // set the sprite and its color
        background.sprite = Resources.Load<Sprite>("CutsceneImages/" + allFrames[frameCount].imageName);
        background.color = new Color(allFrames[frameCount].imageColor[0], allFrames[frameCount].imageColor[1], allFrames[frameCount].imageColor[2], allFrames[frameCount].imageColor[3]);

        batchStep = 0;  // reset batchStep to 0

        // reset batch and text lists
        activeTextBatches = new Queue<CutsceneTextBatchData>();
        activeTextData = new Queue<CutsceneTextData>();

        // add in all batches in the frame to the activeTextBatch queue
        foreach (CutsceneTextBatchData ctbd in allFrames[frameCount].cutsceneTextBatchDatas)
        {
            activeTextBatches.Enqueue(ctbd);
        }

        currentBatch = activeTextBatches.Dequeue(); // set the current batch we will view

        // set up the activeTextData queue that we'll read sentences off of from the current batch
        for(int i = 0; i < currentBatch.cutsceneTextDatas.Length; i++)
        {
            activeTextData.Enqueue(currentBatch.cutsceneTextDatas[i]);
        }

        fadeSpeed = allFrames[frameCount].fadeSpeeds[0];    // set fade in speed for the frame

        fader.BeginFade(-1, fadeSpeed);     // fade in (hence the -1) at the given speed in the data
        StartCoroutine(WaitFade(true, 0));  // wait for the fade in to finish
    }

    // Begin writing the next text available in the cutscene
    void NextText()
    {
        // if there are no more batches in the frame, then go to the next one
        if (activeTextBatches.Count == 0 && activeTextData.Count == 0)
        {
            Debug.Log("Frame is done");
            fadeSpeed = allFrames[frameCount].fadeSpeeds[1];
            fader.BeginFade(1, fadeSpeed);          // fade out at fade out speed
            StartCoroutine(WaitFade(false, 1));     // wait for the fade out to finish
            return;                                 // escape this function for WaitFade will call what we need next (NextFrame)
        }

        // if there are no more texts in this batch, then go to the next batch
        if(activeTextData.Count == 0)
        {
            currentBatch = activeTextBatches.Dequeue();

            for (int i = 0; i < currentBatch.cutsceneTextDatas.Length; i++)
            {
                activeTextData.Enqueue(currentBatch.cutsceneTextDatas[i]);
            }

            // reset all text to "" when starting a new batch
            foreach(CutsceneTextObject cto in textHandlers)
            {
                cto.SetTextToBlank();
            }

            batchStep = 0;  // reset batchStep to 0
        }

        // create enough text handler gameobjects for the size of our text batch
        if (textHandlerObjects.Count < activeTextData.Count)
        {
            int handlerObjectCount = textHandlerObjects.Count;  // use a variable as the loop will change textHandlerObjects.Count while looping and we don't want that to mess it up

            for (int i = 0; i < activeTextData.Count - handlerObjectCount; i++)
            {
                GameObject textGO = Instantiate(cutsceneTextHandlerPrefab, transform);

                textHandlerObjects.Add(textGO);
                textHandlers.Add(textGO.GetComponent<CutsceneTextObject>());
            }
        }

        // use the text handler based on how far we are in the batch
        textHandlers[batchStep].Init(activeTextData.Dequeue(), typeDelaySpeed);

        // step to the next index in the batch
        batchStep++;
    }

    // Go to the next frame in the cutscene
    void NextFrame()
    {
        frameCount++;   // iterate the frame count
        // if this is the last frame, then the scene is done
        if(frameCount >= allFrames.Length)
        {
            fader.BeginFade(1, fadeSpeed);      // fade out at the given fade out speed
            StartCoroutine(WaitFade(false, 2)); // wait for the fade to finish, then close the scene

            Debug.Log("Scene is done");

            return; // escape the function as we're done
        }

        SetUpCurrentFrame();    // set up the next frame now that we've iterated frameCount
    }

    // used by a CutsceneTextObject to let the manager know that it has completed writing out a text
    // @holdTime is used to determine how long the manager should wait before displaying the next text
    public void TextComplete(float holdTime)
    {
        StartCoroutine(DelayText(holdTime));    // start the coroutine to delay the appearance of the next text
    }

    // coroutine to delay dialogue advancing
    // @timeInSeconds is the time in seconds that we want to wait for
    IEnumerator DelayText(float timeInSeconds)
    {
        waiting = true; // we're now waiting for the next text to display

        yield return new WaitForSeconds(timeInSeconds); // wait for time in seconds

        waiting = false;    // we're not longer waiting
        NextText();         // start the next text
    }

    // 0 = fade in, 1 = fade out, 2 = fade out and end scene
    IEnumerator WaitFade(bool fadeIn, int info)
    {
        // simply wait for the fade to be done
        while (!fader.FadeDone(fadeIn))
        {
            yield return null;
        }

        // fade in
        if (info == 0)
            NextText(); // get the next text when done fading in
        // fade out
        else if (info == 1)
            NextFrame();    // start the next frame when fading out
        // fade out and end scene
        else
            Reset();    // reset everything when the scene is finished
    }

    // reset values
    void Reset()
    {
        // clear all lists
        textHandlers.Clear();
        textHandlerObjects.Clear();
        activeTextData.Clear();

        // destroy all text gameObjects
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        frameCount = 0; // reset the frameCount to 0
    }

    // Load a cutscene data from a json file
    public void LoadFromJson()
    {
        string extension = string.Concat("Cutscenes/", cutsceneFileName, ".json"); // append the folder directory name and .json to our cutscene file as they are all json files

        // Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.streamingAssetsPath, extension);

        if (File.Exists(filePath))
        {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);
            // Pass the json to JsonUtility, and tell it to create a GameData object from it
            CutsceneData loadedData = JsonUtility.FromJson<CutsceneData>(dataAsJson);

            // Retrieve the allRoundData property of loadedData
            allFrames = loadedData.allCutsceneFrameData;
        }
        else
        {
            Debug.LogError("Cannot load cutscene data!");
        }
    }

    // check for exceptions in the json
    private string ExceptionChecker()
    {
        string toReturn = "";

        // ensure that the image color has 4 values (r, g, b, a)
        if (allFrames[frameCount].imageColor.Length != 4)
        {
            toReturn += "Invalid image color. Requires 4 float values: (r, g, b, a).";
        }

        // ensure the fadeSpeeds has 2 values (fade in and fade out)
        if(allFrames[frameCount].fadeSpeeds.Length != 2)
        {
            toReturn += "\nInvalid fadeSpeeds. Requires 2 float values: fadeIn speed and fadeOut speed.";
        }

        CutsceneTextBatchData[] tempBatches = allFrames[frameCount].cutsceneTextBatchDatas;

        // iterate through every single cutsceneTextData in every batch in the current frame
        for (int i = 0; i < tempBatches.Length; i++)
        {
            for (int j = 0; j < tempBatches[i].cutsceneTextDatas.Length; j++)
            {
                // make sure the position for the rect transform has 3 values (x, y, z)
                if (tempBatches[i].cutsceneTextDatas[j].position.Length != 3)
                {
                    toReturn += "\nInvalid text position in item #" + j + ". Requires 3 float values: (x, y, z).";
                }

                // make sure the sizeDelta for the rect transform has 2 values (x, y)
                if (tempBatches[i].cutsceneTextDatas[j].sizeDelta.Length != 2)
                {
                    toReturn += "\nInvalid text sizeDelta in item #" + j + ". Requires 2 float values: (width, height).";
                }

                // make sure the color for the font has 4 values (r, g, b, a)
                if (tempBatches[i].cutsceneTextDatas[j].fontColor.Length != 4)
                {
                    toReturn += "\nInvalid text fontColor in item #" + j + ". Requires 4 float values: (r, g, b, a).";
                }
            }
        }

        return toReturn;
    }

    // ONLY FOR DEBUG PURPOSES
    void ChangeTextOnClick(bool onClick)
    {
        if (onClick)
        {
            clickText.text = "Clicking to Advance";
            clickText.color = Color.red;
        }
        else
        {
            clickText.text = "Press Enter or Left Mouse Button to Advance";
            clickText.color = new Color(1, 0.92156863f, 0, 1);
        }
    }
}
