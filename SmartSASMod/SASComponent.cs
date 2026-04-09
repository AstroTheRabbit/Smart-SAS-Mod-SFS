using SFS.World;
using UnityEngine;

namespace SmartSASMod
{
    public class SASComponent : MonoBehaviour
    {
        private DirectionMode direction = DirectionMode.Default;
        private float offset = 0;
        
        private bool IsPlayer => PlayerController.main.player.Value == GetComponent<Rocket>();
        
        public SelectableObject Target { get; set; }

        public DirectionMode Direction
        {
            get => direction;
            set
            {
                direction = value;
                if (IsPlayer)
                {
                    GUI.OnDirectionChange(this);
                }
            }
        }

        public float Offset
        {
            get => offset;
            set
            {
                offset = value;
                if (IsPlayer)
                {
                    GUI.OnOffsetChange(this);
                }
            }
        }
    }
}