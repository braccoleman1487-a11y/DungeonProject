using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace  GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private float _MoveSpeed = 10;
        [SerializeField] private float _DashDistance = 2.5f;
        [SerializeField] private float _DashCooldown = 1.5f;


        [Tooltip("The minimum distance to the destination before we start using the pathfinder")]
        [SerializeField] private float _MinimumPathDistance = 6f;

        [Header("Connections")]
        [SerializeField] private Transform _GraphicsRoot;
        private Rigidbody _rigidbody;
        private InputAction _moveAction;
        private InputAction _dashAction;
        private Vector3 _moveVector;

        private bool _performDash;
        private bool _isDashing;

        List<PathNode> _path;

        [SerializeField]LevelManager _levManager;

        float minDistance = 6f;

        private Vector3 _targetLocation;
        Vector3 _floatingTarget;

        Vector3 _velocity;

        float __MaxSpeed = 5f;

        float _stoppingDistance = 1.5f;

        float __turnRate = 10f;

        
        float sensX;
        float sensY;
        Transform orientation;

        float xRotation;
        float yRotation;
   
        private void Awake()
        {
            // Assign member variables
            _rigidbody = GetComponent<Rigidbody>();

         
         
        }

      




      
        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime *sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

            yRotation += mouseX;
            xRotation += mouseY;

            xRotation = Mathf.Clamp(xRotation, -90, 90);

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);

        }
        private void Start()
        {
            // If we didn't manually set the level manager, find it
            if (_levManager == null) _levManager = FindAnyObjectByType<LevelManager>();
            Cursor.lockState = CursorLockMode.Locked;   
        }

        private void FixedUpdate()
        {
           
        }

        public void SetDestinationTarget(Vector3 targetLocation)
        {
         
        }

       
       
        


       
    }
}
