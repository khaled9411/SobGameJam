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

        [SerializeField] private TrailRenderer trail;

        private ChargerGameInput inputActions;
        private Vector2 currentDelta;
        private bool isGameActive = false;

        private void Awake()
        {
            inputActions = new ChargerGameInput();

            inputActions.ChargerGame.MouseMove.performed += OnMouseMove;
            inputActions.ChargerGame.MouseMove.canceled += OnMouseMove;
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
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
            if (!isGameActive)
            {
                currentDelta = Vector2.zero;
                return;
            }

            currentDelta = context.ReadValue<Vector2>();
        }

        private void Update()
        {
            if (!isGameActive) return;

            // Move the plug based on mouse delta
            if (currentDelta.sqrMagnitude > 0)
            {
                transform.position += (Vector3)(currentDelta * moveSpeed * Time.deltaTime);
                UpdateVisuals();
                // Also reset delta after processing it to avoid continuous movement 
                // if the mouse stops but no new event is fired.
                currentDelta = Vector2.zero;
            }
        }
        void UpdateVisuals()
        {
            transform.right = Vector3.MoveTowards(transform.right, currentDelta, rotationSpeed * Time.deltaTime);
        }
        public void SetActive(bool active)
        {
            isGameActive = active;
        }

        public void SetPlugSize(float size)
        {   
            transform.localScale = transform.localScale* size;

            // setup the trail
            if (trail != null)
            {
                // the trail is smaller
                trail.startWidth = size - size* .4f;
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
