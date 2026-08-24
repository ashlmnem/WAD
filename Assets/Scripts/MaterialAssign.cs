using UnityEngine;

public class AssignMaterial : MonoBehaviour
{
    public Material material;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = material;
        }
    }
}
