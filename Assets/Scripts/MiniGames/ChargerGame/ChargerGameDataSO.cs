using UnityEngine;
using UnityEngine.Tilemaps;

namespace SobGameJam.MiniGames.ChargerGame
{
    [CreateAssetMenu(fileName = "ChargerGameData", menuName = "MiniGames/ChargerGame/ChargerGameData", order = 1)]
    public class ChargerGameDataSO : ScriptableObject
    {
        [Header("Maze Generation")]
        [Tooltip("If true, the maze will use the Fixed Maze Seed every time.")]
        public bool isFixedMaze = false;
        
        [Tooltip("Seed used when Is Fixed Maze is true.")]
        public int fixedMazeSeed = 12345;

        [Tooltip("Base width of the maze grid (number of cells).")]
        public int baseGridWidth = 10;
        
        [Tooltip("Base height of the maze grid (number of cells).")]
        public int baseGridHeight = 10;
        
        [Tooltip("Size of each grid cell in world units.")]
        public float cellSize = 1f;

        [Header("Difficulty Curves (Evaluate based on Round Number)")]
        
        [Tooltip("Determines the time limit in seconds for a given round.")]
        public AnimationCurve timeLimitCurve = new AnimationCurve(
            new Keyframe(1, 10f), 
            new Keyframe(10, 5f), 
            new Keyframe(20, 3f)
        );

        [Tooltip("Determines the radius of the charger plug's collider for a given round. Larger is harder.")]
        public AnimationCurve plugSizeCurve = new AnimationCurve(
            new Keyframe(1, 0.2f),
            new Keyframe(10, 0.3f),
            new Keyframe(20, 0.4f)
        );

        [Header("Visuals / References")]
        [Tooltip("Prefab for the wall (should have a BoxCollider2D and SpriteRenderer).")]
        public GameObject wallPrefab;
        
        [Tooltip("Prefab for the floor (optional, for visual background).")]
        public GameObject floorPrefab;
        
        [Tooltip("Prefab for the Charger Plug player character.")]
        public GameObject plugPrefab;
        
        [Tooltip("Prefab for the Target Socket.")]
        public GameObject socketPrefab;
    }
}
