using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class InventoryStorer : NetworkBehaviour
{
    public ItemDatabaseObject database;
    [SerializeField] public Dictionary<int, InventoryObject> playerInventorys = new Dictionary<int, InventoryObject>();


    private float time = 0.0f;
    public float interpolationPeriod = 30f;

    private void Update()
    {
        time += Time.deltaTime;

        if (time >= interpolationPeriod)
        {
            time = 0.0f;

            foreach (KeyValuePair<int, InventoryObject> kvp in playerInventorys)
            {
                Debug.Log($"Key = {kvp.Key}, Value = {kvp.Value}");
                for (int i = 0; i < kvp.Value.Container.Items.Length; i++)
                {
                    Debug.Log($"{kvp.Value.Container.Items[i].amount}->{kvp.Value.Container.Items[i].item}");

                }
            }
        }
    }
}
