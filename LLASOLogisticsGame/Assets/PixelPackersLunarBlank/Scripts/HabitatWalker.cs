using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace PixelPackersLunarBlank
{
    public sealed class HabitatWalker : MonoBehaviour
    {
        CharacterController motor;Camera view;float yaw,pitch,fall;
        void Start(){motor=GetComponent<CharacterController>();view=GetComponentInChildren<Camera>();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void Update()
        {
            Vector2 move=Vector2.zero,look=Vector2.zero;bool sprint=false,jump=false,escape=false,click=false;
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k!=null){move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));sprint=k.leftShiftKey.isPressed;jump=k.spaceKey.wasPressedThisFrame;escape=k.escapeKey.wasPressedThisFrame;}
            if(Mouse.current!=null){look=Mouse.current.delta.ReadValue()*.1f;click=Mouse.current.leftButton.wasPressedThisFrame;}
#else
            move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));look=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2;sprint=Input.GetKey(KeyCode.LeftShift);jump=Input.GetKeyDown(KeyCode.Space);escape=Input.GetKeyDown(KeyCode.Escape);click=Input.GetMouseButtonDown(0);
#endif
            if(escape){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}if(click){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            yaw+=look.x;pitch=Mathf.Clamp(pitch-look.y,-80,80);transform.localRotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            if(motor.isGrounded){fall=-1;if(jump)fall=2.5f;}else fall-=1.62f*Time.deltaTime;
            motor.Move((transform.TransformDirection(new Vector3(move.x,0,move.y).normalized)*(sprint?7:4)+Vector3.up*fall)*Time.deltaTime);
        }
        void OnGUI(){GUI.Box(new Rect(18,18,480,56),"LUNAR DOME / WALKTHROUGH\nWASD move | Mouse look | Shift sprint | Space jump | Esc cursor");}
    }
}
