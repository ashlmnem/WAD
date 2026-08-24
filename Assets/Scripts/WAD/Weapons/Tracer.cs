using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Bewegt ein visuelles Geschoss vom Muendungspunkt zum Einschlagpunkt.
    /// Erstellt bei Bedarf automatisch ein sichtbares Unlit-Material, falls
    /// keins zugewiesen ist - haeufigste Ursache fuer "unsichtbare" Tracer.
    /// </summary>
    public class Tracer : MonoBehaviour
    {
        public float speed = 300f;
        public LineRenderer lineRenderer;
        public Color tracerColor = new Color(1f, 0.75f, 0.2f); // warmes Gelb-Orange
        [Tooltip("Mindestdauer in Sekunden, damit der Tracer auch bei sehr hoher Geschwindigkeit sichtbar bleibt")]
        public float minVisibleDuration = 0.05f;

        private Vector3 startPoint;
        private Vector3 targetPoint;
        private float travelDistance;
        private float traveled;
        private float effectiveSpeed;

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                // Falls kein Material gesetzt ist: automatisch ein sichtbares erzeugen
                if (lineRenderer.sharedMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Color");
                    if (shader == null) shader = Shader.Find("Sprites/Default");

                    Material mat = new Material(shader);
                    if (mat.HasProperty("_Color")) mat.color = tracerColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tracerColor);
                    lineRenderer.material = mat;
                }

                if (lineRenderer.startWidth <= 0.001f) lineRenderer.startWidth = 0.03f;
                if (lineRenderer.endWidth <= 0.001f) lineRenderer.endWidth = 0.03f;

                lineRenderer.startColor = tracerColor;
                lineRenderer.endColor = tracerColor;
            }
        }

        public void Init(Vector3 start, Vector3 target, float travelSpeed)
        {
            startPoint = start;
            targetPoint = target;
            travelDistance = Vector3.Distance(start, target);
            traveled = 0f;

            // Garantiert eine Mindest-Sichtbarkeitsdauer, auch bei hoher Muzzle Velocity
            float minSpeed = travelDistance / minVisibleDuration;
            effectiveSpeed = Mathf.Min(travelSpeed, minSpeed);
            if (effectiveSpeed <= 0f) effectiveSpeed = 50f;

            transform.position = startPoint;
            if (target != start) transform.rotation = Quaternion.LookRotation((target - start).normalized);

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, startPoint);
            }
        }

        private void Update()
        {
            traveled += effectiveSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(traveled / Mathf.Max(0.01f, travelDistance));

            transform.position = Vector3.Lerp(startPoint, targetPoint, t);

            if (lineRenderer != null)
            {
                Vector3 tailPoint = Vector3.Lerp(startPoint, targetPoint, Mathf.Max(0f, t - 0.08f));
                lineRenderer.SetPosition(0, tailPoint);
                lineRenderer.SetPosition(1, transform.position);
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}