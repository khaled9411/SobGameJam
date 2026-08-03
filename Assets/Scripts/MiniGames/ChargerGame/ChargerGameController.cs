using SobGameJam.Events;
using System.Collections;
using UnityEngine;

namespace SobGameJam.MiniGames.ChargerGame
{
    public class ChargerGameController : MiniGameBase
    {

        [Header("Game Configuration")]
        [SerializeField] private ChargerGameDataSO gameData;
        [SerializeField] private FloatEventChannelSO OnTimeChangeEvent;
        
        [Header("Components")]
        [SerializeField] private ChargerMazeGenerator2D mazeGenerator;

        [Header("Camera Control")]
        [Tooltip("The camera for this mini-game. Falls back to Camera.main if empty.")]
        [SerializeField] private Camera miniGameCamera;
        [Tooltip("Extra space around the maze when framing it in the camera.")]
        [SerializeField] private float cameraPadding = 2f;

        private ChargerPlugController spawnedPlug;
        private GameObject spawnedSocket;
        private float currentTime;
        private Coroutine timerCoroutine;

        private Vector3 originalCameraPosition;
        private float originalCameraSize;
        private bool hasSavedCameraState = false;

        private float currentTimeLimit;
        protected override void OnGameStarted(int roundNumber)
        {
            if (gameData == null)
            {
                Debug.LogError("ChargerGameController: Missing ChargerGameDataSO!");
                LoseGame();
                return;
            }

            if (mazeGenerator == null)
            {
                mazeGenerator = gameObject.AddComponent<ChargerMazeGenerator2D>();
            }

            if (miniGameCamera == null) 
            {
                miniGameCamera = Camera.main;
            }

            // Save original camera state
            if (miniGameCamera != null && !hasSavedCameraState)
            {
                originalCameraPosition = miniGameCamera.transform.position;
                originalCameraSize = miniGameCamera.orthographicSize;
                hasSavedCameraState = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Evaluate Difficulty Curves
            currentTimeLimit = gameData.timeLimitCurve.Evaluate(roundNumber);
            float plugSize = gameData.plugSizeCurve.Evaluate(roundNumber);
            

            // Generate Maze
            mazeGenerator.GenerateMaze(gameData);

            // Frame the Camera to center the maze
            if (miniGameCamera != null)
            {
                float mazeWidth = gameData.baseGridWidth * 2f * gameData.cellSize;
                float mazeHeight = gameData.baseGridHeight * 2f * gameData.cellSize;
                
                // Center point of the generated maze
                Vector3 center = new Vector3(mazeWidth / 2f, mazeHeight / 2f, originalCameraPosition.z);
                
                // Calculate orthographic size to fit both width and height
                float sizeY = mazeHeight / 2f + cameraPadding;
                float sizeX = (mazeWidth / 2f + cameraPadding) / miniGameCamera.aspect;
                
                miniGameCamera.transform.position = center;
                miniGameCamera.orthographicSize = Mathf.Max(sizeY, sizeX);
            }

            // Spawn Plug (Player)
            if (gameData.plugPrefab != null)
            {
                GameObject plugObj = Instantiate(gameData.plugPrefab, mazeGenerator.startPosition, Quaternion.identity, transform);
                spawnedPlug = plugObj.GetComponent<ChargerPlugController>();
                
                if (spawnedPlug == null)
                {
                    Debug.LogWarning("Plug Prefab is missing ChargerPlugController. Adding one automatically.");
                    spawnedPlug = plugObj.AddComponent<ChargerPlugController>();
                }
                
                spawnedPlug.SetPlugSize(plugSize);
                spawnedPlug.OnWallHit += HandleWallHit;
                spawnedPlug.OnSocketReached += HandleSocketReached;
                
                spawnedPlug.SetActive(true);
            }
            else
            {
                Debug.LogError("ChargerGameController: Plug prefab is not assigned in game data!");
            }
            // Spawn indicator
            if(gameData.indicatorPrefab != null)
            {
                GameObject plugObj = Instantiate(gameData.indicatorPrefab, mazeGenerator.startPosition, Quaternion.identity, transform);
            }

            // Spawn Socket (Goal)
            if (gameData.socketPrefab != null)
            {
                spawnedSocket = Instantiate(gameData.socketPrefab, mazeGenerator.endPosition, Quaternion.identity, transform);
                
                if (spawnedSocket.GetComponent<ChargerSocketGoal>() == null)
                {
                    spawnedSocket.AddComponent<ChargerSocketGoal>();
                }
            }
            else
            {
                Debug.LogError("ChargerGameController: Socket prefab is not assigned in game data!");
            }

            // Start Timer
            currentTime = currentTimeLimit;
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }
            timerCoroutine = StartCoroutine(TimerRoutine());
        }

        private IEnumerator TimerRoutine()
        {
            while (currentTime > 0)
            {
                if (!isGameActive) yield break;

                currentTime -= Time.deltaTime;
                if (OnTimeChangeEvent != null)
                {
                    OnTimeChangeEvent.RaiseEvent(NormalizedRmainingTime());
                }
                // Optional: Update a timer UI here if there is one

                yield return null;
            }

            if (isGameActive)
            {
                Debug.Log("Time ran out!");
                HandleTimeOut();
            }
        }
        float NormalizedRmainingTime()
        {
            return currentTime / currentTimeLimit;
        }
        private void HandleWallHit()
        {
            if (!isGameActive) return;
            
            Debug.Log("Hit a wall! You lose.");
            StopGame();
            LoseGame();
        }

        private void HandleSocketReached()
        {
            if (!isGameActive) return;
            
            Debug.Log("Reached the socket! You win.");
            StopGame();
            WinGame();
        }
        
        private void HandleTimeOut()
        {
            if (!isGameActive) return;
            
            StopGame();
            LoseGame();
        }

        private void StopGame()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (spawnedPlug != null)
            {
                spawnedPlug.SetActive(false);
                spawnedPlug.OnWallHit -= HandleWallHit;
                spawnedPlug.OnSocketReached -= HandleSocketReached;
            }

            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            
            //// Restore Camera State when game ends
            //if (hasSavedCameraState && miniGameCamera != null)
            //{
            //    miniGameCamera.transform.position = originalCameraPosition;
            //    miniGameCamera.orthographicSize = originalCameraSize;
            //    hasSavedCameraState = false; // allow saving again if restarted
            //}
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopGame();
            if (mazeGenerator != null)
            {
                mazeGenerator.ClearMaze();
            }
        }
    }
}
