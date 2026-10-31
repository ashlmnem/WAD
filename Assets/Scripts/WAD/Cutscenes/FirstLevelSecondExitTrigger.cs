using UnityEngine;
using UnityEngine.Playables;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Zweiter Outro-Trigger von Level 1. Dateiname und Klassenname muessen
    /// identisch sein, sonst kann Unity die Komponente nicht laden.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FirstLevelSecondExitTrigger : MonoBehaviour
    {
        public PlayableDirector playable_director;

        [Tooltip("Optional: nur Objekte mit diesem Tag loesen den Trigger aus. Leer = jedes Objekt.")]
        public string requiredTag = "Player";

        private bool hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            if (playable_director == null)
            {
                Debug.LogWarning($"[{nameof(FirstLevelSecondExitTrigger)}:{gameObject.name}] 'Playable Director' ist nicht zugewiesen.");
                return;
            }

            hasTriggered = true;
            playable_director.Play();
        }
    }
}
