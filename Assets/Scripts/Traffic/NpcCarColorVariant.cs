using System;
using UnityEngine;

namespace FriendOfOurs.Traffic
{
    /// <summary>Assigns one shared body material when a civilian NPC car becomes active.</summary>
    public sealed class NpcCarColorVariant : MonoBehaviour
    {
        [Serializable]
        private struct MaterialTarget
        {
            [SerializeField] private Renderer renderer;
            [SerializeField, Min(0)] private int materialSlot;

            public void Apply(Material material)
            {
                if (renderer == null)
                {
                    return;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materialSlot < 0 || materialSlot >= materials.Length)
                {
                    return;
                }

                materials[materialSlot] = material;
                renderer.sharedMaterials = materials;
            }
        }

        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private MaterialTarget[] bodyMaterialTargets;

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                ApplyRandomColor();
            }
        }

        [ContextMenu("Apply Random Color")]
        public void ApplyRandomColor()
        {
            if (colorMaterials == null || colorMaterials.Length == 0 || bodyMaterialTargets == null)
            {
                return;
            }

            Material material = colorMaterials[UnityEngine.Random.Range(0, colorMaterials.Length)];
            if (material == null)
            {
                return;
            }

            for (int i = 0; i < bodyMaterialTargets.Length; i++)
            {
                bodyMaterialTargets[i].Apply(material);
            }
        }
    }
}
