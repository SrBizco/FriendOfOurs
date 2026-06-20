using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public static class AttackHitVolume
    {
        public static Vector3 GetCenter(Vector3 origin, Vector3 forward, float heightOffset, float range)
        {
            Vector3 planarForward = new Vector3(forward.x, 0f, forward.z);
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            return origin + Vector3.up * heightOffset + planarForward.normalized * range;
        }
    }
}
