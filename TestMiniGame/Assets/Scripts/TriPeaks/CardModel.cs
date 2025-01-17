using UnityEngine;

public enum CardSuit
{
    Clubs,    // �����
    Diamonds, // �����
    Hearts,   // �����
    Spades    // ����
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

    // ����� ������� � Addressables (��� ID)
    public string AddressKey;

    // �������, ������� ����� (������� �������) ��� ���
    public bool IsFaceUp;

    public CardModel(CardSuit suit, CardRank rank, string addressKey, bool isFaceUp = false)
    {
        Suit = suit;
        Rank = rank;
        AddressKey = addressKey;
        IsFaceUp = isFaceUp;
    }

    // ��������, ����� �� �������� �� ��� ����� ������ (�� 1 ������ ��� �� 1 ������)
    // ���� (Ace) ��������� 1, ������ (King) � 13, �� ���� "��������� �� �����":
    // Ace ����� �������� �� 2 ��� King, King ����� �������� �� Queen ��� Ace
    public bool CanPlaceOnTop(CardModel other)
    {
        int currentValue = (int)Rank;
        int otherValue = (int)other.Rank;

        // "�����" �� �����
        if (currentValue == 1 && (otherValue == 2 || otherValue == 13)) return true;
        if (currentValue == 13 && (otherValue == 12 || otherValue == 1)) return true;

        // ������� ��������
        return (otherValue == currentValue + 1) || (otherValue == currentValue - 1);
    }
}
