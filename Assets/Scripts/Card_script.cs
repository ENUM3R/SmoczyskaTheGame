using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmoczyskaGame
{
    [CreateAssetMenu(fileName = "New Card", menuName = "Card")]
    public class Card : ScriptableObject
    {
        public string cardName;
        public CardType cardType;
        public int value;
        public Sprite cardSprite;
        public enum CardType
        {
            Normal,
            Special
        }
    }
}
