using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField, Range(0, 1000)]
    private int Width = 100;
    [SerializeField, Range(0, 1000)]
    private int Height = 100;

    [SerializeField]
    private string Seed = "Bo De Keersmaeker";
    [SerializeField]
    private bool UseRandomSeed = false;

    [SerializeField, Range(0, 100)]
    private int RandomFillPercent = 50;

    [SerializeField, Range(0, 1000)]
    private int IterationAmount = 5;

    private int[,] Map;
    private int[,] NewMap;

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        Map = new int[Width, Height];
        NewMap = new int[Width, Height];
        RandomFillMap();

        for (int i = 0; i < IterationAmount; i++)
            SmoothMap();

        int borderSize = 1;
        int[,] borderedMap = new int[Width + borderSize * 2, Height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if(x >= borderSize && x < Width + borderSize && y >= borderSize && y < Height + borderSize)
                {
                    borderedMap[x, y] = Map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }

        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.generateMesh(borderedMap, 1f);
    }

    private void RandomFillMap()
    {
        if(UseRandomSeed)
            Seed = Time.time.ToString();

        System.Random pseudoRandom = new System.Random(Seed.GetHashCode());

        for (int x = 0; x < Width; x++)
            for(int y = 0; y < Height; y++)
            {
                if( x <= 0 || x >= Width-1 || y <= 0 || y >= Height - 1)
                    Map[x, y] = 1;
                else
                    Map[x, y] = (pseudoRandom.Next(0, 100) < RandomFillPercent)? 1 : 0;
            }

    }

    private void SmoothMap()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)
                    NewMap[x, y] = 1;
                else if (neighbourWallTiles < 4)
                    NewMap[x, y] = 0;
                else
                    NewMap[x, y] = Map[x, y];
            }

        Map = NewMap;
    }

    private int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for(int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
            for(int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX == gridX && neighbourY == gridY)
                    continue;

                if (neighbourX < 0 || neighbourX >= Width || neighbourY < 0 || neighbourY >= Height)
                    wallCount++;
                else if (Map[neighbourX, neighbourY] == 1)
                    wallCount += Map[neighbourX, neighbourY];
            }

        return wallCount;
    }

    private void OnDrawGizmos()
    {
        //if(Map != null)
        //    for (int x = 0; x < Width; x++)
        //    {
        //        for (int y = 0; y < Height; y++)
        //        {
        //            Gizmos.color = (Map[x, y] == 1) ? Color.black : Color.white;
        //            Vector3 pos = new Vector3(-Width / 2f + x + .5f, 0f, -Height / 2f + y + .5f);
        //            Gizmos.DrawCube(pos, Vector3.one);
        //        }
        //    }
    }
}