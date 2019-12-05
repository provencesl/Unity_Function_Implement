using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* A fader that affects the entire screen via OnGUI. It can fade with any texture, so it does not need to be just a color.
 * For fading with colors, however, it must be saved as a texture in a directory Resources/FaderImages
 * 
 * Created by Steven Shing, 12/2018
 * OpenSource
 */

public class Fading : MonoBehaviour {
    public Texture2D fadeOutTexture;    // the texture that will overlay the screen. This can be a black image or a loading graphic
    public float fadeSpeed;             // fading speed

    private int drawDepth = -1000;      // the texture's order in the draw hierarchy; a low number means it renders on top
    private float alpha = 1.0f;         // texture's alpha
    private int fadeDir = -1;           // the direction to fade: in = -1 or out = 1
    
    // load a texture from Assets/Resources/FaderImages/ given a textureName
    public void LoadTexture(string textureName)
    {
        textureName = string.Concat("FaderImages/", textureName);
        fadeOutTexture = Resources.Load<Texture2D>(textureName);
    }

    void OnGUI()
    {
        // fade by changing alpha using a direction, speed, and dt
        alpha += fadeDir * fadeSpeed * Time.deltaTime;
        // clamp the number between 0 and 1 because GUI.color uses alpha between 0 and 1
        alpha = Mathf.Clamp01(alpha);

        // set color of our GUI. All colors stay the same except Alpha is set to our variable
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);            // set alpha value
        GUI.depth = drawDepth;                                                          // make the texture render on top (drawn last)
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeOutTexture);   // draw the texture to fit the entire screen area
    }

    // sets fadeDir to direction parameter to determine whether we fade in if -1 or out if 1
    public void BeginFade(int direction, float speed)
    {
        fadeDir = direction;    // what direction are we fading (-1 is in and 1 is out)
        fadeSpeed = speed;      // speed at which to fade. measured in frames, it comes out to 1/speed frames to fade
    }

    // check if the fade is done
    // check for 1 if we were fadingIn and check for 0 if we were fadingOut
    public bool FadeDone(bool fadeIn)
    {
        return fadeIn ? alpha == 0 : alpha == 1;
    }
}
