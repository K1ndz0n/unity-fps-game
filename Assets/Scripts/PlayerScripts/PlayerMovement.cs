using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController characterController;

    public float groundSpeed = 15f;
    public float groundAcceleration = 100f;
    public float airAcceleration = 25f;
    public float maxAirSpeed = 15f;
    public float gravity = -35f;
    public float jumpHeight = 3f;

    private Vector3 velocity;
    private bool isGrounded;

    private Vector3 horizontalVelocity = Vector3.zero;
    
    private Vector3 Accelerate(Vector3 currentVelocity, Vector3 wishDir, float maxSpeed, float accel)
    {
        float currentSpeed = Vector3.Dot(currentVelocity, wishDir);
        float addSpeed = maxSpeed - currentSpeed;

        if (addSpeed <= 0)
        {
            return currentVelocity;
        }
        
        float accelSpeed = accel * Time.deltaTime;
        
        if (accelSpeed > addSpeed)
        {
            accelSpeed = addSpeed;
        }
        
        return currentVelocity + wishDir * accelSpeed;
    }


    private void Update()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
            

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;

        if (isGrounded)
        {
            // ruch na ziemi
            if (inputDir != Vector3.zero)
            {
                horizontalVelocity = Accelerate(horizontalVelocity, inputDir, groundSpeed, groundAcceleration);
            }
            else
            {
                // Natychmiastowe zatrzymanie na ziemi
                horizontalVelocity = Vector3.zero;
            }
            
            // Korekta kierunku, gdy gracz nie trzyma inputu w bok
            if (inputDir != Vector3.zero)
            {
                // Korekta kierunku przy czystym chodzeniu do przodu
                if (x == 0 && z != 0)
                {
                    Vector3 forwardDir = transform.forward * Mathf.Sign(z); // z = 1 (W), -1 (S)
                    horizontalVelocity = Vector3.RotateTowards(horizontalVelocity, forwardDir * groundSpeed, Time.deltaTime * 5f, 0f);
                }

                // Korekta kierunku przy czystym chodzeniu w bok
                if (z == 0 && x != 0)
                {
                    Vector3 rightDir = transform.right * Mathf.Sign(x); // x = 1 (D), -1 (A)
                    horizontalVelocity = Vector3.RotateTowards(horizontalVelocity, rightDir * groundSpeed, Time.deltaTime * 5f, 0f);
                }
            }
        }
        else
        {
            // ruch w powiertrzu
            if (inputDir != Vector3.zero)
            {
                horizontalVelocity = Accelerate(horizontalVelocity, inputDir, maxAirSpeed, airAcceleration);
            }
            else
            {
                // delikatne hamowanie w powietrzu, jeśli nie ma inputu
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * 1.5f);
            }
        }

        // Skok
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Grawitacja
        velocity.y += gravity * Time.deltaTime;
        Vector3 move = horizontalVelocity;
        move.y = velocity.y;
        
        Vector3 horizontal = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z);
        if (horizontal.magnitude > groundSpeed)
        {
            horizontal = horizontal.normalized * groundSpeed;
            horizontalVelocity = new Vector3(horizontal.x, horizontalVelocity.y, horizontal.z);
        }
        
        characterController.Move(move * Time.deltaTime);
    }
}