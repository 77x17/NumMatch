using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetResolutionOnStart()
    {
        int screenW = Display.main.systemWidth;
        int screenH = Display.main.systemHeight;

        float targetRatio = 9f / 16f;

        int finalH = (int)(screenH * 0.9f);
        int finalW = Mathf.RoundToInt(finalH * targetRatio);

        if (finalW > screenW)
        {
            finalW = (int)(screenW * 0.9f);
            finalH = Mathf.RoundToInt(finalW / targetRatio);
        }

        Screen.SetResolution(finalW, finalH, false);
    }
}
