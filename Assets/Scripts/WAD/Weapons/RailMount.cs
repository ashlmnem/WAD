using System.Collections.Generic;
using UnityEngine;

namespace WAD.Weapons.Attachments
{
    [System.Serializable]
    public class RailMount
    {
        [Tooltip("Frei waehlbarer Name zur Wiedererkennung, z.B. 'UpperReceiverRail', 'MagPoint'")]
        public string railId;
        public Transform mountTransform;
        public List<AttachmentCategory> acceptedCategories = new List<AttachmentCategory>();

        public bool Accepts(AttachmentCategory category) => acceptedCategories.Contains(category);
    }
}