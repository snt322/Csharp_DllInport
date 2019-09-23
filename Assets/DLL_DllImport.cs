using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.InteropServices;

using System.Reflection;


/*
 * DLLを実行中リンクする
 * 
 */



public class DLL_DllImport : MonoBehaviour
{
    [DllImport("Dll_loop_DllImport")]
    private static extern int loop1();

    [DllImport("Dll_loop_DllImport")]
    private static extern long loop2();



    // Use this for initialization
    void Start()
    {
        Loop_1D_Array();

        Loop_2D_Array();
    }

    void Loop_1D_Array()
    {
        var startTime = Time.realtimeSinceStartup;
        var val = loop1();
        var interval = Time.realtimeSinceStartup - startTime;

        string oStr = string.Format("1次元配列のループ時間 : DllImport : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
        Debug.Log(oStr);
    }

    void Loop_2D_Array()
    {
        var startTime = Time.realtimeSinceStartup;
        var val = loop2();
        var interval = Time.realtimeSinceStartup - startTime;

        string oStr = string.Format("2次元配列のループ時間 : DllImport : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
        Debug.Log(oStr);
    }

}
