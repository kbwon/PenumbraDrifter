using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoSceneLoader : MonoBehaviour
{
    public string titleSceneName = "TitleScene";
    public string tutorialSceneName = "TutorialStage";

    public void StartNewGame()
    {
        DemoProgress.ResetProgress();
        ResumeBeforeSceneLoad();
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void ContinueGame()
    {
        if (!DemoProgress.HasContinue)
            return;

        string sceneName = DemoProgress.GetContinueSceneName();

        if (string.IsNullOrEmpty(sceneName))
            return;

        ResumeBeforeSceneLoad();
        SceneManager.LoadScene(sceneName);
    }

    public void LoadStage(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        ResumeBeforeSceneLoad();
        SceneManager.LoadScene(sceneName);
    }

    public void RestartCurrentStage()
    {
        ResumeBeforeSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToTitle()
    {
        ResumeBeforeSceneLoad();
        SceneManager.LoadScene(titleSceneName);
    }

    public void QuitGame()
    {
        ResumeBeforeSceneLoad();
        Application.Quit();
    }

    void ResumeBeforeSceneLoad()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);

        Time.timeScale = 1f;
    }
}