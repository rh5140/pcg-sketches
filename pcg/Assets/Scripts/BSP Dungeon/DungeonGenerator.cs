// https://www.youtube.com/watch?v=ObdGTz_NnqM
// https://www.reddit.com/r/gamedev/comments/2lyqki/three_techniques_to_procedurally_generate/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator
{
    List<RoomNode> allNodesCollection = new List<RoomNode>();

    private int dungeonWidth;
    private int dungeonLength;

    public DungeonGenerator(int dungeonWidth, int dungeonLength)
    {
        this.dungeonWidth = dungeonWidth;
        this.dungeonLength = dungeonLength;
    }

    public List<Node> CalculateDungeon(int maxIterations, int roomWidthMin, int roomLengthMin, float roomBottomCornerModifier, float roomTopCornerModifier, int roomOffset, int corridorWidth)
    {
        BinarySpacePartitioner bsp = new BinarySpacePartitioner(dungeonWidth, dungeonLength);
        allNodesCollection = bsp.PrepareNodesCollection(maxIterations, roomWidthMin, roomLengthMin);
        List<Node> roomSpaces = StructureHelper.TraverseGraphToExtractLowestLeafs(bsp.RootNode);

        RoomGenerator roomGenerator = new RoomGenerator(maxIterations, roomLengthMin, roomWidthMin);
        List<RoomNode> roomList = roomGenerator.GenerateRoomsInGivenSpaces(roomSpaces, roomBottomCornerModifier, roomTopCornerModifier, roomOffset);

        CorridorsGenerator corridorGenerator = new CorridorsGenerator();
        var corridorList = corridorGenerator.CreateCorridor(allNodesCollection, corridorWidth);

        return new List<Node>(roomList).Concat(corridorList).ToList();
    }
}
