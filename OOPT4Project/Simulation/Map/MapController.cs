﻿using System;
using System.Linq;
using System.Collections.Generic;
using OOPT4Project.Extension;
using OOPT4Project.Simulation.Creature;

namespace OOPT4Project.Simulation.Map
{
	public class MapController : ISimulated
	{
		public List<Tile> TileList { get; private set; } = new();
		public MapClimate MapClimate { get; private set; }

		private SimulationModel _model;

		public MapController(SimulationModel model)
		{ 
			_model = model;
			MapClimate = new MapClimate(this);
		}

		// TODO: OPTIMIZE by storing a curated list of border tiles
		public void CreateMapRandom(int resource, Dictionary<TileType, double> probs, double suddenSwitch)
		{
			TileList.Clear();

			Random rnd = SimulationModel.Generator;
			resource -= 1;

			var currentType = TileTypeLogic.Types.RandomElementByWeight(probs, rnd);
			var initTile = new Tile(new Coordinates(0, 0), currentType);

			TileList.Add(initTile);

			while (resource > 0)
			{
				Tile rndTile;
				if (GetTiles(GetBorderTiles(TileList), currentType).Count == 0 || rnd.NextDouble() < suddenSwitch)
				{
					rndTile = GetRandomTile(GetBorderTiles(TileList));
				}
				else
				{
					List<Tile> typedBorders = GetTiles(GetBorderTiles(TileList), currentType);
					if(typedBorders.Count == 0)
					{
						rndTile = GetRandomTile(GetBorderTiles(TileList));
					}
					else 
						rndTile = GetRandomTile(GetBorderTiles(typedBorders));
				}	

				var emptyNeighboors = GetEmptyNeighboors(TileList, rndTile);

				if (emptyNeighboors.Count == 0)
				{
					continue;
				}

				Coordinates crd = emptyNeighboors.PickRandom(rnd);

				TileList.Add(new Tile(crd, currentType));
				
				currentType = TileTypeLogic.Types.RandomElementByWeight(probs, rnd);
				resource--;
			}

			List<Coordinates> borders = GetEmptyBorders(TileList);
			borders.ForEach(x => TileList.Add(new Tile(x, TileType.Ocean)));

			return;
		}

		public bool RegisterCreatureImmidiately(CreatureEntity creature)
		{
			if(TileList.Contains(creature.CurrentTile))
			{
				creature.CurrentTile.CreatureList.Add(creature);
				return true;
			}
			return false;
		}

		public bool RegisterCreature(CreatureEntity creature)
		{
			if (TileList.Contains(creature.CurrentTile))
			{
				creature.CurrentTile.RegisterCreature(creature);
				return true;
			}
			return false;
		}

		public bool UnregisterCreature(CreatureEntity creature)
		{
			if(TileList.Contains(creature.CurrentTile))
			{
				creature.CurrentTile.UnregisterCreature(creature);
				return true;
			}
			return false;
		}

		public bool TransferCreature(CreatureEntity ent, Tile currentTile, Tile tile)
		{
			var neighboors = GetNeighboorTiles(TileList, currentTile);
			if (tile.CanWalkTo == false || !neighboors.Contains(tile))
				return false;

			currentTile.UnregisterCreature(ent);
			tile.RegisterCreature(ent);
			return true;
		}

		public void SimulateStep()
		{
			MapClimate.SimulateStep();

			foreach (Tile tile in TileList)
			{
				tile.SimulateStep();
			}
			foreach (Tile tile in TileList)
			{
				tile.EndStep();
			}
		}

		public static Tile GetRandomTile(List<Tile> tiles)
		{
			if (tiles.Count == 0)
				throw new Exception();
			return tiles.PickRandom(SimulationModel.Generator);
		}

		public static Tile GetRandomTile(List<Tile> tiles, TileType type, bool except = false)
		{
			if (tiles.Count == 0)
				throw new Exception();
			if (!except)
				return GetTiles(tiles, type).PickRandom(SimulationModel.Generator);
			else
				return GetTiles(tiles, type, true).PickRandom(SimulationModel.Generator);
		}

		public static List<Tile> GetBorderTiles(List<Tile> tiles)
		{
			return tiles.Where(x => 
			Coordinates.GetNeighboors(x.Coordinates)
					   .Except(tiles.Select(x => x.Coordinates)).ToList().Count != 0).ToList();
		}

		public static List<Tile> GetTiles(List<Tile> tiles, TileType type, bool except = false)
		{
			if(!except) 
				return tiles.Where(x => x.Type == type).ToList();
			else
				return tiles.Where(x => x.Type != type).ToList();
		}

		public static List<Tile> GetNeighboorTiles(List<Tile> tiles, Tile tile)
		{
			if (tiles.IndexOf(tile) == -1)
				throw new Exception();
			var neighboors = Coordinates.GetNeighboors(tile.Coordinates);
			return tiles.Where(x => neighboors.Contains(x.Coordinates)).ToList();
		}

		public static List<Coordinates> GetEmptyNeighboors(List<Tile> tiles, Tile tile)
		{
			var neighboors = Coordinates.GetNeighboors(tile.Coordinates);
			var neighboorTiles = GetNeighboorTiles(tiles, tile);
			var neighboorCoordinates = neighboorTiles.Select(x => x.Coordinates).ToList();
			var empty = neighboors.Except(neighboorCoordinates).ToList();

			return empty;
		}

		public static List<Coordinates> GetEmptyBorders(List<Tile> tiles)
		{
			List<Coordinates> allValid = tiles.Select(x => x.Coordinates).ToList();
			List<Coordinates> withEmptyNeightboors =
				GetBorderTiles(tiles).Select(x => x.Coordinates).ToList();

			HashSet<Coordinates> emptyBorders = new();

			foreach(Coordinates coor in withEmptyNeightboors)
			{
				foreach(Coordinates empty in Coordinates.GetNeighboors(coor))
				{
					if(!allValid.Contains(empty))
					{
						emptyBorders.Add(empty);
					}
				}
			}

			return emptyBorders.ToList();
		}
	}
}
