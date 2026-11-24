using UnityEngine;
using System.Collections.Generic;

// Which towers the player owns, and which of them are brought into a game (at most MaxEquipped).
// Both are saved between sessions. Towers without a CoinPrice are owned from the start.
public static class Loadout
{
    public const int MaxEquipped = 5;

    private const string SaveKey = "Loadout";
    private const string OwnedSaveKey = "OwnedTowers";
    // Asset names in Resources/Towers, used when nothing is saved yet
    private static readonly string[] DefaultTowers = { "Basic", "Melee", "Sniper" };

    // Called when a tower is equipped or unequipped
    public static event System.Action Changed;

    private static TowerData[] AllTowers;
    private static List<string> Equipped;
    // Only the bought towers, the free ones aren't in here
    private static List<string> Bought;

    // Every tower in the game, cheapest first
    public static TowerData[] GetAllTowers()
    {
        if (AllTowers == null)
        {
            AllTowers = Resources.LoadAll<TowerData>("Towers");
            System.Array.Sort(AllTowers, (a, b) => a.Cost.CompareTo(b.Cost));
        }
        return AllTowers;
    }

    // Equipped towers in the order they were equipped, this is also their hotkey order
    public static List<TowerData> GetEquipped()
    {
        Load();
        List<TowerData> towers = new List<TowerData>();
        foreach (string name in Equipped)
        {
            TowerData tower = System.Array.Find(GetAllTowers(), t => t.name == name);
            if (tower != null)
            {
                towers.Add(tower);
            }
        }
        return towers;
    }

    public static bool IsOwned(TowerData tower)
    {
        Load();
        return tower.CoinPrice <= 0 || Bought.Contains(tower.name);
    }

    // Returns false and spends nothing when it is already owned or too expensive
    public static bool TryBuy(TowerData tower)
    {
        if (IsOwned(tower) || !CoinWallet.TrySpend(tower.CoinPrice))
        {
            return false;
        }

        Bought.Add(tower.name);
        PlayerPrefs.SetString(OwnedSaveKey, string.Join(",", Bought));
        PlayerPrefs.Save();
        return true;
    }

    // Forgets all bought towers and the equipped ones
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(OwnedSaveKey);
        PlayerPrefs.Save();
        Equipped = null;
        Bought = null;
        Changed?.Invoke();
    }

    public static bool IsEquipped(TowerData tower)
    {
        Load();
        return Equipped.Contains(tower.name);
    }

    public static bool IsFull()
    {
        Load();
        return Equipped.Count >= MaxEquipped;
    }

    public static bool TryEquip(TowerData tower)
    {
        if (!IsOwned(tower) || IsEquipped(tower) || IsFull())
        {
            return false;
        }

        Equipped.Add(tower.name);
        Save();
        return true;
    }

    public static void Unequip(TowerData tower)
    {
        Load();
        if (Equipped.Remove(tower.name))
        {
            Save();
        }
    }

    private static void Load()
    {
        if (Equipped != null)
        {
            return;
        }

        string savedOwned = PlayerPrefs.GetString(OwnedSaveKey, "");
        Bought = savedOwned.Length > 0 ? new List<string>(savedOwned.Split(',')) : new List<string>();

        string saved = PlayerPrefs.GetString(SaveKey, "");
        Equipped = new List<string>(saved.Length > 0 ? saved.Split(',') : DefaultTowers);

        // Towers that were removed from the game or aren't owned, or a save with too many towers
        Equipped.RemoveAll(name =>
        {
            TowerData tower = System.Array.Find(GetAllTowers(), t => t.name == name);
            return tower == null || !IsOwned(tower);
        });
        if (Equipped.Count > MaxEquipped)
        {
            Equipped.RemoveRange(MaxEquipped, Equipped.Count - MaxEquipped);
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",", Equipped));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
