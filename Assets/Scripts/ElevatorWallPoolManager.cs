using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class ElevatorWallPoolManager
{
    public static Dictionary<string, Queue<Component>> elevatorWallPools = new Dictionary<string, Queue<Component>>();

    public static void EnqueueElevatorWall<T>(T item) where T : Component
    {
        if(!item.gameObject.activeSelf) { return;}

        item.transform.position = new Vector3(0, -1000, 0);
        elevatorWallPools[item.gameObject.name].Enqueue(item);
        item.gameObject.SetActive(false);
    }

    public static T DequeueElevatorWall<T>(string key) where T : Component
    {
       return elevatorWallPools[key].Dequeue() as T;
    }

    public static void SetupPool<T>(T poolPrefab, int poolSize, string dictEntry) where T : Component
    {
        elevatorWallPools.Add(dictEntry, new Queue<Component>()); 

        for(int i = 0; i < poolSize; i++)
        {
            T newObj = GameObject.Instantiate(poolPrefab);
            newObj.gameObject.SetActive(false);
            elevatorWallPools[dictEntry].Enqueue((T)newObj);
        }
    }
}
