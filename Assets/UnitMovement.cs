using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class UnitMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public LayerMask ground = 1;
    public float raycastDistance = 100f;
    private Camera cam;
    private NavMeshAgent agent;
    private Mouse mouse;

    private void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        mouse = Mouse.current;


    }
    private void Update()
    {
        if (mouse.rightButton.wasPressedThisFrame)
        {

            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, ground))
            {
                // Check if the hit point is on the NavMesh
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hit.point, out navHit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);

                }
            }
        }
    }
}

