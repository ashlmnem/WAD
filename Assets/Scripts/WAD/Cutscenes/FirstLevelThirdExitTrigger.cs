using UnityEngine;
using UnityEngine.Playables;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Dritter Outro-Trigger von Level 1. Wird per Trigger() aufgerufen
    /// (z.B. aus einem UnityEvent oder einem anderen Skript).
    /// Dateiname und Klassenname muessen identisch sein.
    /// </summary>
    public class FirstLevelThirdExitTrigger : MonoBehaviour
    {
        public PlayableDirector playable_director;

        private bool hasTriggered;

        public void Trigger()
        {
            if (hasTriggered) return;

            if (playable_director == null)
            {
                Debug.LogWarning($"[{nameof(FirstLevelThirdExitTrigger)}:{gameObject.name}] 'Playable Director' ist nicht zugewiesen.");
                return;
            }

            hasTriggered = true;
            playable_director.Play();
        }
    }
}
