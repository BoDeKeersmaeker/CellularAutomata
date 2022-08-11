using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkScript : MonoBehaviour
{
    MapGenerator3D MapGeneratorScript;
    int OwningChunkID = -1;

    // Start is called before the first frame update
    void Start()
    {
        MapGeneratorScript = FindObjectOfType<MapGenerator3D>();

        if (MapGeneratorScript == null)
            print("Problem");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetOwningChunkID(int chunkID)
    {
        OwningChunkID = chunkID;
    }

    public void MineNode(Vector3 worldPos)
    {
        MapGeneratorScript.MineBlock(worldPos);
    }
}
