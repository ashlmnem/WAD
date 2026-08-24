using System.Collections.Generic;
using UnityEngine;
using WAD.Weapons;
using WAD.Weapons.Attachments;

namespace WAD.Menu
{
    [System.Serializable]
    public class AttachmentSelection
    {
        public string railId;
        public AttachmentSO attachment;
    }

    /// <summary>
    /// Persistenter Speicher (DontDestroyOnLoad) fuer die im Main-Menu-Loadout-
    /// Bildschirm gewaehlte Startwaffe + Attachments (jetzt pro Rail-ID statt
    /// pro Attachment-Typ, siehe Rail-System-Rework).
    /// </summary>
    public class WeaponLoadoutManager : MonoBehaviour
    {
        public static WeaponLoadoutManager Instance { get; private set; }

        public WeaponSO selectedWeapon;
        public List<AttachmentSelection> selectedAttachments = new List<AttachmentSelection>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAttachment(string railId, AttachmentSO attachment)
        {
            var existing = selectedAttachments.Find(a => a.railId == railId);
            if (existing != null) existing.attachment = attachment;
            else selectedAttachments.Add(new AttachmentSelection { railId = railId, attachment = attachment });
        }

        public AttachmentSO GetAttachment(string railId)
        {
            return selectedAttachments.Find(a => a.railId == railId)?.attachment;
        }

        /// <summary> Wendet die gespeicherte Auswahl auf eine frisch ausgeruestete Waffe an. </summary>
        public void ApplyTo(WeaponAttachmentManager manager)
        {
            if (manager == null) return;
            foreach (var sel in selectedAttachments)
            {
                if (sel.attachment != null) manager.EquipAttachment(sel.railId, sel.attachment);
            }
        }
    }
}