using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DraggableObject : MonoBehaviour
{
    public LayerMask mask;
    public float radius;

    private Rigidbody rb;
    private float epsilon = 0.01f;

    private float initialDistance;
    private Vector3 initialScale;
    private float initialRadius;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnMouseDown()
    {
        rb.isKinematic = true;

        initialDistance = Vector3.Distance(transform.position, Camera.main.transform.position);
        initialScale = transform.localScale;
        initialRadius = radius;
    }

    private void OnMouseUp()
    {
        rb.isKinematic = false;
    }

    private void OnMouseDrag()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(cameraRay, out hitInfo, Mathf.Infinity, mask))
        {
            Vector3 target = hitInfo.point + hitInfo.normal * epsilon; // push a little to detect walls correctly

            Ray[] rays =
                {
                new Ray(target, transform.up),
                new Ray(target, transform.up * -1),
                new Ray(target, transform.right),
                new Ray(target, transform.right * -1),
                new Ray(target, transform.forward),
                new Ray(target, transform.forward * -1)
                };
            RaycastHit secondHitInfo;

            foreach (Ray ray in rays)
            {
                if (Physics.Raycast(ray, out secondHitInfo, radius, mask))
                {
                    target += (radius - secondHitInfo.distance) * secondHitInfo.normal;
                }
            }

            transform.position = target;

            float scaleFactor = Vector3.Distance(transform.position, Camera.main.transform.position) / initialDistance;
            transform.localScale = initialScale * scaleFactor;
            radius = initialRadius * scaleFactor;
        }
    }
}