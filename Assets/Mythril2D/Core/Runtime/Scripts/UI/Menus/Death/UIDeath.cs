using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gyvr.Mythril2D
{
    public class UIDeath : MonoBehaviour, IUIMenu
    {
        [Header("References")]
        [SerializeField] private Button m_quitButton = null;

        public void Init()
        {
        }

        public void Show(params object[] args)
        {
            gameObject.SetActive(true);
        }

        public bool CanPop() => false;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void EnableInteractions(bool enable)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup)
            {
                canvasGroup.interactable = enable;
            }
        }

        public GameObject FindSomethingToSelect()
        {
            return m_quitButton.gameObject;
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene(GameManager.Config.mainMenuSceneName);
        }
    }
}
