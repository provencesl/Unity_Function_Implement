using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventTest : MonoBehaviour {


    public void OnShowBtnClick()
    {
        EventCenter.BroadCast(EventType.ShowText,"Hello");
    }

    public void OnHideBtnClick()
    {
        EventCenter.BroadCast(EventType.ShowText,null);
    }



}
