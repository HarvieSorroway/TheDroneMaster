using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class DronePather : PathFinder
    {
        public DronePather(ArtificialIntelligence AI, World world, AbstractCreature creature)
            : base(AI, world, creature)
        {
            pathfinderResourceDivider.budget = 1000;
            stepsPerFrame = 350;
            accessibilityStepsPerFrame = 350;
        }

        public override PathCost CheckConnectionCost(PathFinder.PathingCell start, PathFinder.PathingCell goal, MovementConnection connection, bool followingPath)
        {
            PathCost pathCost = base.CheckConnectionCost(start, goal, connection, followingPath);
            if (connection.destinationCoord.TileDefined && destination.TileDefined && Custom.ManhattanDistance(connection.destinationCoord, destination) > 6)
            {
                pathCost.resistance += Mathf.Clamp(10f - realizedRoom.aimap.getTerrainProximity(connection.destinationCoord), 0f, 10f) * 10f;
            }
            return pathCost;
        }

        public override PathCost HeuristicForCell(PathingCell cell, PathCost costToGoal)
        {
            if (!InThisRealizedRoom(cell.worldCoordinate))
            {
                return costToGoal;
            }
            if (lookingForImpossiblePath && !cell.reachable)
            {
                return costToGoal;
            }
            return new PathCost(Custom.LerpMap(creaturePos.Tile.FloatDist(cell.worldCoordinate.Tile), 20f, 50f, costToGoal.resistance, creaturePos.Tile.FloatDist(cell.worldCoordinate.Tile) * costToGoal.resistance), costToGoal.legality);
        }

        public MovementConnection FollowPath(WorldCoordinate originPos, bool actuallyFollowingThisPath)
        {
            if (originPos.TileDefined)
            {
                originPos.x = Math.Min(Math.Max(originPos.x, 0), this.realizedRoom.TileWidth - 1);
                originPos.y = Math.Min(Math.Max(originPos.y, 0), this.realizedRoom.TileHeight - 1);
            }
            if (CoordinateCost(originPos).Allowed)
            {
                PathingCell pathingCell = PathingCellAtWorldCoordinate(originPos);
                if (pathingCell != null)
                {
                    if (!pathingCell.reachable || !pathingCell.possibleToGetBackFrom)
                    {
                        OutOfElement();
                    }
                    MovementConnection movementConnection = default;
                    PathCost pathCost = new PathCost(0f, PathCost.Legality.Unallowed);
                    int num = -acceptablePathAge;
                    PathCost.Legality legality = PathCost.Legality.Unallowed;
                    int num2 = 0;
                    for (; ; )
                    {
                        MovementConnection movementConnection2 = ConnectionAtCoordinate(true, originPos, num2);
                        num2++;
                        if (movementConnection2 == default)
                        {
                            break;
                        }
                        if (!movementConnection2.destinationCoord.TileDefined || Custom.InsideRect(movementConnection2.DestTile, this.coveredArea))
                        {
                            PathingCell pathingCell2 = base.PathingCellAtWorldCoordinate(movementConnection2.destinationCoord);
                            PathCost pathCost2 = this.CheckConnectionCost(pathingCell, pathingCell2, movementConnection2, true);
                            if (!pathingCell2.possibleToGetBackFrom && !this.walkPastPointOfNoReturn)
                            {
                                pathCost2.legality = PathCost.Legality.Unallowed;
                            }
                            PathCost pathCost3 = pathingCell2.costToGoal + pathCost2;
                            if (movementConnection2.destinationCoord.Tile == this.destination.Tile)
                            {
                                pathCost3.resistance = 0f;
                            }
                            if (pathCost2.legality < legality)
                            {
                                movementConnection = movementConnection2;
                                legality = pathCost2.legality;
                                num = pathingCell2.generation;
                                pathCost = pathCost3;
                            }
                            else if (pathCost2.legality == legality)
                            {
                                if (pathingCell2.generation > num)
                                {
                                    movementConnection = movementConnection2;
                                    legality = pathCost2.legality;
                                    num = pathingCell2.generation;
                                    pathCost = pathCost3;
                                }
                                else if (pathingCell2.generation == num && pathCost3 <= pathCost)
                                {
                                    movementConnection = movementConnection2;
                                    legality = pathCost2.legality;
                                    num = pathingCell2.generation;
                                    pathCost = pathCost3;
                                }
                            }
                        }
                    }
                    if (legality <= PathCost.Legality.Unwanted)
                    {
                        if (actuallyFollowingThisPath)
                        {
                            if (movementConnection != default && !movementConnection.destinationCoord.TileDefined)
                            {
                                LeavingRoom();
                            }
                            creatureFollowingGeneration = num;
                        }
                        return movementConnection;
                    }
                }
            }
            return default;
        }
    }
}
