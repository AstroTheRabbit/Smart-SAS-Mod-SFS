using System.Collections.Generic;
using System.Globalization;
using SFS.World;
using UITools;
using UnityEngine;
using Button = SFS.UI.ModGUI.Button;

namespace SmartSASMod
{
    public class SASComponent : MonoBehaviour
    {
        private DirectionMode direction = DirectionMode.Default;
        private float offset;

        public DirectionMode Direction
        {
            get => direction;
            set
            {
                DirectionMode prev = direction;
                direction = value;
                if (prev != direction && IsCurrentRocket())
                    OnDirectionChange();
            }
        }

        public float Offset
        {
            get => offset;
            set
            {
                float prev = offset;
                offset = value;
                if (!Mathf.Approximately(prev, offset) && IsCurrentRocket())
                    OnOffsetChange();
            }
        }

        public SelectableObject Target { get; set; }

        private bool IsCurrentRocket() => PlayerController.main.player.Value == GetComponent<Rocket>();

        public void OnDirectionChange()
        {
            foreach (KeyValuePair<DirectionMode, Button> kvp in GUI.buttons)
            {
                kvp.Value.SetSelected(kvp.Key == direction);
            }
        }

        public void OnOffsetChange()
        {
            GUI.angleInput.Text = offset.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}