using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 70f;
    public Transform playerBody;

    private float mouseXTotal = 0f;
    private float mouseYTotal = 0f;
    private float currentRecoilX = 0f;
    private float currentRecoilY = 0f;

    public float recoilReturnSpeed = 10f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        mouseYTotal -= mouseY;
        mouseXTotal += mouseX;
        
        mouseYTotal = Mathf.Clamp(mouseYTotal, -90f, 90f);
        
        currentRecoilX = Mathf.Lerp(currentRecoilX, 0f, recoilReturnSpeed * Time.deltaTime);
        currentRecoilY = Mathf.Lerp(currentRecoilY, 0f, recoilReturnSpeed * Time.deltaTime);
        
        transform.localRotation = Quaternion.Euler(mouseYTotal + currentRecoilX, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, mouseXTotal + currentRecoilY, 0f);
    }

    public void AddRecoil(float xAmount, float yAmount)
    {
        currentRecoilX -= xAmount;
        currentRecoilY += yAmount;
    }
    
    public void SetRecoilRecoverySpeed(float speed)
    {
        recoilReturnSpeed = speed;
    }
}