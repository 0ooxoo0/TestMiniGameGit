using UnityEngine;

public enum CardSuit
{
    Clubs,    // Трефы
    Diamonds, // Бубны
    Hearts,   // Червы
    Spades    // Пики
}

public enum CardRank
{
    Ace = 1,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King
}

[System.Serializable]
public class CardModel
{
    public CardSuit Suit;
    public CardRank Rank;

    // Адрес ресурса в Addressables (или ID)
    public string AddressKey;

    // Пометка, открыта карта (лицевая сторона) или нет
    public bool IsFaceUp;

    public CardModel(CardSuit suit, CardRank rank, string addressKey, bool isFaceUp = false)
    {
        Suit = suit;
        Rank = rank;
        AddressKey = addressKey;
        IsFaceUp = isFaceUp;
    }

    // Проверка, можно ли положить на эту карту другую (на 1 меньше или на 1 больше)
    // Тузы (Ace) считаются 1, Короли (King) — 13, но есть "прокрутка по кругу":
    // Ace можно положить на 2 или King, King можно положить на Queen или Ace
    public bool CanPlaceOnTop(CardModel other)
    {
        int currentValue = (int)Rank;
        int otherValue = (int)other.Rank;

        // "Сдвиг" по кругу
        if (currentValue == 1 && (otherValue == 2 || otherValue == 13)) return true;
        if (currentValue == 13 && (otherValue == 12 || otherValue == 1)) return true;

        // Обычная проверка
        return (otherValue == currentValue + 1) || (otherValue == currentValue - 1);
    }
}
