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



public class Use_DLL_Test : MonoBehaviour
{

    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hLibModule);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    delegate long loopFuncDelegate();
    delegate Int64 loopFuncDim2Delegate();



    // Use this for initialization
    void Start()
    {
        LoadDLL_MPS_CPU();

        LoadDLL_Dll_1();

        LoadDLL_Dll_2();

    }

    // Update is called once per frame
    void Update()
    {

    }

    //Load Dll MPS_CPU
    private void LoadDLL_MPS_CPU()
    {
        IntPtr hModule = LoadLibrary("MPS_CPU.dll");


        if (hModule != IntPtr.Zero)
        {
            IntPtr add = GetProcAddress(hModule, "loop");
            if (add != IntPtr.Zero)
            {
                loopFuncDelegate func = (loopFuncDelegate)Marshal.GetDelegateForFunctionPointer(add, typeof(loopFuncDelegate));

                var startTime = Time.realtimeSinceStartup;
                var param = func();
                var interval = Time.realtimeSinceStartup - startTime;

                string methodName = MethodBase.GetCurrentMethod().Name;

                Debug.Log(methodName + " : main c++ : " + param + " : " + interval + "sec");
            }

            FreeLibrary(hModule);

        }
        else
        {
            Debug.Log("Failed.");
        }
    }


    /// <summary>
    /// Dll内での繰り返し計算の時間を計測する
    /// </summary>
    private void LoadDLL_Dll_1()
    {
        IntPtr hModule = IntPtr.Zero;
        string libName = "F:\\Visual Studio\\Unity5\\Unity Project\\USE_DLL\\DLL_USE\\Assets\\Dll_loop.dll";
//        string libName = "Dll_loop.dll";
        string funcName = "loop1";
        if(loadLib(libName, out hModule))
        {
            IntPtr hAddress = IntPtr.Zero;
            if(loadFuncAdd(funcName, hModule, out hAddress))
            {
                loopFuncDelegate dlgt = (loopFuncDelegate)Marshal.GetDelegateForFunctionPointer(hAddress, typeof(loopFuncDelegate));
                var startTime = Time.realtimeSinceStartup;
                var param = dlgt();
                var interval = Time.realtimeSinceStartup - startTime;

                string methodName = MethodBase.GetCurrentMethod().Name;
                Debug.Log(methodName + " : main c++ : " + param + " : " + interval + "sec");
            }

            FreeLibrary(hModule);

        }
        else
        {
            Debug.Log("failed LoadLibrary.");
        }

    }

    /// <summary>
    /// Dll内での繰り返し計算の時間を計測する
    /// </summary>
    private void LoadDLL_Dll_2()
    {
        IntPtr hModule = IntPtr.Zero;
        string libName = "F:\\Visual Studio\\Unity5\\Unity Project\\USE_DLL\\DLL_USE\\Assets\\Dll_loop.dll";
//        string libName = "Dll_loop.dll";
        string funcName = "loop2";
        if (loadLib(libName, out hModule))
        {
            IntPtr hAddress = IntPtr.Zero;
            if (loadFuncAdd(funcName, hModule, out hAddress))
            {
                loopFuncDim2Delegate dlgt = (loopFuncDim2Delegate)Marshal.GetDelegateForFunctionPointer(hAddress, typeof(loopFuncDim2Delegate));
                var startTime = Time.realtimeSinceStartup;
                var param = dlgt();
                var interval = Time.realtimeSinceStartup - startTime;

                string methodName = MethodBase.GetCurrentMethod().Name;
                Debug.Log(methodName + " : main c++ : " + param + " : " + interval + "sec");
            }

            FreeLibrary(hModule);

        }
        else
        {
            Debug.Log("failed LoadLibrary.");
        }

    }

    private bool loadLib(string lpLibName, out IntPtr hModule)
    {
        hModule = LoadLibrary(lpLibName);

        if (hModule == IntPtr.Zero) return false;

        return true;
    }
    private bool loadFuncAdd(string lpName, IntPtr hModule, out IntPtr hAddress)
    {
        hAddress = GetProcAddress(hModule, lpName);
        if (hAddress == IntPtr.Zero) return false;

        return true;
    }

}
