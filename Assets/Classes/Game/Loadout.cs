using UnityEngine;
using System.Collections.Generic;

// Which towers the player brings into a game, at most MaxEquipped. Saved between sessions.
// Every tower counts as owned for now, buying them with coins in the shop comes later.
public static class Loadout
{
    public const int MaxEquipped = 5;

    private const string SaveKey = "Loadout";
    // Asset names in Resources/Towers, used when nothing is saved yet
    private static readonly string[] DefaultTowers = { "Basic", "Melee", "Sniper" };

    // Called when a tower is equipped or unequipped
    public static event System.Action Changed;

    private static TowerData[] AllTowers;
    private static List<string> Equipped;

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
        return true;
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

        string saved = PlayerPrefs.GetString(SaveKey, "");
        Equipped = new List<string>(saved.Length > 0 ? saved.Split(',') : DefaultTowers);

        // Towers that were removed from the game, or a save with too many towers
        Equipped.RemoveAll(name => System.Array.Find(GetAllTowers(), t => t.name == name) == null);
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
