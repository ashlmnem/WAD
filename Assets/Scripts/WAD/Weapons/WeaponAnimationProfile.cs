using System.Collections.Generic;
using UnityEngine;

namespace WAD.Weapons
{
    [CreateAssetMenu(fileName = "WeaponAnimationProfile_", menuName = "WAD/Weapon Animation Profile")]
    public class WeaponAnimationProfile : ScriptableObject
    {
        [Header("Arm Motion")]
        public AnimationCurve swayCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float swayAmount = 0.02f;
        public float bobAmount = 0.02f;
        public float idleAmplitude = 0.01f;
        public float idleFrequency = 0.5f;

        [Header("Weapon Motion")]
        public Vector3 weaponIdlePosition = new Vector3(0f, 0.002f, 0f);
        public Vector3 weaponIdleRotation = new Vector3(0.15f, 0.2f, 0.1f);
        public Vector3 weaponMovementPosition = new Vector3(0f, 0.012f, 0f);
        public Vector3 weaponMovementRotation = new Vector3(0.8f, 0f, 0.6f);
        [Min(0.01f)] public float weaponMotionFrequency = 1.8f;
        [Range(0f, 1f)] public float adsMotionDampening = 0.75f;

        [Header("Randomized Fire and Reload States")]
        [Tooltip("Assign clips that are also states in the weapon Animator Controller. One valid clip is selected per shot.")]
        public List<AnimationClip> firingAnimations = new List<AnimationClip>();
        [Tooltip("Assign clips that are also states in the weapon Animator Controller. One valid clip is selected per reload.")]
        public List<AnimationClip> reloadAnimations = new List<AnimationClip>();
    }
}
