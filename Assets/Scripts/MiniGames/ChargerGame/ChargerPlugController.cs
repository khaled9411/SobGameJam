using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
namespace SobGameJam.MiniGames.ChargerGame
{
    public class ChargerPlugController : MonoBehaviour
    {
        public Action OnWallHit;
        public Action OnSocketReached;
        [Tooltip("Speed multiplier for mouse delta input.")]
        [SerializeField] private float moveSpeed = 0.05f;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private AudioSource movesound;
        [SerializeField] private TrailRenderer trail;
        private ChargerGameInput inputActions;
        private Vector2 currentDelta;
        private bool isGameActive = false;

        // NEW: gates movement until the player clicks the plug once.
        // isGameActive still controls whether the round is "live" at all (win/loss etc.),
        // but hasBeenClicked additionally controls whether movement input is actually applied.
        private bool hasBeenClicked = false;

        private float lastplaysound;
        private void Awake()
        {
            inputActions = new ChargerGameInput();
            inputActions.ChargerGame.MouseMove.performed += OnMouseMove;
            inputActions.ChargerGame.MouseMove.canceled += OnMouseMove;
        }
        private void OnEnable()
        {
            inputActions.Enable();
            Application.targetFrameRate = 120;
        }
        private void OnDisable()
        {
            inputActions.Disable();
            Application.targetFrameRate = -1;
        }
        private void OnDestroy()
        {
            if (inputActions != null)
            {
                inputActions.ChargerGame.MouseMove.performed -= OnMouseMove;
                inputActions.ChargerGame.MouseMove.canceled -= OnMouseMove;
                inputActions.Dispose();
            }
        }
        private void OnMouseMove(InputAction.CallbackContext context)
        {
            if (!isGameActive || !hasBeenClicked) // NEW: also require the initial click
            {
                currentDelta = Vector2.zero;
                return;
            }
            currentDelta = context.ReadValue<Vector2>();
        }
        private void Update()
        {
            if (!isGameActive)
            {
                movesound.Stop();
                return;
            }

            // NEW: while waiting for the player to click the plug, check every frame
            // for a left-click landing on this object's collider. Uses Physics2D.OverlapPoint
            // instead of OnMouseDown so no Physics2DRaycaster setup is needed on the camera.
            if (!hasBeenClicked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                    Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
                    if (hit != null && hit.gameObject == gameObject)
                    {
                        hasBeenClicked = true;

                        // Lock/hide the cursor only now, once movement actually begins,
                        // so the player can see the cursor clearly to click the plug first.
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
                return; // don't process movement until the click above has happened
            }

            // Move the plug based on mouse delta
            if (currentDelta.sqrMagnitude > 0)
            {
                if (!movesound.isPlaying)
                {
                    movesound.Play();
                }
                transform.position += (Vector3)(currentDelta * moveSpeed * Time.deltaTime);
                UpdateVisuals();
                // Also reset delta after processing it to avoid continuous movement 
                // if the mouse stops but no new event is fired.
                currentDelta = Vector2.zero;
                lastplaysound = Time.time;
            }
            else
            {
                if (Time.time - lastplaysound > .2)
                {
                    movesound.Stop();
                }
            }
        }
        void UpdateVisuals()
        {
            transform.right = Vector3.MoveTowards(transform.right, currentDelta, rotationSpeed * Time.deltaTime);
        }
        public void SetActive(bool active)
        {
            isGameActive = active;
            if (active)
            {
                hasBeenClicked = false; // NEW: require a fresh click every time the plug is (re)activated for a new round
            }
        }
        public void SetPlugSize(float size)
        {
            transform.localScale = transform.localScale * size;
            // setup the trail
            if (trail != null)
            {
                // the trail is smaller
                trail.startWidth = size - size * .4f;
                trail.endWidth = size - size * .4f;
            }
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isGameActive) return;
            Debug.Log("hello");
            if (collision.CompareTag("Wall"))
            {
                OnWallHit?.Invoke();
            }
            else if (collision.TryGetComponent<ChargerSocketGoal>(out _))
            {
                OnSocketReached?.Invoke();
            }
        }

    }
}