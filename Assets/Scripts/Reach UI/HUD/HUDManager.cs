using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [DefaultExecutionOrder(-15)]
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
        private Dictionary<int, List<Image>> imagesByLevel = new Dictionary<int, List<Image>>();
        private Transform timerMask;
        private Transform timerMaskImage;
        private float totalTime = 0f;

        public enum DefaultBehaviour { Visible, Invisible }

        public void InitializeComponents()
        {
            if (imagesByLevel.Count != 0)
            {
                return;
            }

            GameDataManager gameDataManager = GUIManager.Instance.GameDataManager;
            SpawnManager spawnManager = FindObjectOfType<SpawnManager>();

            if (spawnManager != null)
            {
                foreach (EnemyData enemyData in spawnManager.enemyTypes)
                {
                    Sprite enemySprite = enemyData.spriteLibrary.GetSprite(enemyData.defaultSpriteCategory, "Frame 1");
                    if (enemySprite != null)
                    {
                        int level = enemyData.level;
                        if (!imagesByLevel.ContainsKey(level))
                        {
                            imagesByLevel[level] = new List<Image>();
                        }
                        int id = imagesByLevel[level].Count;

                        Transform transform = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find($"Level{level}")?.Find($"Image{id}");
                        Image image = transform?.GetComponent<Image>();
                        if (image != null)
                        {
                            image.sprite = enemySprite;
                            imagesByLevel[level].Add(image);
                            transform.gameObject.SetActive(true);
                            Debug.Log($"Image for Level {level} with ID {id} set in HUDPanel.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Sprite for {enemyData.defaultSpriteCategory}/{enemyData.defaultSpriteLabel} not found in SpriteLibrary.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("SpawnManager not found in the scene.");
            }


            progressBar = HUDPanel.transform.Find("Statistic")?.Find("Progress")?.Find("ProgressBar")?.Find("Background")?.Find("Bar");
            scoreText = HUDPanel.transform.Find("Statistic")?.Find("Stats")?.Find("Score")?.Find("Text")?.GetComponent<TextMeshProUGUI>();
            livesText = HUDPanel.transform.Find("Statistic")?.Find("Stats")?.Find("Lives")?.Find("Text")?.GetComponent<TextMeshProUGUI>();

            totalTime = spawnManager?.gameState?.LoseTime ?? 0f;
            Transform timer = HUDPanel.transform.Find("Statistic")?.Find("Stats")?.Find("Timer");
            if (totalTime <= 0f)
            {
                timer?.gameObject.SetActive(false);
            }
            else
            {
                timer?.gameObject.SetActive(true);
                timerMask = timer?.Find("Mask");
                timerMaskImage = timerMask?.Find("Image");
            }
        }

        void SetTimerLeftRatio(float ratio)
        {
            if (timerMask == null || timerMaskImage == null) return;
            timerMask.localScale = new Vector3(1, ratio, 1);
            timerMaskImage.localScale = new Vector3(1, 1f / ratio, 1);
        }

        void Awake()
        {
            if (HUDPanel == null)
                return;

            cg = HUDPanel.AddComponent<CanvasGroup>();

            // InitializeComponents();
            
            if (defaultBehaviour == DefaultBehaviour.Visible) { cg.alpha = 1; isOn = true; onSetVisible.Invoke(); }
            else if (defaultBehaviour == DefaultBehaviour.Invisible) { cg.alpha = 0; isOn = false; onSetInvisible.Invoke(); }
        }

        void Start()
        {
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            // UnsubscribeFromEvents();
        }

        void SetEnableLevel(int level, int enable)
        {
            if (imagesByLevel.ContainsKey(level))
            {
                foreach (Image image in imagesByLevel[level])
                {
                    image.color = new Color(enable, enable, enable, 1f);
                }
            }
            else
            {
                Debug.LogWarning($"No images found for Level {level}.");
            }
        }

        public void OnDataChanged(GameSessionData gameSessionData)
        {
            if (progressBar != null && gameSessionData != null)
            {
                // min of current and 1
                float progress = Mathf.Min(1f, (gameSessionData.currentLevel - 1 + (gameSessionData.xpToNextLevel > 0 ? gameSessionData.currentXP / gameSessionData.xpToNextLevel : 0)) / 3f);
                progressBar.localScale = new Vector3(progress, 1, 1);
                if (progress >= 1f / 3f - 1e-9)
                {
                    SetEnableLevel(2, 1);
                }
                else
                {
                    SetEnableLevel(2, 0);
                }

                if (progress >= 2f / 3f - 1e-9)
                {
                    SetEnableLevel(3, 1);
                }
                else
                {
                    SetEnableLevel(3, 0);
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

        public void OnGameTimerUpdate(float time)
        {
            SetTimerLeftRatio(1.0f - time / totalTime);
        }

        public void SubscribeToEvents()
        {
            GameDataManager gameDataManager = GUIManager.Instance.GameDataManager;
            if (gameDataManager != null)
            {
                gameDataManager.OnDataChanged.AddListener(OnDataChanged);
                // gameDataManager.OnScoreChanged.AddListener(OnScoreChanged);
                // gameDataManager.OnLivesChanged.AddListener(OnLivesChanged);

                // Set the initial state of the HUD
                if (livesText != null)
                {
                    Debug.Log($"HUDManager: Setting initial lives text to {gameDataManager.SessionData.lives}");
                    livesText.text = $"{gameDataManager.SessionData.lives}";
                }
                if (scoreText != null)
                {
                    scoreText.text = $"Score: {(int)gameDataManager.SessionData.score}";
                }
            }
        }

        public void UnsubscribeFromEvents()
        {
            GameDataManager gameDataManager = GUIManager.Instance.GameDataManager;
            if (gameDataManager != null)
            {
                gameDataManager.OnDataChanged.RemoveListener(OnDataChanged);
                // gameDataManager.OnScoreChanged.RemoveListener(OnScoreChanged);
                // gameDataManager.OnLivesChanged.RemoveListener(OnLivesChanged);
            }
            
            SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
            spawnManager?.gameState?.OnGameTimerUpdate?.RemoveListener(OnGameTimerUpdate);
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