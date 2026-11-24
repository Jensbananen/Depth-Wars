using UnityEngine;

// Coins to buy towers with in the shop. Saved between sessions, unlike the money inside a game.
public static class CoinWallet
{
    private const string SaveKey = "Coins";

    // Called when the number of coins changes
    public static event System.Action Changed;

    public static int Coins => PlayerPrefs.GetInt(SaveKey, 0);

    public static void Add(int amount)
    {
        Set(Coins + amount);
    }

    // Returns false and spends nothing when there aren't enough coins
    public static bool TrySpend(int amount)
    {
        if (Coins < amount)
        {
            return false;
        }

        Set(Coins - amount);
        return true;
    }

    public static void Reset()
    {
        Set(0);
    }

    private static void Set(int amount)
    {
        PlayerPrefs.SetInt(SaveKey, amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
