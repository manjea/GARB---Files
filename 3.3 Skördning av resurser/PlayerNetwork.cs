using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

public class PlayerNetwork : NetworkBehaviour
{
    public List<GroundItem> items = new List<GroundItem>();

    public GameObject campfirePrefab;

    [Client]
    public void Kms(GameObject player)
    {
        CmdDie(player);
    }

    [Command]
    private void CmdDie(GameObject player)
    {
        NetworkServer.Destroy(player);
    }

    private InventoryStorer inventoryStorer;
    private ItemDatabaseObject database;
    private void Awake()
    {
        inventoryStorer = GameObject.FindGameObjectWithTag("GameManager").GetComponent<InventoryStorer>();
        database = inventoryStorer.database;

    }

    #region - Create Player Inventory -
    [Client]
    public void AskSeerverToCreateInventory(int _playerId)
    {
        CreatePlayerInventory(_playerId);
    }

    [Command(requiresAuthority = false)]
    private void CreatePlayerInventory(int _playerId)
    {
        inventoryStorer.playerInventorys.Add(_playerId, new InventoryObject());
        inventoryStorer.playerInventorys[_playerId].database = inventoryStorer.database;
        inventoryStorer.playerInventorys[_playerId].Container = new Inventory();
        inventoryStorer.playerInventorys[_playerId].Container.Items = new InventorySlot[24];
        for (int i = 0; i < inventoryStorer.playerInventorys[_playerId].Container.Items.Length; i++)
        {
            inventoryStorer.playerInventorys[_playerId].Container.Items[i] = new InventorySlot();
            Debug.Log(inventoryStorer.playerInventorys[_playerId].Container.Items[i].ID);
        }
    }
    #endregion

    #region - Add Item -
    [Client]
    public void AskServerToAddItem(int item, int amount, int playerId)
    {
        AddItemOnServer(item, amount, playerId);
    }

    [Command(requiresAuthority = false)]
    private void AddItemOnServer(int item, int amount, int playerId)
    {
        inventoryStorer.playerInventorys[playerId].AddItem(new Item(database.GetItem[item]), amount);
    }
    #endregion

    #region - Get Player Inventory -
    [Client]
    public InventoryObject AskSeerverToGetInventory(int _playerId)
    {
        return GetPlayerInventory(_playerId);
    }

    private InventoryObject GetPlayerInventory(int _playerId)
    {
        return inventoryStorer.playerInventorys[_playerId];
    }
    #endregion

    #region - Craft Campfire -
    [Client]
    public void TellServerToCraftCampFire(int _playerId)
    {
        CmdCraftCampfire(_playerId);
    }
    [Command(requiresAuthority = false)]
    private void CmdCraftCampfire(int _playerId)
    {
        InventorySlot[] slots = inventoryStorer.playerInventorys[_playerId].Container.Items;
        int woodIndex = 0;
        int rockIndex = 0;

        bool hasWood = false;
        bool hasRock = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if(slots[i].ID == 0)
            {
                hasRock = true;
                rockIndex = i;
            }
            if(slots[i].ID == 1)
            {
                hasWood = true;
                woodIndex = i;
            }
        }
        
        if(hasWood && hasRock)
        {
            slots[rockIndex].amount -= 1;
            slots[woodIndex].amount -= 1;

            if(slots[rockIndex].amount <= 0)
            {
                slots[rockIndex].UpdateSlot(-1, null, 0);
            }
            if (slots[woodIndex].amount <= 0)
            {
                slots[woodIndex].UpdateSlot(-1, null, 0);
            }

            inventoryStorer.playerInventorys[_playerId].AddItem(new Item(database.GetItem[5]), 1);
        }

    }

    #endregion

    #region - Spawn Campfire -
    [Client]
    public void TellServerToSpawnCampfire(Vector3 transf)
    {
        CmdSpawnCampfire(transf);
    }
    [Command(requiresAuthority = false)]
    public void CmdSpawnCampfire(Vector3 transf)
    {
        var obj = Instantiate(campfirePrefab, transf, Quaternion.identity);
        NetworkServer.Spawn(obj);
    }
    #endregion
}
