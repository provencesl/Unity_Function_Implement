using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class TextUI : MonoBehaviour {

    private Text text;

	// Use this for initialization
	void Start () {

        text = GetComponent<Text>();

        EventCenter.AddListener(EventType.ShowText, Show);
        EventCenter.AddListener(EventType.HideText, Hide);

        this.gameObject.SetActive(false);
	}

    private void Show(object data)
    {
        string str = data as string;
        text.text = str;
        this.gameObject.SetActive(true);

    }

    private void Hide(object data)
    {
        this.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener(EventType.ShowText,Show);
        EventCenter.RemoveListener(EventType.HideText,Hide);
    }



}
