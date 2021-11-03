# Superliminal Game Object Sizing Illusion Demo

  I will share my experience that I gained while trying to create object sizing feature from superliminal game in Unity. Since I am not an experienced game developer, I made a lot of mistakes. The main purpose of writing this article is sharing my mistakes, not to teach you how to make such demo. Besides, my solution may not be the best solution (most probably it is not). I learned many things while trying to create a working demo and had fun. Hope you enjoy.
  
  To clarify what is this demo and what I tried to achieve, I will make an introduction. Superliminal is a FPS puzzle game based on optic illusions. One of these illusions is that when you drag an object with mouse its scale according to your view does not change so it's real scale changes (it becomes larger when you put object further in order to seem unchanged in your perspective). My aim is to create this illusion in Unity.
  
## How Illusion Works

  I do not know the technique they used in Superliminal but I have an idea about how it works.   
  
  ```
  // The trick is placing objects close to surfaces where the player is
  // looking in order to change distance between player and object.
  // As player moves the mouse, distance between player and object changes
  // according to distance to surfaces. Then object is scaled amount of 
  // position change and rotated towards the player to be seen as unchanged 
  // according to player's point of view.
  
  if player is dragging an object:
    cast a ray from the player towards the object
    if it hits a collider:
      place the object according to hit point
      rotate the object towards the player
      scale the object by the distance change     
  ```
  
## Moving the object

  I set up test environment (FPS controls, player, room, draggable object). I added a script to object named DraggableObject. Firstly I wrote the movement code.
  
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;

      private void OnMouseDrag()
      {
          Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
          RaycastHit hitInfo;

          if (Physics.Raycast(cameraRay, out hitInfo, Mathf.Infinity, mask))
          {
              transform.position = hitInfo.point;
          }
      }
  }
  ```
  
## Intersection problem
  
  In order to prevent intersection I added offset variable.
  
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;
      public float offset;

      private void OnMouseDrag()
      {
          Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
          RaycastHit hitInfo;

          if (Physics.Raycast(cameraRay, out hitInfo, Mathf.Infinity, mask))
          {
              transform.position = hitInfo.point + hitInfo.normal * offset;
          }
      }
  }
  ```
  
  That did not solve the intersection problem at corners because it adds offset value for just one wall. After that, I thought that moving the object with `transform.Translate` and leaving collision problems to colliders & rigidbody may solve my problem. I changed my code according to this.
  
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;
      public float moveSpeed;

      private void OnMouseDrag()
      {
          Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
          RaycastHit hitInfo;

          if (Physics.Raycast(cameraRay, out hitInfo, Mathf.Infinity, mask))
          {
              Vector3 target = (hitInfo.point - transform.position).normalized;
              transform.Translate(target * Time.deltaTime * moveSpeed);
          }
      }
  }
  ```
  
  This version solved intersection issues, but the object was moving very slow when moveSpeed is low. Raising the value caused another problem. Since translate step value increased the object started vibrating. When translate step value is high it leaps into wall and then moves back because of colliders. After watching some tutorials about movement I decided to try `rigidbody.movePosition` instead of `transform.Translate`.
  
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;
      public float moveSpeed;
      Rigidbody rb;

      private void Start()
      {
          rb = GetComponent<Rigidbody>();
      }

      private void OnMouseDrag()
      {
          Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
          RaycastHit hitInfo;

          if (Physics.Raycast(cameraRay, out hitInfo, Mathf.Infinity, mask))
          {
              Vector3 direction = (hitInfo.point - transform.position).normalized;
              rb.MovePosition(transform.position + direction * Time.deltaTime * moveSpeed);
          }
      }
  }
  ```
  
  This method had same move speed problem too I mentioned above. Therefore I decided to return manipulating `transform.position` in order to get instant reaction. However, I had to solve intersection problem with the code. I found a way which is casting rays to six directions (up, down, right, left, forward, backward) and check is there a wall, if there is a wall push object according to normal of this wall.
  
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;
      public float radius;
      private float epsilon = 0.01f;

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
          }
      }
  }
  ```
  
## Scaling the object
  
  This idea worked well. It had some laggy movement problems but not all the time so it was okay for me. Next thing to do was scaling the object to create the illusion. Idea behind scaling was simple. Object was to be scaled by the distance change between player and object. I also added rigidbody to the object to add gravity.
  
  ```cs
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
  ```
  
## Rotating the object
  
  Moving and scaling was working okay. Final thing to be done was to rotate the object. I didn't want to make the object child of player to rotate according to player because the object will be affected by players movement also. First idea I tried was caching initial difference between `transform.forward` vectors of the object and camera and add this difference to camera's forward vector to calculate object's new forward vector.
    
  ```cs
  public class DraggableObject : MonoBehaviour
  {
      public LayerMask mask;
      public float radius;

      private Rigidbody rb;
      private float epsilon = 0.01f;

      private float initialDistance;
      private Vector3 initialScale;
      private float initialRadius;
      private Vector3 directionDifferenceBetweenCameraAndPlayer;

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
          directionDifferenceBetweenCameraAndPlayer = transform.forward - Camera.main.transform.forward;
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

              transform.forward = (Camera.main.transform.forward + directionDifferenceBetweenCameraAndPlayer).normalized;
          }
      }
  }
  ```  
 
  It did not work as I wanted and I don't know the reason yet :/. I realized that I should rotate the object just on y-axis. Thus, I tried another way to rotate the object without making it player's child object. I calculated angle difference between the object's forward vector and camera's forward vector on y-axis and add this difference to camera's forward vector to get objects new forward vector. But that did not work either.
  
## Final version
  
  Finally I decided to try making the object player's child object, but that also did not work as I wanted. Unfortunately, I can not explain rotating problem because I did not understand the source of the problem. Thus, I deleted rotation part. The final version is below.
  
  ```cs
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
  ```
  
  I am an inexperienced game developer and I tried to make this demo without getting much help to gain some experience. Therefore, it may be inefficient, it may have wrong ideas, but I hope it helps people trying to learn Unity.
 
