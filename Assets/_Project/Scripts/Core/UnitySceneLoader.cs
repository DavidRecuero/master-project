using UnityEngine.SceneManagement;

public class UnitySceneLoader : ISceneLoader
{
    public void LoadScene(int sceneIndex) => SceneManager.LoadScene(sceneIndex);

    public void ReloadCurrentScene() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}