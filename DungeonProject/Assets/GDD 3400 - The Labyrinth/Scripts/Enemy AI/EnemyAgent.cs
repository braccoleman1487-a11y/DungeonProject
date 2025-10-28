using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
namespace GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAgent : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;
        bool isMovingToTarget = false;
        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        [SerializeField] private float _TurnRate = 10f;
        [SerializeField] private float _MaxSpeed = 5f;
        [SerializeField] private float _SightDistance = 25f;

        [SerializeField] private float _StoppingDistance = 1.5f;

        [Tooltip("The distance to the destination before we start leaving the path")]
        [SerializeField] private float _LeavingPathDistance = 2f; // This should not be less than 1

        [Tooltip("The minimum distance to the destination before we start using the pathfinder")]
        [SerializeField] private float _MinimumPathDistance = 6f;

        bool valuableMissing = false;

        public enum EnemyStateM
        {
            passive,
            suspicious,
            hostile
        }

        //the speed at which the suspicion is increased
        public float suspicionTime = 0.8f;
        [SerializeField] private float _visionRange = 20f;
        [SerializeField] private float _visionAngle = 60f; // Field of view angle


        private float _suspicionTimer = 0;
        private Transform _player;
        Collider[] targets = new Collider[10];
        public LayerMask targetLayer;

        //because why not
        float susValue = 0;

        //the needed suspision to enter suspicious state 
        float neededSus = 40;

        //the needed suspicion to enter hostile state. The timer will increase every 10 seconds when it is in suspicious state.
        float neededHostile = 75;


        bool canSeePlayer;

        Vector3 lastKnownLocation = Vector3.zero;

        private Vector3 _velocity;
        private Vector3 _floatingTarget;
        private Vector3 _destinationTarget;
        List<PathNode> _path;

        private Rigidbody _rb;

        private LayerMask _wallLayer;

        private bool DEBUG_SHOW_PATH = true;

        bool playerInRange = false;

        List<Vector3> valuableLocations = new List<Vector3>();



        EnemyStateM _currentMainState = EnemyStateM.passive;

        public void Awake()
        {
            // Grab and store the rigidbody component
            _rb = GetComponent<Rigidbody>();

            // Grab and store the wall layer
            _wallLayer = LayerMask.GetMask("Walls");
            _player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        public void Start()
        {
            // If we didn't manually set the level manager, find it
            if (_levelManager == null) _levelManager = FindAnyObjectByType<LevelManager>();

            // If we still don't have a level manager, throw an error
            if (_levelManager == null) Debug.LogError("Unable To Find Level Manager");

            FindValuableLocInScene();
        }

        private void FindValuableLocInScene()
        {
            List<GameObject> valuables = new List<GameObject>(GameObject.FindGameObjectsWithTag("Collectable"));
            if (valuables.Count == 0)
            {
                Debug.Log("No valuables found. Critical error");
            }
            else
            {
                foreach (GameObject valuable in valuables)
                {
                    //add the location of the valuable to the array
                    valuableLocations.Add(valuable.transform.position);       
                }

            }
        }

        public void Update()
        {
            if (!_isActive) return;

            Perception();
            DecisionMaking();
        }

        private void Perception()
        {
            canSeePlayer = false;
            int numTargets = Physics.OverlapSphereNonAlloc(transform.position, 25, targets, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
           
            if (numTargets > 0 && targets.Length > 0)
            {
                foreach (Collider target in targets)
                {
                    if (target.gameObject.CompareTag("Player"))
                    {
                        canSeePlayer = true;
                        return;
                    }
                }
            }



        }

        private void DecisionMaking()
        {
            switch (_currentMainState)
            {
                case EnemyStateM.passive:
                    PassiveBehavior();
                    break;
                case EnemyStateM.suspicious:
                    SuspiciousBehavior();
                    break;

                case EnemyStateM.hostile:
                    HostileBehavior();

                    break;
            }
            PathFollowing();


        }

        private void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Player")
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void HostileBehavior()
        {
            //double movement speed and switch to persuing the player. If we lost the player, then go to their last known location. Once the AI reaches the last known loc
            //it will decrease back to suspicious.
            throw new NotImplementedException();
        }

        private void SuspiciousBehavior()
        {
            //if we are far enough from the player and we can still see them, stop a bit away from the player.
            if (canSeePlayer)
            {
                if (Vector3.Distance(transform.position, _player.transform.position) >= _StoppingDistance)
                {
                    SetDestinationTarget(_player.transform.position);
                }
                else
                {
                    if (Vector3.Distance(transform.position, _player.transform.position) < 20)
                    {
                        //if the player gets too close to the enemy, go hostile
                        _currentMainState = EnemyStateM.hostile;
                        return;
                    }
                }
            }
            else
            {
                //we can't see the player, so we go back to patroling. The route will take the AI to all the valuables. Regardless if they exist or not.
                //if the AI gets to the location of the valuable and it is not there,
                //the AI will permanently enter the hostile phase.
                if (!isMovingToTarget)
                {
                    if (valuableMissing)
                    {
                        _currentMainState = EnemyStateM.hostile;
                    }
                    else
                    {
                        Vector3 randomValuableLocation = valuableLocations[UnityEngine.Random.Range(0, valuableLocations.Count - 1)];
                        SetDestinationTarget(randomValuableLocation);
                        isMovingToTarget = true;
                    }

                }


            }


        }

        private void PassiveBehavior()
        {
            if (canSeePlayer)
            {
                // Increase suspicion over time
                susValue += Time.deltaTime *suspicionTime;
                Debug.Log(susValue.ToString());
                if (susValue >= neededSus)
                {
                    Debug.Log("Enemy has become suspicious of the player");
                    _currentMainState = EnemyStateM.suspicious;
                    susValue = neededSus;
                    return;
                }
            }
            else
            {
                //slowly decrease suspicion if player not seen
                susValue -= Time.deltaTime / (suspicionTime * 2f);
                susValue = Mathf.Clamp(susValue, 0, neededSus);
            }
        
        }

        private GameObject ChooseRandomNode()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 100);
            List<GameObject> nodesInRange = new List<GameObject>();
            foreach (Collider hit in hits)
            {
                if (hit.gameObject.name == "PathNode" || hit.gameObject.name == "ExitNode")
                {
                    nodesInRange.Add(hit.gameObject);
                }
            }
            if (nodesInRange.Count == 0) return null;
            Debug.Log("found at least one node in range");
            return nodesInRange[UnityEngine.Random.Range(0, nodesInRange.Count - 1)];

        }

        #region Path Following

        private void PathFollowing()
        {

            if (_path != null)
            {
                //only follow a path if we have one
                int closestNodeIndex = GetClosestNode();
                PathNode targetNode = _path[closestNodeIndex];

                if (Vector3.Distance(transform.position, targetNode.transform.position) < _StoppingDistance)
                {
                    int nextNodeIndex = closestNodeIndex + 1;
                    if (nextNodeIndex < _path.Count)
                        targetNode = _path[nextNodeIndex];
                    else
                    {
                        // Reached end of path
                        _path = null;
                        isMovingToTarget = false;
                        //Check if near enough to a valuable and see if it is missing
                        DiscoverMissingValuables();
                        return;
                    }
                }

                _floatingTarget = targetNode.transform.position;
            }
            
        }

        private void DiscoverMissingValuables()
        {
            GameObject firstFoundValuable = CheckIfValuablesInRange();


        }

        GameObject CheckIfValuablesInRange()
        {
            return null;
        }

        // Public method to set the destination target
        public void SetDestinationTarget(Vector3 destination)
        {
           //storing the destination in a member variable
           _destinationTarget = destination;

            //if the straight line distance is greater than the minumum , lets do pathfinding
            if (Vector3.Distance(transform.position, _destinationTarget) > _MinimumPathDistance)
            {
                PathNode startNode = _levelManager.GetNode(transform.position);
                PathNode endNode = _levelManager.GetNode(destination);


                Debug.Log("pathfinding");
                //we couln't find a node close enough
                if (startNode == null || endNode==null) return;

                _path = Pathfinder.FindPath(startNode, endNode);

                StartCoroutine(DrawPathDebugLines(_path));
            }
            // othewise move directly to destination
            else
            {
                _floatingTarget = destination;
            }
            

        }

        // Get the closest node to the player's current position
        private int GetClosestNode()
        {
            int closestNodeIndex = 0;
            float closestDistance = float.MaxValue;
            if(_path!= null)
            {
                for (int i = 0; i < _path.Count; i++)
                {
                    float distance = Vector3.Distance(transform.position, _path[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNodeIndex = i;
                    }
                }
                return closestNodeIndex;
            }
            return -1;
           
        }

        #endregion

        #region Action
        private void FixedUpdate()
        {
            if (!_isActive) return;

            if (_floatingTarget != Vector3.zero && Vector3.Distance(transform.position, _floatingTarget) > _StoppingDistance)
            {
                Vector3 direction = (_floatingTarget - transform.position).normalized;
                _velocity = direction * _MaxSpeed;
            }
            else
            {
                _velocity *= 0.95f;
            }

            if (_velocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _TurnRate);
            }

            _rb.linearVelocity = _velocity;
        }
        #endregion

        private IEnumerator DrawPathDebugLines(List<PathNode> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].transform.position, path[i + 1].transform.position, Color.red, 3.5f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
