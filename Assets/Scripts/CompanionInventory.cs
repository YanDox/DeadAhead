using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionInventory : MonoBehaviour
{
    public const int BUS_PART = 0;
    public int[] items = new int[1];

    public bool AddItem(int itemType)
    {
        if (itemType >= 0 && itemType < items.Length)
        {
            items[itemType]++;
            return true;
        }
        return false;
    }

    public bool UseItem(int itemType)
    {
        if (itemType >= 0 && itemType < items.Length && items[itemType] > 0)
        {
            items[itemType]--;
            return true;
        }
        return false;
    }
}
