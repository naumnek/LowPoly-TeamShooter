using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.FPS.Game;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Unity.FPS.Gameplay;

//Скрипт для установки параметров лута.
//Определяет оружия для игроков и ботов в матче.

namespace naumnek.Settings
{

    [Serializable]
    public struct MaxValueAttributes
    {
        public float MaxBullets;
        public float Damage;
        public float BulletSpeed;
        public float BulletSpreadAngle;
        public float BulletsPerShoot;
    }

    public class SettingsManager : MonoBehaviour
    {
        public PlayerInfo PlayerInfo = new PlayerInfo { };

        public CustomizationInfo Customization;

        public MaxValueAttributes MaxWeaponAttributes;

        public Items[] ItemList;
        public Items[] RequredItems { get; private set; }

        public WeaponController[] RequredWeaponsList { get; private set; }
        //public WeaponController[] WeaponsList { get; private set; }

        private static SettingsManager instance;

        public static SettingsManager GetInstance() => instance;

        // Start is called before the first frame update
        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            RequredItems = ItemList.Where(w => !w.IsBlocked).ToArray();
            RequredWeaponsList = new WeaponController[RequredItems.Length];
            for (int i = 0; i < RequredItems.Length; i++)
            {
                RequredWeaponsList[i] = RequredItems[i].Weapon;
                RequredItems[i].Reset();
            }
        }

