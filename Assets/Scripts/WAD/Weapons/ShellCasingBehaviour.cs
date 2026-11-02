using UnityEngine;

namespace WAD.Weapons.Casings
{
    /// <summary>
    /// Liegt auf dem Huelsen-Prefab selbst (AmmoTypeSO.casingPrefab).
    ///
    /// - Spielt beim ersten Bodenkontakt einen metallischen "Klick"-Sound ab.
    /// - Spieler-Kollision: Eine Huelse kollidiert normal mit der Welt (Boden, Waende, andere Huelsen),
    ///   aber NICHT mit dem Spieler, solange sie in der Luft ist bzw. noch rollt. Vorher konnte man auf
    ///   fliegende Huelsen treten (der CharacterController wurde nach oben gedrueckt/blockiert), und die
    ///   Huelse - ein 5-10 cm grosser, 1 kg schwerer Kollider - hat den Spieler am Auswurfschacht sogar angeschoben.
    ///   Sobald sie zur Ruhe kommt, bekommt sie einen winzigen Zusatz-Kollider ("PlayerContact"), mit dem sie
    ///   fuer den Spieler nur noch ein sehr kleines Objekt ist; der grosse Kollider ignoriert den Spieler dauerhaft.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShellCasingBehaviour : MonoBehaviour
    {
        [Header("Sound")]
        public AudioClip[] landSounds;
        [Range(0f, 1f)] public float landVolume = 0.5f;
        [Tooltip("Mindest-Aufprallgeschwindigkeit, damit ueberhaupt ein Sound abgespielt wird (verhindert Sound bei sanftem Absetzen)")]
        public float minImpactVelocity = 0.5f;

        [Header("Interaktion mit dem Spieler")]
        [Tooltip("Radius (in Metern, Weltgroesse) des winzigen Kolliders, mit dem eine zur Ruhe gekommene Huelse den Spieler beruehrt")]
        public float playerContactRadius = 0.006f;
        [Tooltip("Unterhalb dieser Geschwindigkeit (m/s) gilt die Huelse nach einem Bodenkontakt als 'zur Ruhe gekommen'")]
        public float settleSpeed = 0.35f;
        [Tooltip("So lange (Sekunden) muss sie langsam sein, bevor der Spieler-Kontakt aktiviert wird")]
        public float settleTime = 0.2f;
        [Tooltip("Spaetestens nach dieser Zeit (Sekunden) wird der Spieler-Kontakt aktiviert, falls die Huelse nie richtig zur Ruhe kommt")]
        public float maxFlightTime = 3f;

        private bool hasPlayedLandSound;
        private bool hasTouchedSurface;
        private bool inFlight;
        private float flightStartTime;
        private float slowTime;
        private AudioSource audioSource;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D-Sound
        }

        /// <summary>
        /// Vom Ejector direkt nach dem Instanziieren aufgerufen: Die Huelse ignoriert ab sofort alle
        /// uebergebenen Spieler-Collider (muss VOR dem ersten Physik-Schritt passieren, weil sie am
        /// Auswurfschacht innerhalb des Spielers entsteht).
        /// </summary>
        public void BeginFlight(Collider[] playerColliders)
        {
            var own = GetComponentsInChildren<Collider>();

            if (playerColliders != null)
            {
                foreach (var mine in own)
                {
                    if (mine == null) continue;

                    foreach (var theirs in playerColliders)
                    {
                        if (theirs != null) Physics.IgnoreCollision(mine, theirs, true);
                    }
                }
            }

            inFlight = true;
            flightStartTime = Time.time;
            slowTime = 0f;
        }

        private void FixedUpdate()
        {
            if (!inFlight) return;

            bool slowEnough = hasTouchedSurface && body.linearVelocity.magnitude < settleSpeed;
            slowTime = slowEnough ? slowTime + Time.fixedDeltaTime : 0f;

            if (slowTime >= settleTime || Time.time - flightStartTime >= maxFlightTime)
            {
                EnablePlayerContact();
            }
        }

        private void EnablePlayerContact()
        {
            inFlight = false;

            // Kleiner Zusatz-Kollider am selben Rigidbody. Er ist von den Spieler-Collidern NICHT ausgenommen,
            // der grosse Hauptkollider bleibt es dauerhaft - fuer den Spieler ist die Huelse also nur noch winzig.
            var contact = new GameObject("PlayerContact");
            contact.transform.SetParent(transform, false);

            var sphere = contact.AddComponent<SphereCollider>();

            Vector3 scale = transform.lossyScale;
            float maxScale = Mathf.Max(0.0001f, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)));
            sphere.radius = playerContactRadius / maxScale;
        }

        private void OnCollisionEnter(Collision collision)
        {
            hasTouchedSurface = true;

            if (hasPlayedLandSound) return;
            if (collision.relativeVelocity.magnitude < minImpactVelocity) return;

            hasPlayedLandSound = true;

            if (landSounds != null && landSounds.Length > 0)
            {
                audioSource.PlayOneShot(landSounds[Random.Range(0, landSounds.Length)], landVolume);
            }
        }
    }
}
