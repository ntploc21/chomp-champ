using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

namespace Michsky.UI.Reach
{
    public class HUDManager : MonoBehaviour
    {
        // Resources
        public GameObject HUDPanel;
        private CanvasGroup cg;

        // Settings
        [Range(1, 20)] public float fadeSpeed = 8;
        public DefaultBehaviour defaultBehaviour = DefaultBehaviour.Visible;

        // Events
        public UnityEvent onSetVisible;
        public UnityEvent onSetInvisible;

        // Helpers
        private bool isOn;

        // Displayed canvases
        private Transform progressBar;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI livesText;
        private Image imageLevel1;
        private Image imageLevel1_2;
        private Image imageLevel2;
        private Image imageLevel2_2;

        public enum DefaultBehaviour { Visible, Invisible }

        public void OnDataChanged(GameSessionData gameSessionData)
        {
            if (progressBar != null && gameSessionData != null)
            {
                // min of current and 1
                float progress = Mathf.Min(1f, (gameSessionData.currentLevel - 1 + (gameSessionData.xpToNextLevel > 0 ? gameSessionData.currentXP / gameSessionData.xpToNextLevel : 0)) / 3f);
                progressBar.localScale = new Vector3(progress, 1, 1);
                if (progress >= 1f / 3f - 1e-9)
                {
                    if (imageLevel1 != null)
                        imageLevel1.color = new Color(1, 1, 1, 1);
                    if (imageLevel1_2 != null)
                        imageLevel1_2.color = new Color(1, 1, 1, 1);
                }
                if (progress >= 2f / 3f - 1e-9)
                {
                    if (imageLevel2 != null)
                        imageLevel2.color = new Color(1, 1, 1, 1);
                    if (imageLevel2_2 != null)
                        imageLevel2_2.color = new Color(1, 1, 1, 1);
                }
                // Debug.Log($"ProgressBar scale set to: {progressBar.localScale}");
            }
            OnScoreChanged(gameSessionData.score);
            OnLivesChanged(gameSessionData.lives);
        }

        public void OnScoreChanged(float newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {(int)newScore}";
                // Debug.Log($"ScoreText updated: {scoreText.text}");
            }
        }

        public void OnLivesChanged(int newLives)
        {
            if (livesText != null)
            {
                livesText.text = $"{newLives}";
                // Debug.Log($"LivesText updated: {livesText.text}");
            }
        }

        void Awake()
        {
            Debug.Log("HUDManager Awake called.");

            if (HUDPanel == null)
                return;

            cg = HUDPanel.AddComponent<CanvasGroup>();

            progressBar = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("ProgressBar")?.Find("Background")?.Find("Bar");
            scoreText = HUDPanel.transform.Find("Statistic")?.Find("Stats")?.Find("Score")?.Find("Text")?.GetComponent<TextMeshProUGUI>();
            livesText = HUDPanel.transform.Find("Statistic")?.Find("Stats")?.Find("Lives")?.Find("Text")?.GetComponent<TextMeshProUGUI>();
            imageLevel1 = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("Level1")?.Find("Image")?.GetComponent<Image>();
            imageLevel1_2 = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("Level1")?.Find("Image2")?.GetComponent<Image>();
            imageLevel2 = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("Level2")?.Find("Image")?.GetComponent<Image>();
            imageLevel2_2 = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("Level2")?.Find("Image2")?.GetComponent<Image>();

            if (defaultBehaviour == DefaultBehaviour.Visible) { cg.alpha = 1; isOn = true; onSetVisible.Invoke(); }
                else if (defaultBehaviour == DefaultBehaviour.Invisible) { cg.alpha = 0; isOn = false; onSetInvisible.Invoke(); }

            Debug.Log("HUDManager Awake completed.");
        }

        public void SetVisible()
        {
            if (isOn == true) { SetVisible(false); }
            else { SetVisible(true); }
        }

        public void SetVisible(bool value)
        {
            if (HUDPanel == null)
                return;

            if (value == true)
            {
                isOn = true;
                onSetVisible.Invoke();

                StopCoroutine("DoFadeIn");
                StopCoroutine("DoFadeOut");
                StartCoroutine("DoFadeIn");
            }

            else
            {
                isOn = false;
                onSetInvisible.Invoke();

                StopCoroutine("DoFadeIn");
                StopCoroutine("DoFadeOut");
                StartCoroutine("DoFadeOut");
            }
        }

        IEnumerator DoFadeIn()
        {
            while (cg.alpha < 0.99f)
            {
                cg.alpha += Time.unscaledDeltaTime * fadeSpeed;
                yield return null;
            }

            cg.alpha = 1;
        }

        IEnumerator DoFadeOut()
        {
            while (cg.alpha > 0.01f)
            {
                cg.alpha -= Time.unscaledDeltaTime * fadeSpeed;
                yield return null;
            }

            cg.alpha = 0;
        }
    }
}