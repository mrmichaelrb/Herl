using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class LoadGamePlayScene
{
  static LoadGamePlayScene()
  {
    EditorSceneManager.newSceneCreated += OnNewScene;
  }

  private static void OnNewScene(Scene scene, UnityEditor.SceneManagement.NewSceneSetup setup, NewSceneMode mode)
  {
    if (scene.name != "GamePlay")
    {
      EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
    }
  }
}
