using UnityEngine;
using UnityEngine.Playables;
using WAD.Player;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Liegt auf einem Trigger-Collider in der Welt. Startet beim Betreten
    /// durch den Spieler eine Timeline-Cutscene (PlayableDirector) und kann
    /// waehrenddessen optional Bewegung/Kamera-Steuerung sperren.
    ///
    /// Setup: Objekt mit Box Collider (IsTrigger = true) + dieses Skript,
    /// director = der PlayableDirector deiner Timeline.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CutsceneTrigger : MonoBehaviour
    {
        [Header("Referenzen")]
        public PlayableDirector director;
        public TarkovMovementController movementController;
        public FirstPersonCameraLook cameraLook;

        [Header("Einstellungen")]
        public bool triggerOnlyOnce = true;
        public bool lockPlayerDuringCutscene = true;

        private bool hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && triggerOnlyOnce) return;
            if (!other.CompareTag("Player")) return; // Player-Objekt braucht den Tag "Player"

            hasTriggered = true;
            PlayCutscene();
        }

        private void PlayCutscene()
        {
            if (director == null) return;

            if (lockPlayerDuringCutscene)
            {
                if (movementController != null) movementController.enabled = false;
                if (cameraLook != null) cameraLook.enabled = false;
            }

            director.stopped += OnCutsceneFinished;
            director.Play();
        }

        private void OnCutsceneFinished(PlayableDirector finishedDirector)
        {
            director.stopped -= OnCutsceneFinished;

            if (lockPlayerDuringCutscene)
            {
                if (movementController != null) movementController.enabled = true;
                if (cameraLook != null) cameraLook.enabled = true;
            }
        }
    }
}
