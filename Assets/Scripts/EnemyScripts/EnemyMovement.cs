using System.Collections;
using UnityEngine;

namespace EnemyScripts
{
    public class EnemyMovement : MonoBehaviour
    {
        public CharacterController characterController;
        public Animator animator;

        public float groundSpeed = 15f;
        public float groundAcceleration = 100f;
        public float airAcceleration = 25f;
        public float maxAirSpeed = 15f;
        public float gravity = -35f;
        public float jumpHeight = 3f;
        public float rotationSpeed = 10f;
        public bool rotationLocked = false;

        private Vector3 velocity;
        private bool isGrounded;
        private Vector3 horizontalVelocity = Vector3.zero;
        private Vector3 inputDirection = Vector3.zero;

        public void SetInputDirection(Vector3 dir)
        {
            inputDirection = dir.normalized;
        }

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

        public void Jump()
        {
            if (characterController.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        
        public IEnumerator RotateEnemy(float degrees)
        {
            float startYaw = transform.eulerAngles.y;
            float targetYaw = startYaw + degrees;
            float currentYaw = startYaw;

            while (!Mathf.Approximately(currentYaw, targetYaw))
            {
                currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, 150f * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                yield return null;
            }
        }

        void Update()
        {
            if (horizontalVelocity.magnitude < 0.05f)
            {
                horizontalVelocity = Vector3.zero;
            }
            
            if (animator != null)
            {
                // Sprawdzamy czy wróg się porusza (czyli czy ma istotną prędkość poziomą)
                bool isMoving = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z).sqrMagnitude > 0.01f;
                animator.SetBool("isMoving", isMoving);
                animator.SetBool("isShooting", !isMoving);
            }
            isGrounded = characterController.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (isGrounded)
            {
                if (inputDirection != Vector3.zero)
                {
                    horizontalVelocity = Accelerate(horizontalVelocity, inputDirection, groundSpeed, groundAcceleration);
                }
                else
                {
                    horizontalVelocity = Vector3.zero;
                }
            }
            else
            {
                if (inputDirection != Vector3.zero)
                {
                    horizontalVelocity = Accelerate(horizontalVelocity, inputDirection, maxAirSpeed, airAcceleration);

                }
                else
                {
                    horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * 1.5f);
                }
            }

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
            
            if (!rotationLocked && horizontal.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
}