using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Michsky.UI.Reach;

/// <summary>
/// Achievement notification UI component that displays when achievements are unlocked
/// Integrates with the existing Reach.UI notification system
/// </summary>
public class AchievementNotification : MonoBehaviour
{
  [Header("UI References")]
  [SerializeField] private Image achievementIcon;
  [SerializeField] private TextMeshProUGUI achievementTitle;
  [SerializeField] private TextMeshProUGUI achievementDescription;
  [SerializeField] private Image backgroundGlow;
  [SerializeField] private GameObject lockedIndicator;
  [SerializeField] private GameObject unlockedIndicator;

  [Header("Animation Settings")]
  [SerializeField] private float showDuration = 3f;
  [SerializeField] private float fadeInTime = 0.5f;
  [SerializeField] private float fadeOutTime = 0.5f;
  [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

  [Header("Achievement Type Colors")]
  [SerializeField] private Color commonColor = Color.white;
  [SerializeField] private Color rareColor = Color.blue;
  [SerializeField] private Color legendaryColor = Color.yellow;

  [Header("Sound Effects")]
  [SerializeField] private AudioClip unlockSound;
  [SerializeField] private AudioSource audioSource;

  private CanvasGroup canvasGroup;
  private RectTransform rectTransform;
  private bool isShowing = false;

  private void Awake()
  {
    canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
      canvasGroup = gameObject.AddComponent<CanvasGroup>();

    rectTransform = GetComponent<RectTransform>();

    // Start hidden
    canvasGroup.alpha = 0f;
    gameObject.SetActive(false);
  }

  /// <summary>
  /// Show achievement notification with the given achievement data
  /// </summary>
  public void ShowAchievement(LevelAchievementConfig.LevelAchievement achievement)
  {
    if (isShowing)
      return;

    StartCoroutine(ShowAchievementCoroutine(achievement));
  }

  /// <summary>
  /// Show achievement notification with custom data
  /// </summary>
  public void ShowAchievement(string title, string description, Sprite icon = null, AchievementLibrary.AchievementType type = AchievementLibrary.AchievementType.Common)
  {
    if (isShowing)
      return;

    StartCoroutine(ShowCustomAchievementCoroutine(title, description, icon, type));
  }

  private IEnumerator ShowAchievementCoroutine(LevelAchievementConfig.LevelAchievement achievement)
  {
    yield return ShowCustomAchievementCoroutine(
        achievement.AchievementTitle,
        achievement.AchievementDescription,
        achievement.AchievementIcon,
        achievement.AchievementType
    );
  }

  private IEnumerator ShowCustomAchievementCoroutine(string title, string description, Sprite icon, AchievementLibrary.AchievementType type)
  {
    isShowing = true;
    gameObject.SetActive(true);

    // Set up the notification content
    if (achievementTitle != null)
      achievementTitle.text = title;

    if (achievementDescription != null)
      achievementDescription.text = description;

    if (achievementIcon != null && icon != null)
      achievementIcon.sprite = icon;

    // Set up type-specific styling
    SetupAchievementTypeAppearance(type);

    // Set up locked/unlocked indicators
    if (lockedIndicator != null)
      lockedIndicator.SetActive(false);
    if (unlockedIndicator != null)
      unlockedIndicator.SetActive(true);

    // Play unlock sound
    PlayUnlockSound();

    // Animate in
    yield return AnimateIn();

    // Wait for display duration
    yield return new WaitForSeconds(showDuration);

    // Animate out
    yield return AnimateOut();

    // Hide and cleanup
    gameObject.SetActive(false);
    isShowing = false;
  }

  private void SetupAchievementTypeAppearance(AchievementLibrary.AchievementType type)
  {
    Color typeColor = commonColor;

    switch (type)
    {
      case AchievementLibrary.AchievementType.Common:
        typeColor = commonColor;
        break;
      case AchievementLibrary.AchievementType.Rare:
        typeColor = rareColor;
        break;
      case AchievementLibrary.AchievementType.Legendary:
        typeColor = legendaryColor;
        break;
    }

    // Apply color to background glow
    if (backgroundGlow != null)
      backgroundGlow.color = typeColor;

    // Apply color to title text
    if (achievementTitle != null)
      achievementTitle.color = typeColor;
  }

  private IEnumerator AnimateIn()
  {
    float elapsed = 0f;
    Vector3 startScale = Vector3.zero;
    Vector3 endScale = Vector3.one;

    while (elapsed < fadeInTime)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / fadeInTime;
      float curveProgress = showCurve.Evaluate(progress);

      canvasGroup.alpha = curveProgress;
      rectTransform.localScale = Vector3.Lerp(startScale, endScale, curveProgress);

      yield return null;
    }

    canvasGroup.alpha = 1f;
    rectTransform.localScale = endScale;
  }

  private IEnumerator AnimateOut()
  {
    float elapsed = 0f;
    Vector3 startScale = Vector3.one;
    Vector3 endScale = Vector3.zero;
    float startAlpha = canvasGroup.alpha;

    while (elapsed < fadeOutTime)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / fadeOutTime;
      float curveProgress = showCurve.Evaluate(1f - progress);

      canvasGroup.alpha = startAlpha * curveProgress;
      rectTransform.localScale = Vector3.Lerp(startScale, endScale, progress);

      yield return null;
    }

    canvasGroup.alpha = 0f;
    rectTransform.localScale = endScale;
  }

  private void PlayUnlockSound()
  {
    if (unlockSound != null && audioSource != null)
    {
      audioSource.PlayOneShot(unlockSound);
    }
  }

  /// <summary>
  /// Force hide the notification immediately
  /// </summary>
  public void ForceHide()
  {
    StopAllCoroutines();
    gameObject.SetActive(false);
    isShowing = false;
    canvasGroup.alpha = 0f;
    rectTransform.localScale = Vector3.one;
  }

  /// <summary>
  /// Check if notification is currently showing
  /// </summary>
  public bool IsShowing => isShowing;

#if UNITY_EDITOR
  [ContextMenu("Test Common Achievement")]
  private void TestCommonAchievement()
  {
    ShowAchievement("Test Common", "This is a test common achievement", null, AchievementLibrary.AchievementType.Common);
  }

  [ContextMenu("Test Rare Achievement")]
  private void TestRareAchievement()
  {
    ShowAchievement("Test Rare", "This is a test rare achievement", null, AchievementLibrary.AchievementType.Rare);
  }

  [ContextMenu("Test Legendary Achievement")]
  private void TestLegendaryAchievement()
  {
    ShowAchievement("Test Legendary", "This is a test legendary achievement", null, AchievementLibrary.AchievementType.Legendary);
  }
#endif
}
