using UnityEngine;

namespace EnemyScripts
{
    public class Hitbox : MonoBehaviour
    {
        public Target target;
        public float damageMultiplier = 1f;

        public void Hit(float baseDamage)
        {
            float totalDamage = baseDamage * damageMultiplier;
            target.TakeDamage(totalDamage);
        }
    }
}