using UnityEngine;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Wie die Kamera in einem Cutscene-Schritt gefuehrt wird.
    /// </summary>
    public enum CutsceneCameraMode
    {
        /// <summary>Aktuelle Spielerkamera bleibt stehen (nur Untertitel/Sound).</summary>
        HoldPlayerView,
        /// <summary>Kamera faehrt zwischen zwei Punkten (First-Person-Blick).</summary>
        DollyBetweenPoints,
        /// <summary>Kamera steht an einem Punkt und schwenkt von Blick A nach Blick B.</summary>
        PanAtPoint,
        /// <summary>Kamera folgt einem Objekt in der Szene (z.B. Helikopter).</summary>
        FollowTarget
    }

    /// <summary>
    /// Ein einzelner Schritt einer Cutscene. Alles im Inspector einstellbar.
    /// </summary>
    [System.Serializable]
    public class CutsceneStep
    {
        [Tooltip("Nur zur Uebersicht im Inspector")]
        public string label = "Schritt";

        [Header("Timing")]
        [Min(0.1f)] public float duration = 3f;
        [Tooltip("Ueberblendung von vorherigem Schritt (Sekunden). 0 = harter Schnitt.")]
        public float blendFromPrevious = 0f;
        [Tooltip("Praeziser Schnitt - Schritt startet mit einem Schnitt statt Ueberblendung")]
        public bool hardCut = false;

        [Header("Kamera")]
        public CutsceneCameraMode cameraMode = CutsceneCameraMode.HoldPlayerView;
        [Tooltip("Startpunkt (leer = aktuelles Kamera-Transform)")]
        public Transform pointA;
        [Tooltip("Endpunkt (bei Dolly) bzw. Blickziel (bei Pan)")]
        public Transform pointB;
        [Tooltip("Zielobjekt bei 'FollowTarget'")]
        public Transform followTarget;
        [Tooltip("Versatz zum Follow-Ziel")]
        public Vector3 followOffset = new Vector3(0f, 2f, -5f);
        [Tooltip("Zusaetzliche Blickrichtung, falls kein pointB gesetzt ist")]
        public Vector3 lookDirection = Vector3.forward;
        [Tooltip("Sichtfeld waehrend dieses Schritts (0 = unveraendert)")]
        public float fieldOfView = 0f;
        [Tooltip("Handheld-Wackeln fuer diesen Schritt (Found-Footage Look)")]
        [Range(0f, 3f)] public float handheldShake = 0.4f;

        [Header("Untertitel")]
        [TextArea(2, 4)] public string subtitleText;
        [Tooltip("Wie lange der Untertitel steht (0 = volle Schrittdauer)")]
        public float subtitleDuration = 0f;

        [Header("Audio")]
        public AudioClip sound;
        [Range(0f, 1f)] public float soundVolume = 1f;
        [Tooltip("Sound 3D an einer Position abspielen (leer = 2D)")]
        public Transform soundPosition;

        [Header("Zeit-Steuerung")]
        [Tooltip("Spielzeit fuer diesen Schritt (Slow-Motion-Effekt). 1 = normal")]
        [Range(0.05f, 3f)] public float timeScale = 1f;

        [Header("Optik")]
        [Tooltip("Optik-Profil, das in diesem Schritt aktiv wird (leer = Level-Standard)")]
        public WAD.CameraFx.FoundFootageProfile footageOverride;

        [Header("Ausloeser")]
        [Tooltip("UnityEvent, das am Ende dieses Schritts ausgeloest wird")]
        public UnityEngine.Events.UnityEvent onStepEnd;
    }

    /// <summary>
    /// Eine komplette Cutscene: Folge von Schritten in First-Person-Optik.
    /// Erstelle im Editor via: Assets > Create > WAD > Cutscene Sequence
    /// </summary>
    [CreateAssetMenu(fileName = "Cutscene_", menuName = "WAD/Cutscene Sequence")]
    public class CutsceneSequence : ScriptableObject
    {
        [Header("Identifikation")]
        public string cutsceneId;
        public string displayName;

        [Header("Hintergrund-Optik")]
        [Tooltip("Found-Footage-Profil fuer diese Cutscene (leer = Level-Standard)")]
        public WAD.CameraFx.FoundFootageProfile footageProfile;

        [Header("Steuerung")]
        [Tooltip("Spieler waehrend der Cutscene nicht steuern lassen")]
        public bool lockPlayerInput = true;
        [Tooltip("Waffe waehrend der Cutscene ausblenden")]
        public bool hideWeapon = true;
        [Tooltip("Mauszeiger sichtbar machen")]
        public bool showCursor = false;
        [Tooltip("Cutscene darf per Tastendruck uebersprungen werden")]
        public bool skippable = true;
        public KeyCode skipKey = KeyCode.Escape;

        [Header("Schritte")]
        public CutsceneStep[] steps = System.Array.Empty<CutsceneStep>();

        [Header("Nach dem Ende")]
        [Tooltip("Am Ende automatisch eine Szene laden (leer = keine)")]
        public string loadSceneAfter = "";

        public float TotalDuration
        {
            get
            {
                float total = 0f;
                foreach (var s in steps) if (s != null) total += s.duration;
                return total;
            }
        }
    }
}
