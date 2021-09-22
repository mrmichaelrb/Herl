using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

class VirtualReality
{
  public static bool IsOpenVrHmdPresent()
  {
    return ((XRSettings.enabled) && (XRSettings.loadedDeviceName == "OpenVR"));
  }

  public static void CenterHmd()
  {
    InputTracking.Recenter();

    if (IsOpenVrHmdPresent())
    {
      OpenVR.System.ResetSeatedZeroPose();
      OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseSeated);
    }
  }

  public static int NumFramePresents()
  {
    int result = 1;
    if (IsOpenVrHmdPresent())
    {
      Compositor_FrameTiming timing = new Compositor_FrameTiming();
      timing.m_nSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Compositor_FrameTiming));
      if (OpenVR.Compositor.GetFrameTiming(ref timing, 0))
      {
        result = (int)timing.m_nNumFramePresents;
      }
    }
    return result;
  }
}