        public void ResetAllItemInfo()
        {
            foreach (Items item in ItemList)
            {
                item.Reset();
            }
        }

    }

    [Serializable]
    public class Items
    {
        [Header("Options Item")]
        public WeaponController Weapon;
        public bool IsUnvisibly;
        public bool IsBlocked;

        public string Name() => Weapon.WeaponName;
        public WeaponAttributes Attributes { get; private set; }

        public bool IsPaid { get; private set; }

        public bool RemovedFromList { get; private set; }

        public void OnPaid() => IsPaid = true;
        public void OnRemovedFromList() => RemovedFromList = true;

        public void Reset()
        {
            IsPaid = false;
            RemovedFromList = false;
            IsBlocked = false;
            Attributes = new WeaponAttributes();
            Attributes.SetWeapon(Weapon);
        }
    }

    public class WeaponAttributes
    {
        public WeaponController Controller { get; private set; }

        public float MaxBullets { get; private set; }
        public float Damage { get; private set; }
        public float BulletSpeed { get; private set; }
        public float SpreadAngle { get; private set; }
        public float BulletsPerShoot { get; private set; }

        public void SetWeapon(WeaponController weapon)
        {
            ProjectileStandard projectile = weapon.ProjectilePrefab.GetComponent<ProjectileStandard>();

            MaxBullets = weapon.MaxBullets;
            Damage = projectile.Damage;
            BulletSpeed = projectile.Speed;
            SpreadAngle = weapon.BulletSpreadAngle;
            BulletsPerShoot = weapon.BulletsPerShot;
        }
    }

    public enum SkinType
    {
        Free,
        NFT,
    }

    [Serializable]
    public class CustomizationInfo
    {
        public Mesh[] FreeCharacterModels;
        public Material[] FreeCharacterMaterials;
        public Mesh[] NFTCharacterModels;
        public Material[] NFTCharacterMaterials;
        public SkinType Type { get; private set; } = SkinType.Free;

        public int CurrentIndexFreeModel { get; private set; } = 0;
        public int CurrentIndexFreeMaterial { get; private set; } = 0;
        public int CurrentIndexNFTModel { get; private set; } = 0;
        public int CurrentIndexNFTMaterial { get; private set; } = 0;

        private int IndexFreeModel = 0;
        private int IndexFreeMaterial = 0;
        private int IndexNFTModel = 0;
        private int IndexNFTMaterial = 0;

        private List<Mesh> AllModels = new List<Mesh>();
        private List<Material> AllMaterials = new List<Material>();

        public Mesh GetRandomModel()
        {
            if(AllModels.Count == 0)
            {
                AllModels.AddRange(FreeCharacterModels);
                AllModels.AddRange(NFTCharacterModels);
            }
            return AllModels[UnityEngine.Random.Range(0, AllModels.Count)];
        }
        public Material GetRandomMaterial()
        {
            if (AllMaterials.Count == 0)
            {
                AllMaterials.AddRange(FreeCharacterMaterials);
                AllMaterials.AddRange(NFTCharacterMaterials);
            }
            return AllMaterials[UnityEngine.Random.Range(0, AllMaterials.Count)];
        }

        public Mesh GetModel(SkinType type, int index)
        {
            switch (type)
            {
                case SkinType.Free:
                    return FreeCharacterModels[index];
                case SkinType.NFT:
                    return NFTCharacterModels[index];
            }
            return FreeCharacterModels[IndexFreeModel];
        }
        public Material GetMaterial(SkinType type, int index)
        {
            switch (type)
            {
                case SkinType.Free:
                    return FreeCharacterMaterials[index];
                case SkinType.NFT:
                    return FreeCharacterMaterials[index];
            }
            return FreeCharacterMaterials[IndexFreeMaterial];
        }

        public Mesh GetCurrentModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return FreeCharacterModels[IndexFreeModel];
                case SkinType.NFT:
                    return NFTCharacterModels[IndexNFTModel];
            }
            return FreeCharacterModels[IndexFreeModel];
        }

        public Material GetCurrentMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return FreeCharacterMaterials[IndexFreeMaterial];
                case SkinType.NFT:
                    return FreeCharacterMaterials[IndexNFTMaterial];
            }
            return FreeCharacterMaterials[IndexFreeMaterial];
        }

        public bool CheckIndexModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return CurrentIndexFreeModel == IndexFreeModel;
                case SkinType.NFT:
                    return CurrentIndexNFTModel == IndexNFTModel;
            }
            return CurrentIndexFreeModel == IndexFreeModel;
        }

        public bool CheckIndexMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return CurrentIndexFreeMaterial == IndexFreeMaterial;
                case SkinType.NFT:
                    return CurrentIndexNFTMaterial == IndexNFTMaterial;
            }
            return CurrentIndexFreeMaterial == IndexFreeMaterial;
        }

        public int GetCurrentIndexModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return CurrentIndexFreeModel;
                case SkinType.NFT:
                    return CurrentIndexNFTModel;
            }
            return CurrentIndexFreeModel;
        }

        public int GetCurrentIndexMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    return CurrentIndexFreeMaterial;
                case SkinType.NFT:
                    return CurrentIndexNFTMaterial;
            }
            return CurrentIndexFreeMaterial;
        }


        public void SetCharacterType(SkinType newType)
        {
            Type = newType;
        }

        public void SetCurrentIndexModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    CurrentIndexFreeModel = IndexFreeModel;
                    break;
                case SkinType.NFT:
                    CurrentIndexNFTModel = IndexNFTModel;
                    break;
            }
        }

        public void SetCurrentIndexMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    CurrentIndexFreeMaterial = IndexFreeMaterial;
                    break;
                case SkinType.NFT:
                    CurrentIndexNFTMaterial = IndexNFTMaterial;
                    break;
            }
        }

        public Mesh LeftModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    IndexFreeModel--;
                    if (IndexFreeModel < 0)
                        IndexFreeModel = FreeCharacterModels.Length - 1;
                    break;
                case SkinType.NFT:
                    IndexNFTModel--;
                    if (IndexNFTModel < 0)
                        IndexNFTModel = NFTCharacterModels.Length - 1;
                    return NFTCharacterModels[IndexNFTModel];
            }
            return FreeCharacterModels[IndexFreeModel];
        }
        public Mesh RightModel()
        {
            switch (Type)
            {
                case SkinType.Free:
                    IndexFreeModel++;
                    if (IndexFreeModel >= FreeCharacterModels.Length)
                        IndexFreeModel = 0;
                    return FreeCharacterModels[IndexFreeModel];
                case SkinType.NFT:
                    IndexNFTModel++;
                    if (IndexNFTModel >= NFTCharacterModels.Length)
                        IndexNFTModel = 0;
                    return NFTCharacterModels[IndexNFTModel];
            }
            return FreeCharacterModels[IndexFreeModel];
        }

        public Material LeftMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    IndexFreeMaterial--;
                    if (IndexFreeMaterial < 0)
                        IndexFreeMaterial = FreeCharacterMaterials.Length - 1;
                    return FreeCharacterMaterials[IndexFreeMaterial];
                case SkinType.NFT:
                    IndexNFTMaterial--;
                    if (IndexNFTMaterial < 0)
                        IndexNFTMaterial = NFTCharacterMaterials.Length - 1;
                    return NFTCharacterMaterials[IndexNFTMaterial];
            }
            return FreeCharacterMaterials[IndexFreeMaterial];
        }
        public Material RightMaterial()
        {
            switch (Type)
            {
                case SkinType.Free:
                    IndexFreeMaterial++;
                    if (IndexFreeMaterial >= FreeCharacterMaterials.Length)
                        IndexFreeMaterial = 0;
                    return FreeCharacterMaterials[IndexFreeMaterial];
                case SkinType.NFT:
                    IndexNFTMaterial++;
                    if (IndexNFTMaterial >= NFTCharacterMaterials.Length)
                        IndexNFTMaterial = 0;
                    return NFTCharacterMaterials[IndexNFTMaterial];
            }
            return FreeCharacterMaterials[IndexFreeMaterial];
        }
    }

    public class PlayerInfo
    {
        public string Name { get; private set; }
        public int Score { get; private set; }

        public void SetPlayer(string name)
        {
            Name = name;
        }
        public void AddPlayerScore(int score)
        {
            Score += score;
        }
    }
}
