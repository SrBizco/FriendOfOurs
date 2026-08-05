using System;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingMoney;

        public event Action<int> MoneyChanged;

        public int Money { get; private set; }

        private void Awake()
        {
            Money = startingMoney;
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Money += amount;
            MoneyChanged?.Invoke(Money);
        }
    }
}
