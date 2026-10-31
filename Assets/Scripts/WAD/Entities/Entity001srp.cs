using UnityEngine;
using WAD.Core;
using WAD.Weapons;
using WAD.Player;

namespace WAD.Entities
{
    /// <summary>
    /// Entity-001srp: Beim ersten Schuss wird eine Leuchtrakete verschossen
    /// (ruft Helikopter -> Exit 1 auf Level 1), danach verwandelt sich die
    /// Waffe selbst: 75% M27-Pistole, 25% "Staccato-9".
    ///
    /// WICHTIG: weaponHolder/cameraLook werden NICHT im Prefab manuell
    /// zugewiesen (geht nicht - Cross-Prefab-Referenz auf Szenen-Objekte ist
    /// in Unity nicht erlaubt), sondern automatisch von
    /// PlayerWeaponHolder.PickUpWeapon() ueber AutoWireReferences() gesetzt,
    /// sobald die Waffe tatsaechlich ausgeruestet wird.
    /// </summary>
    [RequireComponent(typeof(WeaponController))]
    public class Entity001srp : MonoBehaviour
    {
        [Header("Daten")]
        public EntitySO entityData;
        public EntityUseExit linkedExit;

        [Header("Transformation")]
        [Range(0f, 1f)] public float chanceForM27 = 0.75f;
        public GameObject m27Prefab;
        public GameObject staccato9Prefab;

        [Header("Helikopter")]
        public GameObject helicopterPrefab;
        [Tooltip("Optional. Falls leer: Helikopter erscheint automatisch ueber dem Spieler.")]
        public Transform helicopterSpawnPoint;
        public float fallbackSpawnHeightAboveGround = 40f;

        // Werden automatisch von PlayerWeaponHolder.PickUpWeapon() gesetzt - NICHT im Inspector befuellen.
        private PlayerWeaponHolder weaponHolder;
        private FirstPersonCameraLook cameraLook;

        private bool hasFiredFlare = false;
        private WeaponController weaponController;

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
        }

        private void OnEnable()
        {
            if (weaponController == null) weaponController = GetComponent<WeaponController>();

            // Ohne diese Pruefung wirft OnEnable eine NullReferenceException, wenn
            // die Komponente ohne WeaponController aktiviert wird; danach bliebe die
            // Waffe zudem dauerhaft von OnFire abgekoppelt.
            if (weaponController == null)
            {
                Debug.LogWarning($"[Entity001srp:{gameObject.name}] Kein WeaponController auf demselben Objekt - Entity-Logik ist deaktiviert.");
                return;
            }

            weaponController.OnFired -= OnFire; // gegen Doppel-Abos bei mehrfachem Enable
            weaponController.OnFired += OnFire;
        }

        private void OnDisable()
        {
            if (weaponController != null) weaponController.OnFired -= OnFire;
        }

        /// <summary> Von PlayerWeaponHolder beim Ausruesten aufgerufen - siehe Klassenkommentar. </summary>
        public void AutoWireReferences(PlayerWeaponHolder holder)
        {
            weaponHolder = holder;
            cameraLook = holder != null ? holder.cameraLook : null;
        }

        private void OnFire()
        {
            if (hasFiredFlare) return;

            hasFiredFlare = true;
            FireFlareAndSummonHelicopter();
            TransformIntoRandomWeapon();
        }

        private void FireFlareAndSummonHelicopter()
        {
            if (helicopterPrefab != null)
            {
                Vector3 spawnPos = helicopterSpawnPoint != null
                    ? helicopterSpawnPoint.position
                    : GetFallbackSpawnPosition();

                Instantiate(helicopterPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[Entity001srp:{gameObject.name}] 'Helicopter Prefab' ist nicht zugewiesen.");
            }

            linkedExit?.MarkUsed();
        }

        /// <summary> Ohne manuell gesetzten Spawn-Punkt: Helikopter erscheint ueber dem Spieler. </summary>
        private Vector3 GetFallbackSpawnPosition()
        {
            Vector3 basePos = weaponHolder != null ? weaponHolder.transform.position : transform.position;
            return basePos + Vector3.up * fallbackSpawnHeightAboveGround;
        }

        private void TransformIntoRandomWeapon()
        {
            bool becomesM27 = Random.value < chanceForM27;
            GameObject resultPrefab = becomesM27 ? m27Prefab : staccato9Prefab;

            if (resultPrefab == null)
            {
                Debug.LogWarning($"[Entity001srp:{gameObject.name}] Result-Prefab ({(becomesM27 ? "M27 Prefab" : "Staccato9 Prefab")}) ist nicht zugewiesen - Waffe bleibt unveraendert.");
                return;
            }

            if (weaponHolder == null)
            {
                Debug.LogWarning($"[Entity001srp:{gameObject.name}] Keine Waffe ausgeruestet - AutoWireReferences() wurde nicht aufgerufen? Wurde die Waffe wirklich ueber PickUpWeapon() ausgeruestet?");
                return;
            }

            GameObject instance = Instantiate(resultPrefab);
            var newController = instance.GetComponent<WeaponController>();

            if (newController == null)
            {
                // Ohne Ersatz darf die Flare-Gun NICHT zerstoert werden - sonst
                // bleibt der Spieler ohne Waffe zurueck.
                Debug.LogError($"[Entity001srp:{gameObject.name}] Result-Prefab hat keinen WeaponController - Transformation abgebrochen.");
                Destroy(instance);
                return;
            }

            // Erst die alte Waffe aus den Slots nehmen (sonst zeigt der Slot auf
            // ein zerstoertes Objekt und belegt dauerhaft einen Platz), dann die
            // neue aufnehmen, dann das alte Objekt zerstoeren.
            weaponHolder.RemoveWeapon(GetComponent<WeaponController>());
            weaponHolder.PickUpWeapon(newController);

            Destroy(gameObject);
        }
    }
}