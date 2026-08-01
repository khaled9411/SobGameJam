using System.Collections.Generic;
using UnityEngine;
using SobGameJam.MiniGames;

namespace SobGameJam.MiniGames.WireCut
{
    public enum WireColor { Red, Blue, Yellow, Green, White }

    [CreateAssetMenu(menuName = "MiniGames/WireCut/Clue")]
    public class WireClueSO : ScriptableObject
    {
        [TextArea] public string clueText;
        public WireColor answerColor;
        public int difficultyTier; // 1 = direct, 2 = indirect, 3 = hardest
    }
}