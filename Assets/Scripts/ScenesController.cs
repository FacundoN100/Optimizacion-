using UnityEngine.SceneManagement;

public sealed class ScenesController
{
    public void BackToMenu() => SceneManager.LoadScene(0);
    public void Retry() => SceneManager.LoadScene(1);
}
