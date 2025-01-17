using UnityEngine;
using System.Collections.Generic;

public static class DeckGenerator
{
    public static List<CardModel> CreateShuffledDeck()
    {
        List<CardModel> deck = new List<CardModel>();

        // ��������� �� ���� ������ � ������
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                // ���������� ���� ��� Addressables
                // ��������: Card_Suit_Rank => "Card_Hearts_Ace"
                string addressKey = $"Card_{suit}_{rank}";

                var card = new CardModel(suit, rank, addressKey, false);
                deck.Add(card);
            }
        }

        // ������������
        Shuffle(deck);
        return deck;
    }

    // ��������� ������������ (Fisher�Yates)
    private static void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
