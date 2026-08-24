using UnityEngine;

namespace WAD.UI
{
    [System.Serializable]
    public class CompassMarker
    {
        public RectTransform rectTransform;
        [Tooltip("0=Norden, 90=Osten, 180=Sueden, 270=Westen")]
        public float degrees;
    }

    /// <summary>
    /// Gleitender Kompassstreifen wie in Arma/Tarkov: Buchstaben bewegen sich
    /// horizontal je nach Blickrichtung der Kamera. Baue die Marker-Objekte
    /// (Text "N", "E", "S", "W") selbst im Canvas, das Skript positioniert sie nur.
    /// </summary>
    public class CompassUI : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform playerCameraOrRoot; // Kamera oder Player-Root, je nachdem wovon die Blickrichtung kommt

        [Header("Marker (N/E/S/W, ggf. auch NE/SE/SW/NW)")]
        public CompassMarker[] markers;

        [Header("Darstellung")]
        [Tooltip("Wie viele Grad Blickfeld ueber die volle Breite des Kompass-Containers abgebildet werden")]
        public float visibleDegreesRange = 180f;
        public float containerWidthPixels = 500f;

        private void Update()
        {
            if (playerCameraOrRoot == null) return;

            float currentYaw = playerCameraOrRoot.eulerAngles.y;

            foreach (var marker in markers)
            {
                if (marker.rectTransform == null) continue;

                float delta = Mathf.DeltaAngle(currentYaw, marker.degrees);
                float x = (delta / visibleDegreesRange) * containerWidthPixels;

                Vector2 pos = marker.rectTransform.anchoredPosition;
                pos.x = x;
                marker.rectTransform.anchoredPosition = pos;

                // Ausserhalb des sichtbaren Bereichs ausblenden (falls kein Mask-Objekt genutzt wird)
                bool visible = Mathf.Abs(delta) <= visibleDegreesRange * 0.55f;
                marker.rectTransform.gameObject.SetActive(visible);
            }
        }
    }
}
