using System;
using System.Threading.Tasks;
using Godot;

namespace PotatoFiesta.Generation;

[GlobalClass]
public partial class Labyrinth : TileMap
{
	private static int width = 500;
	private static int height = 500;
	private static int[,] maze = new int[width, height];
	private static Random rand = new Random();


	public override void _Ready()
	{
		InitializeMaze();
		GenerateMaze(0, 0);
		//GeneratePath(width / 2, height / 2);
		PrintMaze();
	}

	private static void InitializeMaze()
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				maze[i, j] = 0;
			}
		}
	}

	static void GenerateMaze(int x, int y)
	{
		maze[x, y] = 1;

		int[] directions = new int[] { 0, 1, 2, 3 };
		Shuffle(directions);

		foreach (int direction in directions)
		{
			int newX = x;
			int newY = y;

			if (direction == 0)
				newY -= 2;
			else if (direction == 1)
				newX += 2;
			else if (direction == 2)
				newY += 2;
			else if (direction == 3)
				newX -= 2;

			if (IsInBounds(newX, newY) && maze[newX, newY] == 0)
			{
				maze[newX, newY] = 1;
				maze[x + (newX - x) / 2, y + (newY - y) / 2] = 1;
				GenerateMaze(newX, newY);
			}
		}
	}

	private void GeneratePath(int x, int y)
	{
		while (true)
		{
			var pathSize = (int)rand.NextInt64(20) + 10;
			var random = rand.NextInt64(4);

			for (var path = 0; path < pathSize; path++)
			{
				maze[x, y] = 0;


				if (random == 0)
				{
					x++;
				}
				else if (random == 1)
				{
					x--;
				}
				else if (random == 2)
				{
					y++;
				}
				else if (random == 3)
				{
					y--;
				}

				if (!IsInBounds(x, y))
					return;
			}
		}

	}

	public async void PrintMaze()
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				if (j == 0)
					await Task.Delay(1);
				if (maze[i, j] == 1)
					SetCell(0, new Vector2I(i, j), 0, new Vector2I(0, 0));
			}
		}
	}

	static bool IsInBounds(int x, int y)
	{
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	static void Shuffle(int[] array)
	{
		int n = array.Length;
		for (int i = 0; i < n; i++)
		{
			int r = i + rand.Next(n - i);
			int temp = array[i];
			array[i] = array[r];
			array[r] = temp;
		}
	}
}