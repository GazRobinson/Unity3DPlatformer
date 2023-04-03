using UnityEngine;
using TMPro;
namespace GaRo.Debug {
    public class PlayerDebugInfo : MonoBehaviour
    {
        private PlatformPlayerController Player;
        private TextMeshProUGUI Text;

        // Start is called before the first frame update
        void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlatformPlayerController>();
            if(Player == null)
			{
                UnityEngine.Debug.LogError("Player was not found by the Debug Info. Player should be tagged as 'Player'");
                enabled = false;
			}
            Text = GetComponent<TextMeshProUGUI>();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            PlayerState State = Player.GetStateInfo;
            string DebugString = "Velocity: " + State.Velocity + "\n";
            DebugString += "Final Velocity: " + State.FinalVelocity + "\n";
            DebugString += "Forward Velocity: " + State.ForwardVelocity + "\n";
            DebugString += "Side Velocity: " + State.SideVelocity + "\n\n";
            DebugString += "Input Direction: " + State.InputDirection + "\n";
            DebugString += "Jump Held: " + State.JumpHeld + "\n\n";
            DebugString += "Is Grounded: " + State.IsGrounded + "\n";
            DebugString += "Is Sliding: " + State.IsSliding + "\n";

            Text.text = DebugString;
        }
    }
}
