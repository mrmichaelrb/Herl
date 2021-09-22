using UnityEngine;
using UnityEngine.XR;
using System.Runtime.InteropServices;
using System;

namespace Assets
{
  class Initialization
  {
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod()
    {
      if (Application.isEditor)
      {
        bool windowsPlatform = 
        (Application.platform == RuntimePlatform.WindowsPlayer) || 
        (Application.platform == RuntimePlatform.WindowsEditor);

        if ((windowsPlatform) && (XRSettings.enabled))
        {
          const uint MB_YESNO = (uint)0x00000004L;
          const uint MB_ICONQUESTION = (uint)0x00000020L;
          const uint MB_TASKMODAL = (uint)0x00002000L;
          const uint MB_TOPMOST = (uint)0x00040000L;
          const uint MB_SETFOREGROUND = (uint)0x00010000L;

          const int IDNO = 7;

          const int SW_RESTORE = 9;

          uint dialogType = MB_YESNO | MB_ICONQUESTION | MB_TASKMODAL | MB_TOPMOST | MB_SETFOREGROUND;

          string caption = UnityEngine.Application.productName;

          IntPtr activeWindow = GetActiveWindow();

          int result = MessageBox(IntPtr.Zero, "Enable rendering in virtual reality hardware?", caption, dialogType);

          ShowWindow(activeWindow, SW_RESTORE);
          SetForegroundWindow(activeWindow);

          if (result == IDNO)
          {
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;
          }
        }
      }
    }
  }
}
