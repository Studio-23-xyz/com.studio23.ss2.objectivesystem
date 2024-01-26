using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Studio23.SS2.InventorySystem.Core;
using Studio23.SS2.InventorySystem.Data;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Utilities
{
    public static class Helpers
    {
        public static bool HasSaveFile<TItem>(this InventoryBase<TItem> inventory)
        where TItem:ItemBase
        {
            var savefilePath = inventory.SavefilePath();
            return File.Exists(savefilePath);
        }

        public static string SavefilePath<TItem>(this InventoryBase<TItem> inventory) where TItem : ItemBase
        {
            var savefilePath = Path.Combine(Application.persistentDataPath, inventory.SaveDirectory);
            var saveFileName = inventory.InventoryName;
            var extention = ".tm";
            return Path.Combine(savefilePath, $"{saveFileName}{extention}");
        }

        public static async UniTask SafeLoadInventory<TItem>(this InventoryBase<TItem> inventory)
            where TItem: ItemBase
        {
            if (inventory.HasSaveFile())
                await inventory.LoadInventory();
            else
            {
                Debug.LogWarning($"{inventory.InventoryName}: no save file at path {inventory.SavefilePath()}");
            }
        }
        
        public static void NukeSave<TItem>(this InventoryBase<TItem> inventory)
            where TItem:ItemBase
        {
            var savefilePath = inventory.SavefilePath();
            File.Delete(savefilePath);
        }

        public static async UniTask TestLoad<TItem>(this InventoryBase<TItem> inventory)
            where TItem:ItemBase

        {
            var savefilePath = Path.Combine(Application.persistentDataPath, inventory.SaveDirectory);
            var saveFileName = inventory.InventoryName;
            var extention = ".tm";
            bool enableEncryption = true;
            string encryptionKey = "1234567812345678";
            string encryptionIv = "1234567876543218";
            List<ItemSaveData> loadedItemDatas = await SaveSystem.Core.SaveSystem.Instance.LoadData<List<ItemSaveData>>(
                saveFileName, savefilePath,
                extention,
                enableEncryption, encryptionKey, encryptionIv
            );

            foreach (var loadedItemData in loadedItemDatas)
            {
                Debug.Log($"loadedItemData.SOName = {loadedItemData.SOName}{($"Inventory System/{inventory.InventoryName}/{loadedItemData.SOName}")}");
            }
        }
    }
}