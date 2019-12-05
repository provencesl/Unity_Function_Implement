using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CutsceneFrameData {
    public string imageName;
    public float[] imageColor;
    public float[] fadeSpeeds;
    public CutsceneTextBatchData[] cutsceneTextBatchDatas;
}
