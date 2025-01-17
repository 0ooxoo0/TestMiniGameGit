using UnityEngine;
using System.Collections.Generic;

public static class DeckGenerator
{
    public static List<CardModel> CreateShuffledDeck()
    {
        List<CardModel> deck = new List<CardModel>();

        // Пробегаем по всем мастям и рангам
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                // Сформируем ключ для Addressables
                // Например: Card_Suit_Rank => "Card_Hearts_Ace"
                string addressKey = $"Card_{suit}_{rank}";

                var card = new CardModel(suit, rank, addressKey, false);
                deck.Add(card);
            }
        }

        // Перемешиваем
        Shuffle(deck);
        return deck;
    }

    // Случайная перестановка (Fisher–Yates)
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
