using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//
/// <summary>
/// 功能：
/// （1）添加事件监听
/// （2）移除事件监听
/// （3）广播消息
/// 职责：
///     消息中心
/// 
/// 
/// </summary>
public class EventCenter {

    private static Dictionary<EventType, Action<object>> event_dic = new Dictionary<EventType, Action<object>>();

    public static void AddListener(EventType eventType, Action<object> callback)
    {
        if (!event_dic.ContainsKey(eventType))
        {
            event_dic.Add(eventType, null);
        }
        //参数类型为委托的字典的使用方式之一
        //event_dic.Add(eventType,null);
        //event_dic[eventType] += callback
        event_dic[eventType] += callback;


    }


    public static void RemoveListener(EventType eventType, Action<object> callback)
    {
        if (!event_dic.ContainsKey(eventType))
        {
            return;
        }

        if (event_dic[eventType] == null)
        {
            return;
        }

        event_dic[eventType] -= callback;

    }

    //广播
    public static void BroadCast(EventType eventType, object data)
    {
        if (!event_dic.ContainsKey(eventType))
        {
            return;
        }

        Action<object> callback = event_dic[eventType];
        if (callback != null)
        {
            callback(data);
        }



    }






}


//Enum of EventType
public enum EventType
{
    ShowText,
    HideText
}