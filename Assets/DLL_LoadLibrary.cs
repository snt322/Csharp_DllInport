using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.InteropServices;           //DllImport属性の名前空間


/// <summary>
/// 動的リンク (実行時リンク)
/// LoadLibrary()を使用
/// 
/// 実行時リンク、起動時リンクについての参考URL
/// https://tekk.hatenadiary.org/entry/20091027/1256655409
/// </summary>


/*
 * Unity C#でDllをDllImportAttribute属性でロードするとロードしたDllはUnityEditorを終了するまでアンロードされないので以下の不都合がある
 * DllをUnityEditorでロードして動作テストする。その後、Dllを修正して新しいDllに置き換えようとするとEditor上でDllをロードされたままなので置き換えができない。
 * UnityEditorを終了させないとDllを置き換えできない。 
 * 
 * 上記の不都合を回避するために
 * kernel32.dllのLoadLibrary()、FreeLibrary()、GetProcAddress()を使用する。
 * 
 * LoadLibrary(HMODULE hModule, LPCSTR lpProcName) : https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getprocaddress
 * Bool FreeLibrary(HMODULE hLibModule) : https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-freelibrary
 * FARPROC GetProcAddress(HMODULE hModule, LPCSTR lpProcName) : https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getprocaddress
 * 
 * Qiita参考URL https://qiita.com/tan-y/items/9cf3d233df1a379802b2
 */

/*
 * LoadLibrary("Dll名")の使用上のセキュリティ上の注意点
 * LoadLibrary("Dll名")の検索動作の挙動のスキを突かれて予期しないDllをロードする危険がある。
 * Dll名はフルパスで指定する。
 * 
 * 参考URL https://www.jpcert.or.jp/sc-magazine/codezine02-9.html
 */

/*
 * [呼び出し規約]
 * C#側の標準呼び出し規約はWinapi(=StdCall)
 * VC++のdllの呼び出し規約は標準で__stdcallで、windows10のC#と一致しているので特に注意が必要ない場合がある。
 * ただし、C#側のWinapiはOSに依存するので注意が必要。
 * 
 * 呼び出し規約とは関数のスタック(引数、戻り値)の後始末を呼び出す側(.exe側)または呼び出される側(.dll側)のどちらが行うかの手順のこと
 * stack overflow 参考URL https://ja.stackoverflow.com/questions/41904/dll%E3%82%92%E5%A4%96%E9%83%A8%E3%81%8B%E3%82%89%E5%91%BC%E3%81%B3%E5%87%BA%E3%81%97%E5%8F%AF%E8%83%BD%E3%81%AA%E3%82%88%E3%81%86%E3%81%AB%E6%A7%8B%E6%88%90%E3%81%97%E3%81%9F%E3%81%84
 */

/*
 * C++、C++/CLI、C#のプリミティブ型の比較表
 * 参考URL https://so-zou.jp/software/tech/programming/language-comparison/grammar/type/
 */

/*
 * LoadLibrary()、FreeLibrary()、GetProcAddress()に失敗した際にエラー内容を取得する方法
 * 
 * System.Runtime.InteropServices.Marshal.GetLastWin32Error()を使用する
 * GetLastError()はDllImportでの使用を非推奨
 * 
 * 参照URL https://blogs.msdn.microsoft.com/adam_nathan/2003/04/25/getlasterror-and-managed-code/
 * Win32 APIのWin32エラー・コードを取得するには？［C#、VB］ https://www.atmarkit.co.jp/fdotnet/dotnettips/740win32errcode/win32errcode.html
 */

/*
 * windowsApiの代表的なdllについて
 * kernel32.dllはメモリ、Dllの操作など
 * user32.dllはウインドウ操作やメッセージ、入力処理など
 * gdi32.dll
 * shell32.dll 実行パスや特殊パス、UI表示など
 * , etc.
 * 参考URL http://eternalwindows.jp/windevelop/dll/dll04.html
 */

/*
 * DllImport属性 CharSetについて
 * CharSetは文字列のマーシャリングを制御し、Dllの関数名をプラットフォーム呼び出しが見つける仕組みのこと
 *
 * ※CharSetの設定で関数名の検索挙動が変わる。
 * 
 * 例としてMessageBox()をロードする。
 * MessageBox()は内部でAnsi用のMessageBoxA()とUnicode用のMessageBoxW()があり、どちらをロードするかCharSetで指定する。
 * ①CharSet = Ansiの場合
 *          MessageBox()を検索、見つからなければMessageBoxA()を検索
 * ②CharSet = Unicodeの場合
 *          MessageBoxW()を検索、見つからなければMessageBox()を検索
 * 
 * MSDN https://docs.microsoft.com/ja-jp/dotnet/framework/interop/specifying-a-character-set
 */

