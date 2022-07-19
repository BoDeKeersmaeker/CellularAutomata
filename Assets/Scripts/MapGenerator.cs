using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour
{
    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    class Room : IComparable<Room> 
    {
        public List<Coord> Tiles;
        public List<Coord> EdgeTiles;
        public List<Room> ConnectedRooms;
        public int RoomSize;
        public bool IsAccessibleFromMainRoom;
        public bool IsMainRoom;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            Tiles = roomTiles;
            RoomSize = Tiles.Count;
            ConnectedRooms = new List<Room>();
            EdgeTiles = new List<Coord>();

            foreach(Coord tile in Tiles)
                for(int x = tile.tileX-1; x <= tile.tileX+1; x++)
                    for(int y = tile.tileY-1; y <= tile.tileY+1; y++)
                    {
                        if (x != tile.tileX && y != tile.tileY)
                            continue;

                        if (map[x, y] == 1)
                            EdgeTiles.Add(tile);
                    }
        }

        public void SetAccessibleFromMainRoom()
        {
            if (IsAccessibleFromMainRoom)
                return;

            IsAccessibleFromMainRoom = true;
            foreach(Room connectedRoom in ConnectedRooms)
                connectedRoom.SetAccessibleFromMainRoom();
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.IsAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if (roomB.IsAccessibleFromMainRoom)
                roomA.SetAccessibleFromMainRoom();

            roomA.ConnectedRooms.Add(roomB);
            roomB.ConnectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return ConnectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.RoomSize.CompareTo(RoomSize);
        }
    }

    [SerializeField]
    private string Seed = "Bo De Keersmaeker";

    [SerializeField, Range(0, 1000)]
    private float WallThresholdSize = 50;
    [SerializeField, Range(0, 1000)]
    private float RoomThresholdSize = 50;

    [SerializeField, Range(0, 1000)]
    private int Width = 100;
    [SerializeField, Range(0, 1000)]
    private int Height = 100;
    [SerializeField, Range(0, 100)]
    private int RandomFillPercent = 50;
    [SerializeField, Range(0, 1000)]
    private int IterationAmount = 5;
    [SerializeField, Range(1, 1000)]
    private int BorderSize = 1;
    [SerializeField, Range(1, 100)]
    private int PassageRadius = 1;

    [SerializeField]
    private bool UseRandomSeed = false;
    [SerializeField]
    private bool DrawDebug = false;

    private int[,] Map;
    private int[,] NewMap;

    //debug variables.
    List<Tuple<Coord, Coord>> DebugLines = new List<Tuple<Coord, Coord>>();

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
        DebugLines.Clear();
        Map = new int[Width, Height];
        NewMap = new int[Width, Height];
        RandomFillMap();

        for (int i = 0; i < IterationAmount; i++)
            SmoothMap();

        ProcessMap();

        int[,] borderedMap = new int[Width + BorderSize * 2, Height + BorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= BorderSize && x < Width + BorderSize && y >= BorderSize && y < Height + BorderSize)
                    borderedMap[x, y] = Map[x - BorderSize, y - BorderSize];
                else
                    borderedMap[x, y] = 1;
            }

        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.generateMesh(borderedMap, 1f);
    }

    private void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach(List<Coord> wallRegion in wallRegions)
            if(wallRegion.Count < WallThresholdSize)
                foreach(Coord tile in wallRegion)
                    Map[tile.tileX, tile.tileY] = 0;

        List<List<Coord>> roomRegions = GetRegions(0);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
            if (roomRegion.Count < RoomThresholdSize)
                foreach (Coord tile in roomRegion)
                    Map[tile.tileX, tile.tileY] = 1;
            else
                survivingRooms.Add(new Room(roomRegion, Map));

        survivingRooms.Sort();
        survivingRooms[0].IsMainRoom = true;
        survivingRooms[0].IsAccessibleFromMainRoom = true;

        ConnectClosestRooms(survivingRooms);
    }

    private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
                if (room.IsAccessibleFromMainRoom)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int smallestDistance = 0;

        Coord closestTileA = new Coord();
        Coord closestTileB = new Coord();

        Room closestRoomA = new Room();
        Room closestRoomB = new Room();

        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.ConnectedRooms.Count > 0)
                    continue;
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                    continue;

                for (int tileIndexA = 0; tileIndexA < roomA.EdgeTiles.Count; tileIndexA++)
                    for (int tileIndexB = 0; tileIndexB < roomB.EdgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.EdgeTiles[tileIndexA];
                        Coord tileB = roomB.EdgeTiles[tileIndexB];

                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < smallestDistance || !possibleConnectionFound)
                        {
                            smallestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;

                            closestTileA = tileA;
                            closestTileB = tileB;

                            closestRoomA = roomA;
                            closestRoomB = roomB;
                        }
                    }
            }

            if(possibleConnectionFound && !forceAccessibilityFromMainRoom)
                CreatePassage(closestRoomA, closestRoomB, closestTileA, closestTileB);
        }
        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(closestRoomA, closestRoomB, closestTileA, closestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
            ConnectClosestRooms(allRooms, true);
    }

    private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        DebugLines.Add(new Tuple<Coord, Coord>(tileA, tileB));

        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord temp in line)
            DrawCirlce(temp, PassageRadius);
    }

    private void DrawCirlce(Coord temp, int radius)
    {
        for (int x = -radius; x < radius; x++)
            for (int y = -radius; y < radius; y++)
                if (x * x + y * y <= radius * radius)
                {
                    int tempX = temp.tileX + x;
                    int tempY = temp.tileY + y;

                    if (!IsInMapRange(tempX, tempY))
                        continue;

                    Map[tempX, tempY] = 0;
                }
    }

    //Algorithm that checks all tiles crossed by the line.
    private List<Coord> GetLine(Coord start, Coord end)
    {
        List<Coord> line = new List<Coord>();
        int x = start.tileX;
        int y = start.tileY;

        int dx = end.tileX - start.tileX;
        int dy = end.tileY - start.tileY;

        bool inverted = false;

        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        //longest and shortest because in the case that dy is longer than dx the algorithm would break.
        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradinetAccumulation = longest / 2;

        for(int i =0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradinetAccumulation += shortest;

            if (gradinetAccumulation >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;

                gradinetAccumulation -= longest;
            }
        }


        return line;
    }

    private Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-Width / 2f + .5f + tile.tileX, .1f, -Height / 2 + .5f + tile.tileY);
    }

    private List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[Width, Height];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (mapFlags[x, y] == 0 && Map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                        mapFlags[tile.tileX, tile.tileY] = 1;
                }
            }

        return regions;
    }

    private List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[Width, Height];
        int tileType = Map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX -1; x <= tile.tileX +1; x++)
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(!IsInMapRange(x, y) || (y != tile.tileY && x != tile.tileX))
                        continue;

                    if (mapFlags[x, y] == 0 && Map[x, y] == tileType)
                    {
                        mapFlags[x, y] = 1;
                        queue.Enqueue(new Coord(x, y));
                    }
                    
                }
        }

        return tiles;
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

                if (!IsInMapRange(neighbourX, neighbourY))
                    wallCount++;
                else if (Map[neighbourX, neighbourY] == 1)
                    wallCount += Map[neighbourX, neighbourY];
            }

        return wallCount;
    }

    private bool IsInMapRange(int x, int y)
    {
        return(x >= 0 && x < Width && y >= 0 && y < Height);
    }

    private void OnDrawGizmos()
    {
        //if (Map != null)
        //    for (int x = 0; x < Width; x++)
        //    {
        //        for (int y = 0; y < Height; y++)
        //        {
        //            Gizmos.color = (Map[x, y] == 1) ? Color.black : Color.white;
        //            Vector3 pos = new Vector3(-Width / 2f + x + .5f, 0f, -Height / 2f + y + .5f);
        //            Gizmos.DrawCube(pos, Vector3.one);
        //        }
        //    }

        if (DrawDebug)
        {
            foreach (Tuple<Coord, Coord> debugLine in DebugLines)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(CoordToWorldPoint(debugLine.Item1), CoordToWorldPoint(debugLine.Item2));
            }
        }
    }
}