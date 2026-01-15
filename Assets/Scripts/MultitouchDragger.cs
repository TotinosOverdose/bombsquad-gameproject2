using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;

// See Unity docs on EnhancedTouch: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html
// and Samyam mini-tutorial (relevant section starts after 20 mins): https://youtu.be/4MOOitENQVg?si=0RqKa3K0nIEf3YjV&t=1229
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class MultitouchDragger : MonoBehaviour {
    Camera cam;
    [SerializeField] LayerMask groundMask;
    
    Dictionary<Finger, Draggable> activeDrags = new Dictionary<Finger, Draggable>();

    void Awake() {
        EnhancedTouchSupport.Enable();
        cam = Camera.main;
    }

    Draggable TryFindDraggableUnder(Touch touch) {
        Draggable result = null;
        var ray = cam.ScreenPointToRay(touch.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            result = hit.collider.GetComponent<Draggable>();
        }
        return result;
    }

    Vector3 GroundPositionUnder(Touch touch) {
        var ray = cam.ScreenPointToRay(touch.screenPosition);
        if (Physics.Raycast(ray,
                            out RaycastHit hit,
                            Mathf.Infinity,
                            groundMask)) {
            return hit.point;
        }
        return Vector3.zero; // ew!
    }

    void Update() {
        foreach (var touch in Touch.activeTouches) {
            if (touch.began) { // same as touch.phase == TouchPhase.Began
                print("Touching");
                //var newDraggable = TryFindDraggableUnder(touch);
                var point = cam.ScreenToWorldPoint(touch.screenPosition);
                var collider = Physics2D.OverlapPoint(point); // TODO: layermask
                if (collider) {
                    var newDraggable = collider.GetComponent<Draggable>();
                    if (newDraggable)
                    {
                        activeDrags.Add(touch.finger, newDraggable);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved) {
                if (activeDrags.ContainsKey(touch.finger)) {
                    var point = cam.ScreenToWorldPoint(touch.screenPosition);
                    point.z = 0;
                    activeDrags[touch.finger].transform.position = point;
                }
            }
            else if (touch.ended) {
                if (activeDrags.ContainsKey(touch.finger)) {
                    activeDrags.Remove(touch.finger);
                }
            }
        }
        // if (Mouse.current.leftButton.wasPressedThisFrame) {
        //     print("foo");
        //     
        // }
    }
}