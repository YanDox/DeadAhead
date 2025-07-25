using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medkit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int healAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Inventory Inventory = other.GetComponent<Inventory>();
            if (Inventory != null && Inventory.AddItem(Inventory.MEDKIT))
            {
                Destroy(gameObject);
            }
        }
    }
}
