using UnityEngine;

namespace Michsky.UI.Reach
{
    public class ExitGame : MonoBehaviour
    {
        public void Exit()
        {

#if UNITY_EDITOR
            Debug.Log("<b>[Reach UI]</b> Exit function works in builds only.");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("<b>[Reach UI]</b> Exiting the game.");
            Application.Quit();
#endif
        }
    }
}