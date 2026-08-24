using UnityEngine;
using WAD.Weapons;

namespace WAD.Inventory
{
    /// <summary>
    /// Blick-Interaktion: Raycast von der Kamera nach vorn, 'F' zum Ausloesen.
    /// Findet JEDES IInteractable ueber einen einzigen Lookup - neue
    /// Stationstypen (Taskmaster, Waffentausch usw.) muessen hier nicht mehr
    /// einzeln eingetragen werden, nur IInteractable implementieren.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Referenzen (von IInteractable-Implementierungen genutzt)")]
        public Camera playerCamera;
        public InventoryManager inventory;
        public PlayerWeaponHolder weaponHolder;

        [Header("Einstellungen")]
        public float interactionRange = 2.5f;
        public LayerMask interactableLayers = ~0;

        private IInteractable currentTarget;

        public event System.Action<string> OnLookAtInteractable;
        public event System.Action OnLookAwayFromInteractable;

        private void Update()
        {
            UpdateLookTarget();

            if (Input.GetKeyDown(KeyCode.F) && currentTarget != null)
            {
                currentTarget.Interact(this);
            }
        }

        private void UpdateLookTarget()
        {
            if (playerCamera == null) return;

            IInteractable hit = null;

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit rayHit, interactionRange, interactableLayers))
            {
                hit = rayHit.collider.GetComponentInParent<IInteractable>();

                // Lose WeaponController im Weltraum sollen nicht ausgeruestet werden
                // koennen, wenn es die gerade in der Hand gehaltene Waffe selbst ist.
                if (hit is WeaponController weaponHit && weaponHolder != null
                    && weaponHit.transform.parent == weaponHolder.weaponSocket)
                {
                    hit = null;
                }
            }

            if (hit != currentTarget)
            {
                currentTarget = hit;

                if (hit != null) OnLookAtInteractable?.Invoke(hit.InteractionPrompt);
                else OnLookAwayFromInteractable?.Invoke();
            }
        }
    }
}