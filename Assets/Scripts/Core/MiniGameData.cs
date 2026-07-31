using UnityEngine;

namespace SobGameJam.Core
{
    /// <summary>
    /// Metadata for a specific mini-game.
    /// The GameManager uses this to know what scene to load and how to introduce it.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMiniGameData", menuName = "MiniGames/MiniGame Data")]
    public class MiniGameData : ScriptableObject
    {
        [Tooltip("The name of the scene to load additively. MUST match exactly.")]
        public string sceneName;

        [Tooltip("The text prompt to show the player before the game starts (e.g., 'JUMP!', 'DODGE!').")]
        public string instructionPrompt;

        [Tooltip("How long the instruction prompt should stay on screen before starting the game.")]
        public float instructionDuration = 1.5f;
    }
}
