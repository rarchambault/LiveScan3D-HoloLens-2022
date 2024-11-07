using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class StringEvent : UnityEvent<string> { }

public class KeyboardInput : MonoBehaviour {
    public StringEvent keyboardDone;
    public string titleText;
    TouchScreenKeyboard keyboard;

    void Start ()
    {
#if WINDOWS_UWP
        //keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, titleText);
        Debug.Log("Invoking KeyboradDone event");
        keyboardDone.Invoke("10.122.14.47");
#else
        //Just for testing in the editor
        if (keyboardDone != null)
            //keyboardDone.Invoke("127.0.0.1");
            keyboardDone.Invoke("10.122.14.47");
#endif
    }
	
	void Update ()
    {
        //if (TouchScreenKeyboard.visible == false && keyboard != null)
        //{
        //    if (keyboard.done == true)
        //    {
        //        keyboardDone.Invoke(keyboard.text);
        //        keyboard = null;
        //    }
        //}
    }
}
