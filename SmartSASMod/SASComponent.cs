using UnityEngine;
using SFS.World;

namespace SmartSASMod
{
    public class SASComponent : MonoBehaviour
    {
        public DirectionMode currentDirection = DirectionMode.Default;
        public SelectableObject previousTarget;
    }
}