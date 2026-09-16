using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(BoxCollider))]
public class SceneSecondEndTriggers : MonoBehaviour
{
    public PlayableDirector playable_director;

    void OnTriggerEnter()
    {
        playable_director.Play();
    }
}
