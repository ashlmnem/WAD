using UnityEngine;

namespace WAD.Weapons.Casings
{
    /// <summary>
    /// Liegt auf demselben Objekt wie WeaponController. Wirft bei jedem Schuss
    /// eine physisch simulierte Huelse aus dem Auswurfschacht aus - Modell
    /// kommt aus AmmoTypeSO.casingPrefab, passt sich also automatisch dem
    /// geladenen Kaliber an (9mm-Huelse sieht anders aus als .308 etc.).
    /// </summary>
    public class ShellCasingEjector : MonoBehaviour
    {
        [Header("Referenz")]
        [Tooltip("Leerer Punkt am Auswurfschacht der Waffe - 'right' zeigt zur Auswurfseite, 'forward' nach vorn")]
        public Transform ejectionPort;

        [Header("Physik")]
        public float ejectForceMin = 1.3f;
        public float ejectForceMax = 2.2f;
        [Tooltip("Wie stark die Huelse zusaetzlich nach oben geworfen wird")]
        public float upwardForceMultiplier = 1f;
        [Tooltip("Wie stark die Huelse zusaetzlich leicht nach hinten geworfen wird (realistisch bei den meisten Waffen)")]
        public float backwardForceMultiplier = 0.3f;
        public float torqueStrength = 6f;

        [Header("Aufraeumen")]
        public float destroyAfterSeconds = 10f;

        /// <summary> Von WeaponController nach jedem Schuss aufgerufen. </summary>
        public void EjectCasing(GameObject casingPrefab)
        {
            if (casingPrefab == null || ejectionPort == null) return;

            GameObject casing = Instantiate(casingPrefab, ejectionPort.position, ejectionPort.rotation);

            var rb = casing.GetComponent<Rigidbody>();
            if (rb == null) rb = casing.AddComponent<Rigidbody>();

            Vector3 ejectDirection = ejectionPort.right
                + Vector3.up * upwardForceMultiplier
                + (-ejectionPort.forward) * backwardForceMultiplier;

            float force = Random.Range(ejectForceMin, ejectForceMax);
            rb.AddForce(ejectDirection.normalized * force, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * torqueStrength;
            rb.AddTorque(randomTorque, ForceMode.Impulse);

            Destroy(casing, destroyAfterSeconds);
        }
    }
}