/*
 * エラー・コード(HRESULT型)から可読メッセージに変換する方法
 * 例)
 * [変換前]      [変換後]
 * 0x00000002 ⇒ 指定されたファイルが見つかりません。
 * Win32エラー・コード一覧 http://ir9.jp/prog/ayu/win32err.htm
 * 
 * 
 * [DllImport("kernel32")]
 * private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, System.Text.StringBuilder lpBuffer, int nSize, IntPtr Arguments);
 *
 * Win32エラー・コードからエラー・メッセージを取得するには？［C#、VB］ https://www.atmarkit.co.jp/fdotnet/dotnettips/741win32errmsg/win32errmsg.html
*/

public class DLL_LoadLibrary : MonoBehaviour
{
    /// <summary>
    /// LoadLibrary()のロード
    /// </summary>
    /// <param name="lpFileName">ライブラリ名</param>
    /// <returns>ライブラリのアドレス</returns>
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, ExactSpelling = false)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    /// <summary>
    /// FreeLibrary()のロード
    /// </summary>
    /// <param name="hModule">ライブラリのアドレス</param>
    /// <returns></returns>
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, ExactSpelling = false)]
    private static extern bool FreeLibrary(IntPtr hModule);

    /// <summary>
    /// GetProcAddress()のロード
    /// </summary>
    /// <param name="hModule"></param>
    /// <param name="lpProcName"></param>
    /// <returns></returns>
    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dwFlags"></param>
    /// <param name="lpSource"></param>
    /// <param name="dwMessageId"></param>
    /// <param name="dwLanguageId"></param>
    /// <param name="lpBuffer"></param>
    /// <param name="nSize"></param>
    /// <param name="Arguments"></param>
    /// <returns></returns>
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern uint FormatMessage(
        uint dwFlags,
        IntPtr lpSource,
        uint dwMessageId,
        uint dwLanguageId,
        System.Text.StringBuilder lpBuffer,
        int nSize, IntPtr Arguments
        );

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    delegate long MyDelegate();



    // Use this for initialization
    void Start()
    {
        Loop_1D_Array();

        Loop_1D_Array_with_MyFunc();

        Loop_2D_Array();

        Loop_2D_Array_with_MyFunc();

        Loop_1D_Array_DoublePrecision_with_MyFunc(100,true, 1000000.0);

        StartCoroutine(Proc());
    }

    /// <summary>
    /// 1次元配列のループ処理を行い、処理時間をコンソールへ出力する
    /// LoadLibrary(),GetProcAddress(),FreeLibrary()を直接使用する
    /// </summary>
    void Loop_1D_Array()
    {
        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop_LoadLibrary.dll";

        IntPtr hModule = LoadLibrary(libName);

        if (hModule != IntPtr.Zero)
        {
            IntPtr procPtr = GetProcAddress(hModule, "loop1");
            if (procPtr != IntPtr.Zero)
            {
                MyDelegate del = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(procPtr, typeof(MyDelegate));
                if (del != null)
                {
                    var startTime = Time.realtimeSinceStartup;
                    var val = del();
                    var interval = Time.realtimeSinceStartup - startTime;

                    string oStr = string.Format("1次元配列のループ時間 : LoadLibraryを直接使用 : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
                    Debug.Log(oStr);
                }
            }
            else
            {
                var eMsg = Marshal.GetLastWin32Error();
                string str = "Failed to GetProcAddress." + "Error No." + eMsg;
                Debug.Log(str);
            }

            FreeLibrary(hModule);                       //必ずアンロードする
        }
        else
        {
            var eMsg = Marshal.GetLastWin32Error();

            var Msg = new System.Text.StringBuilder(255);
            FormatMessage(0x00001000, IntPtr.Zero, (uint)eMsg, 0, Msg, Msg.Capacity, IntPtr.Zero);

            string str = "Failed to LoadLibrary." + "Error(GetLastWin32Error) No." + eMsg + "  :  " + Msg.ToString();

            Debug.Log(str);
        }
    }

    /// <summary>
    /// 1次元配列のループ処理を行い、処理時間をコンソールへ出力する
    /// LoadLibrary(),GetProcAddress(),FreeLibrary()を直接使用する
    /// </summary>
    void Loop_2D_Array()
    {
        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop_LoadLibrary.dll";

        IntPtr hModule = LoadLibrary(libName);

        if (hModule != IntPtr.Zero)
        {
            IntPtr procPtr = GetProcAddress(hModule, "loop2");
            if (procPtr != IntPtr.Zero)
            {
                MyDelegate del = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(procPtr, typeof(MyDelegate));
                if (del != null)
                {
                    var startTime = Time.realtimeSinceStartup;
                    var val = del();
                    var interval = Time.realtimeSinceStartup - startTime;

                    string oStr = string.Format("2次元配列のループ時間 : LoadLibraryを直接使用 : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
                    Debug.Log(oStr);
                }
            }
            else
            {
                var eMsg = Marshal.GetLastWin32Error();
                string str = "Failed to GetProcAddress." + "Error No." + eMsg;
                Debug.Log(str);
            }

            FreeLibrary(hModule);                       //必ずアンロードする
        }
        else
        {
            var eMsg = Marshal.GetLastWin32Error();

            var Msg = new System.Text.StringBuilder(255);
            FormatMessage(0x00001000, IntPtr.Zero, (uint)eMsg, 0, Msg, Msg.Capacity, IntPtr.Zero);

            string str = "Failed to LoadLibrary." + "Error(GetLastWin32Error) No." + eMsg + "  :  " + Msg.ToString();

            Debug.Log(str);
        }
    }



    /// <summary>
    /// 1次元配列のループ処理を行い、処理時間をコンソールへ出力する
    /// 独自メソッドを使用する LoadLib(), FreeLib(), GetProcAdd()
    /// </summary>
    void Loop_1D_Array_with_MyFunc()
    {
        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop_LoadLibrary.dll";

        IntPtr hModule;
        if (LoadLib(libName, out hModule))                           //ライブラリのロード
        {
            string lpProcName = "loop1";
            IntPtr hProcAdd;
            if (GetProcAdd(hModule, lpProcName, out hProcAdd))       //関数のロード
            {
                MyDelegate del = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(hProcAdd, typeof(MyDelegate));

                var startTime = Time.realtimeSinceStartup;
                var val = del();
                var interval = Time.realtimeSinceStartup - startTime;

                string oStr = string.Format("1次元配列のループ時間 : MyFuncを通してLoadLibraryを使用 : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
                Debug.Log(oStr);
            }
            FreeLib(ref hModule);                                   //ライブラリのアンロード
        }
        else
        {
            var eMsg = Marshal.GetLastWin32Error();
            var Msg = new System.Text.StringBuilder(255);
            FormatMessage(0x00001000, IntPtr.Zero, (uint)eMsg, 0, Msg, Msg.Capacity, IntPtr.Zero);

            string str = "Failed to LoadLibrary." + "Error(GetLastWin32Error) No." + eMsg + "  :  " + Msg.ToString();

            Debug.Log(str);
        }
    }

    /// <summary>
    /// 2次元配列のループ処理を行い、処理時間をコンソールへ出力する
    /// 独自メソッドを使用する LoadLib(), FreeLib(), GetProcAdd()
    /// </summary>
    void Loop_2D_Array_with_MyFunc()
    {
        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop_LoadLibrary.dll";

        IntPtr hModule;
        if (LoadLib(libName, out hModule))                              //ライブラリのロード
        {
            string lpProcName = "loop2";
            IntPtr hProcAdd;
            if (GetProcAdd(hModule, lpProcName, out hProcAdd))          //関数のロード
            {
                MyDelegate del = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(hProcAdd, typeof(MyDelegate));

                var startTime = Time.realtimeSinceStartup;
                var val = del();
                var interval = Time.realtimeSinceStartup - startTime;

                string oStr = string.Format("2次元配列のループ時間 : MyFuncを通してLoadLibraryを使用 : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
                Debug.Log(oStr);
            }
            FreeLib(ref hModule);                                       //ライブラリのアンロード
        }
        else
        {
            var eMsg = Marshal.GetLastWin32Error();
            var Msg = new System.Text.StringBuilder(255);
            FormatMessage(0x00001000, IntPtr.Zero, (uint)eMsg, 0, Msg, Msg.Capacity, IntPtr.Zero);

            string str = "Failed to LoadLibrary." + "Error(GetLastWin32Error) No." + eMsg + "  :  " + Msg.ToString();

            Debug.Log(str);
        }
    }

    /// <summary>
    /// LoadLibrary()でDLLをロードする。
    /// </summary>
    /// <param name="lpLibName">Library名</param>
    /// <param name="hModule">Libraryへのポインタ</param>
    /// <returns>ロードに成功したらtrue</returns>
    private bool LoadLib(string lpLibName, out IntPtr hModule)
    {
        hModule = LoadLibrary(lpLibName);

        if (hModule == IntPtr.Zero) return false;

        return true;
    }

    /// <summary>
    /// FreeLibrary()を呼んでDLLをアンロードする
    /// </summary>
    /// <param name="hModule">DLLへのポインタ</param>
    /// <returns>アンロードに成功したらtrue</returns>
    private bool FreeLib(ref IntPtr hModule)
    {
        if (hModule == IntPtr.Zero) return false;
        else
        {
            FreeLibrary(hModule);
            hModule = IntPtr.Zero;
        }
        return true;
    }

    /// <summary>
    /// GetProcAddress()を呼んでアドレスを呼び出す
    /// </summary>
    /// <param name="hModule">Libraryのアドレス</param>
    /// <param name="lpProcName">関数名</param>
    /// <param name="hProcAdd">関数へのアドレス</param>
    /// <returns>関数へのアドレスを取得出来たらtrue</returns>
    private bool GetProcAdd(IntPtr hModule, string lpProcName, out IntPtr hProcAdd)
    {
        if (hModule == IntPtr.Zero)
        {
            hProcAdd = IntPtr.Zero;
            return false;
        }

        hProcAdd = GetProcAddress(hModule, lpProcName);
        if (hProcAdd == IntPtr.Zero)
        {
            return false;
        }

        return true;
    }

    IEnumerator Proc()
    {
        yield return new WaitForSeconds(1f);

        //loop()の処理時間
        {
            Loop_1D_Array();
            Debug.Log("Coroutine Load_1D_Array()");
        }

        //loop2()の処理時間
        {
            Loop_2D_Array();
            Debug.Log("Coroutine Load_2D_Array()");
        }


    }

    /// <summary>
    /// double型1次元配列のループ処理のデリゲート
    /// </summary>
    /// <param name="loopCount">ループ回数</param>
    /// <param name="isDivid">true:割り算 false:掛け算</param>
    /// <param name="value">初期値</param>
    /// <returns>計算結果</returns>
    delegate double MyDelegateDouble(long loopCount, bool isDivid, double value);
    /// <summary>
    /// double型1次元配列のループ処理を行い、処理時間をコンソールへ出力する
    /// </summary>
    /// <param name="loopCount">ループ回数</param>
    /// <param name="isDivid">true:割り算 false:掛け算</param>
    /// <param name="value">初期値</param>
    /// <returns>計算結果</returns>
    void Loop_1D_Array_DoublePrecision_with_MyFunc(long loopCount, bool isDivid, double value)
    {
        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop_DoublePrecision.dll";

        IntPtr hModule;
        if (LoadLib(libName, out hModule))                           //ライブラリのロード
        {
            string lpProcName = "loop_DoublePrecision";
            IntPtr hProcAdd;
            if (GetProcAdd(hModule, lpProcName, out hProcAdd))       //関数のロード
            {
                MyDelegateDouble del = (MyDelegateDouble)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(hProcAdd, typeof(MyDelegateDouble));

                var startTime = Time.realtimeSinceStartup;
                var val = del(loopCount, isDivid, value);
                var interval = Time.realtimeSinceStartup - startTime;

                string oStr = string.Format("1次元double型配列のループ時間 : MyFuncを通してLoadLibraryを使用 : c++ : 結果 {0} : 経過時間 {1}sec", val, interval);
                Debug.Log(oStr);
            }
            FreeLib(ref hModule);                                   //ライブラリのアンロード
        }
        else
        {
            var eMsg = Marshal.GetLastWin32Error();
            var Msg = new System.Text.StringBuilder(255);
            FormatMessage(0x00001000, IntPtr.Zero, (uint)eMsg, 0, Msg, Msg.Capacity, IntPtr.Zero);

            string str = "Failed to LoadLibrary." + "Error(GetLastWin32Error) No." + eMsg + "  :  " + Msg.ToString();

            Debug.Log(str);
        }
    }

}
