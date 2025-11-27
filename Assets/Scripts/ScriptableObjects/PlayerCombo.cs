using System;
using UnityEngine;

namespace Utilities.Combat.Attacks.Combos
{
    [Serializable]
    [CreateAssetMenu(fileName = "Player Combo", menuName = "Attacks/Player Combo")]
    public class PlayerCombo : ScriptableObject
    {
        private enum AttackCategory
        {
            Single,
            AOE,
            Aerial
        }
        private enum AttackWeight
        {
            Light,
            Heavy
        }

        [SerializeField, Tooltip("Type of attack, single target or area of effect")]
        private AttackCategory _attackType = AttackCategory.Single;
        [SerializeField, Tooltip("Weight of the attack, light or heavy")]
        private AttackWeight _attackWeight = AttackWeight.Light;

        // Combines the two private enums into one public enum for easier use
        // uses a switch expression to return the correct AttackType based on the private enums selected
        public AttackType attackType
        {
            get
            {
                switch (_attackWeight, _attackType)
                {
                    case (AttackWeight.Light, AttackCategory.Single):
                        return AttackType.LightSingle;

                    case (AttackWeight.Light, AttackCategory.AOE):
                        return AttackType.LightAOE;

                    case (AttackWeight.Light, AttackCategory.Aerial):
                        return AttackType.LightAerial;

                    case (AttackWeight.Heavy, AttackCategory.Single):
                        return AttackType.HeavySingle;

                    case (AttackWeight.Heavy, AttackCategory.AOE):
                        return AttackType.HeavyAOE;

                    case (AttackWeight.Heavy, AttackCategory.Aerial):
                        return AttackType.HeavyAerial;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private set { }
        }

        [SerializeField]
        private string _comboName = "New Combo";
        public string comboName { get => _comboName != "" ? _comboName : this.name; }

        [SerializeField, Tooltip("Attacks included in this combo sequence. The order does matter and reflects the order of the attacks")]
        private PlayerAttack[] _comboAttacks;

        public PlayerAttack GetAttackOfIndex(int index)
        {
            if (_comboAttacks == null || index < 0 || index >= _comboAttacks.Length)
            {
                Debug.LogWarning($"Invalid attack index: {index}");
                return null;
            }
            return _comboAttacks[index];
        }
    }
}