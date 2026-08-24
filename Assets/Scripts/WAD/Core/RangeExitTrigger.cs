using UnityEngine;
using UnityEngine.SceneManagement;

namespace WAD.ShootingRange
{
    /// <summary>
    /// Trigger-Collider am Ausgang des Schiessstands - bringt zurueck ins Main Menu.
    /// Alternativ kannst du stattdessen einen simplen UI-Button mit
    /// SceneManager.LoadScene("MainMenu") verknuepfen.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RangeExitTrigger : MonoBehaviour
    {
        public string mainMenuSceneName = "MainMenu";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
