using UnityEngine;

namespace FriendOfOurs.Scenes
{
    public sealed class AdditiveSceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;
    }
}
