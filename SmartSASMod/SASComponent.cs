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
                DirectionMode prev = direction;
                direction = value;
                if (IsPlayer && prev != direction)
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
                float prev = offset;
                offset = value;
                if (IsPlayer && !Mathf.Approximately(prev, offset))
                    GUI.OnOffsetChange(this);
            }
        }
    }
}