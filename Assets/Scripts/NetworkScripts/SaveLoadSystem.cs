using UnityEngine;
using System.IO;
using Unity.Netcode;

public static class SaveLoadSystem{

    public struct PlayerSaveData: INetworkSerializable{
        public Vector3 PlayerPosition;
        public float Health;
        public float Strength;
        public float Speed;
        public float Endurance;
        public float SummoningCapacity;
        public float BindingAffinity;
        public float Corruption;
        public float MaxHealth;
        public float MaxStrength;
        public float MaxSpeed;
        public float MaxEndurance;
        public float MaxSummoningCapacity;
        public float MaxBindingAffinity;
        public float MaxCorruption;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
            serializer.SerializeValue(ref PlayerPosition);
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref Strength);
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Endurance);
            serializer.SerializeValue(ref SummoningCapacity);
            serializer.SerializeValue(ref BindingAffinity);
            serializer.SerializeValue(ref Corruption);
            serializer.SerializeValue(ref MaxHealth);
            serializer.SerializeValue(ref MaxStrength);
            serializer.SerializeValue(ref MaxSpeed);
            serializer.SerializeValue(ref MaxEndurance);
            serializer.SerializeValue(ref MaxSummoningCapacity);
            serializer.SerializeValue(ref MaxBindingAffinity);
            serializer.SerializeValue(ref MaxCorruption);
        }
    }

    private static readonly string saveFolder = Application.persistentDataPath + "/Saves/";

    public static void SavePlayerData(string playerId, PlayerSaveData saveData){
        // filepath is: /Users/derekaraki-kurdyla/Library/Application Support/DefaultCompany/Siege of Summoners/Saves/[playerId].json. 
        if (!Directory.Exists(saveFolder)){
            Directory.CreateDirectory(saveFolder);
        }
        string filePath = saveFolder + playerId + ".json";
        Debug.Log("Saving player data for playerId " + playerId + " to filepath " + filePath + ". ");
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(filePath, json);
    }

    public static PlayerSaveData LoadPlayerData(string playerId){
        string filePath = saveFolder + playerId + ".json";

        Debug.Log($"Loaded data for {playerId} from {filePath}");
        // Debug.Log($"Loaded JSON content: {json}");
        // Debug.Log($"Loaded PlayerSaveData - Position: {data.PlayerPosition}, Health: {data.Health}");

        if (File.Exists(filePath)){
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<PlayerSaveData>(json);
        }
        return default;
    }

}