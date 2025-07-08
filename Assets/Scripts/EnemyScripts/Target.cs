using UnityEngine;

namespace EnemyScripts
{
    public class Target : MonoBehaviour
    {
        public float hp = 50;

        public void TakeDamage(float amount)
        {
            hp -= amount;
            if (hp <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            Destroy(gameObject);
            GameManager.Instance.enemyCounter--;
        }
    }
}