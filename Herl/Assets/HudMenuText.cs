using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

[RequireComponent(typeof(Text))]
public class HudMenuText : MonoBehaviour
{
  const string MenuNormal =
    @"Menu - Enter/Start
Quit - Esc/Back

Keyboard Steering:
Up/Down - W/S or Num 8/2
Left/Right - A/D or Num 4/6

Blaster - Left Ctrl or Right Trigger
Boost - Left Shift or Right Bumper

Prev/Next World - H/J or Left Stick Up/Down
Terran/Alien - K/L or Left Stick Left/Right

Mouse Steering - M
Invert - I
Free Look - F or Controller X

Spawn Bots - V
Raymarcher - R";

  const string MenuVr =
    @"Menu - Enter/Start
Quit - Esc/Back

Iris Size - O/P
Center - C or Controller A or Right Button
Free Look - F or Controller X or Left Button

Blaster - Left Ctrl or Right Trigger
Boost - Left Shift or Right Bumper

Prev/Next World - H/J or Left Stick Up/Down
Terran/Alien - K/L or Left Stick Left/Right

Spawn Bots - V
Raymarcher - R";

  public GameObject MenuBackground;

  Text _text;

  void Awake()
  {
    _text = GetComponent<Text>();

    string title = Application.productName + " version " + Application.version + "\n";

    if (XRSettings.enabled)
    {
      _text.text = title + MenuVr;
    }
    else
    {
      _text.text = title + MenuNormal;
    }
  }

  void Update()
  {
    if (Input.GetButtonDown("Menu") || (VirtualReality.IsOpenVrHmdPresent() && Input.GetButtonDown("Center")))
    {
      _text.enabled = !_text.enabled;
      MenuBackground.SetActive(_text.enabled);
    }
  }
}
