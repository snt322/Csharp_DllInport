using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.InteropServices;           //DllImport属性の名前空間



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

public class Use_DLL : MonoBehaviour
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



    delegate long MyDelegate();


    [DllImport("MPS_CPU")]
    private static extern int test_Add(int a, int b);

    [DllImport("MPS_CPU")]
    private static extern long loop();

    [SerializeField]
    private UnityEngine.UI.Text m_Txt = null;



    // Use this for initialization
    void Start()
    {
        MyTest1();

        StartCoroutine(Proc());
    }


    void MyTest1()
    {

        Debug.Log("現在のディレクトリ：" + System.IO.Directory.GetCurrentDirectory());

        string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
        libName += "\\Dll_loop.dll";

        IntPtr hModule = LoadLibrary(libName);
        //        IntPtr hModule = LoadLibrary("F:\\Visual Studio\\Unity5\\Unity Project\\USE_DLL\\DLL_USE\\Assets\\Dll_loop.dll");

        if (hModule != IntPtr.Zero)
        {


            IntPtr procPtr = GetProcAddress(hModule, "loop2");
            if (procPtr != IntPtr.Zero)
            {
                MyDelegate del = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(procPtr, typeof(MyDelegate));
                if (del != null)
                {
                    var value = del();
                    string str = "MyDelegate called." + value;

                    Debug.Log(str);
                }
            }
            else
            {
                var eMsg = Marshal.GetLastWin32Error();
                string str = "Failed to GetProcAddress." + "Error No." + eMsg;

                m_Txt.text = str;

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


    IEnumerator Proc()
    {
        yield return new WaitForSeconds(1f);

        //loop()の処理時間
        {
            var startTime = Time.realtimeSinceStartup;
            var param = loop();
            var endTime = Time.realtimeSinceStartup - startTime;
            Debug.Log("main c++ : " + param + " : " + endTime + "sec");
        }

        //loop2()の処理時間
        {
            string libName = System.IO.Directory.GetCurrentDirectory() + "\\Assets";
            libName += "\\MPS_CPU_LoadTest.dll";

            IntPtr pLbry = LoadLibrary(libName);
//            IntPtr pLbry = LoadLibrary("F:\\Visual Studio\\Unity5\\Unity Project\\USE_DLL\\DLL_USE\\Assets\\MPS_CPU_LoadTest.dll");
            try
            {

                if (pLbry != IntPtr.Zero)
                {
                    IntPtr add = GetProcAddress(pLbry, "loop2");
                    if (add != IntPtr.Zero)
                    {
                        MyDelegate dlgt = (MyDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(add, typeof(MyDelegate));

                        var startTime = UnityEngine.Time.realtimeSinceStartup;

                        var result = dlgt();

                        var interval = UnityEngine.Time.realtimeSinceStartup - startTime;


                        Debug.Log("二次元配列の処理 : main c++ : " + result + " : " + interval + "sec");
                    }
                    else
                    {
                        throw new Exception("DLL ロードに失敗2");
                    }

                    FreeLibrary(pLbry);                     //必ずアンロードする

                }
                else
                {
                    throw new Exception("DLL ロードに失敗1");
                }
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
            }



        }


    }

}
