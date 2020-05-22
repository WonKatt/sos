using System;
using System.Collections.Generic;
using System.Threading;
using Lab3;

namespace Lab3
{
	internal class Program
	{

		public static void Main(string[] args)
		{
			var matr = new float[4][]
			{
				new float[] {2, 10, 9, 7}, new float[] {15, 4, 14, 8}, new float[] {13, 14, 16, 11},
				new float[] {4, 15, 13, 19}
			};
			var inputMatr = new float[matr.Length][];

			for (var i = 0; i < matr.Length; i++)
			{
				inputMatr[i] = new float[matr.Length];
				for (var j = 0; j < matr.Length; j++) inputMatr[i][j] = matr[i][j];
			}

			var res = new Hungarian(matr).execute();

			Console.WriteLine("Îïòèìàëüí³ çíà÷åííÿ:");
			for (var i = 0; i < res.Length; i++)
			{
				Console.WriteLine("X");
				for (var j = 0; j < res[i].Length; j++) Console.WriteLine(res[i][j]);

				Console.WriteLine(", ");
			}


			Console.WriteLine("\nÖ³ëîâà ôóíêö³ÿ: \nF(X') = ");
			var fx = default(float);
			for (var i = 0; i < res.Length; i++)
			{
				fx += inputMatr[res[i][0]][res[i][1]];
				Console.WriteLine(inputMatr[res[i][0]][res[i][1]]);
				if (i < res.Length - 1) Console.WriteLine(" + ");
			}

			Console.WriteLine(" = " + fx);
		}


	}


	public class Hungarian
	{

		private int numRows;
		private int numCols;

		private bool[][] primes;
		private bool[][] stars;
		private bool[] rowsCovered;
		private bool[] colsCovered;
		private float[][] costs;

		public Hungarian(float[][] theCosts)
		{
			costs = theCosts;
			numRows = costs.Length;
			numCols = costs[0].Length;

			primes = new bool[numRows][];
			stars = new bool[numRows][];

			// Èíèöèàëèçàöèÿ ìàññèâîâ ñ ïîêðûòèåì ñòðîê/ñòîëáöîâ
			rowsCovered = new bool[numRows];
			colsCovered = new bool[numCols];
			for (var i = 0; i < numRows; i++) rowsCovered[i] = false;

			for (var j = 0; j < numCols; j++) colsCovered[j] = false;

			// Èíèöèàëèçàöèÿ ìàòðèö
			for (var i = 0; i < numRows; i++)
			{
				primes[i] = new bool[numCols];
				stars[i] = new bool[numCols];
				for (var j = 0; j < numCols; j++)
				{
					primes[i][j] = false;
					stars[i][j] = false;
				}
			}
		}

		public int[][] execute()
		{
			subtractRowColMins();

			findStars(); // O(n^2)
			resetCovered(); // O(n);
			coverStarredZeroCols(); // O(n^2)

			while (!allColsCovered())
			{
				var primedLocation = primeUncoveredZero(); // O(n^2)
				if (primedLocation[0] == -1)
				{
					minUncoveredRowsCols(); // O(n^2)
					primedLocation = primeUncoveredZero(); // O(n^2)
				}

				// is there a starred 0 in the primed zeros row?
				var primedRow = primedLocation[0];
				var starCol = findStarColInRow(primedRow);
				if (starCol != -1)
				{
					// cover ther row of the primedLocation and uncover the star column
					rowsCovered[primedRow] = true;
					colsCovered[starCol] = false;
				}
				else
				{
					// otherwise we need to find an augmenting path and start over.
					augmentPathStartingAtPrime(primedLocation);
					resetCovered();
					resetPrimes();
					coverStarredZeroCols();
				}
			}

			return starsToAssignments(); // O(n^2)

		}

		public int[][] starsToAssignments()
		{
			var toRet = new int[numCols][];
			for (var j = 0; j < numCols; j++)
				toRet[j] = new int[]
				{
					findStarRowInCol(j), j
				}; // O(n)

			return toRet;
		}

		public void resetPrimes()
		{
			for (var i = 0; i < numRows; i++)
			for (var j = 0; j < numCols; j++) primes[i][j] = false;
		}

		public void resetCovered()
		{
			for (var i = 0; i < numRows; i++) rowsCovered[i] = false;

			for (var j = 0; j < numCols; j++) colsCovered[j] = false;
		}

		public void findStars()
		{
			var rowStars = new bool[numRows];
			var colStars = new bool[numCols];

			for (var i = 0; i < numRows; i++) rowStars[i] = false;

			for (var j = 0; j < numCols; j++) colStars[j] = false;

			for (var j = 0; j < numCols; j++)
			for (var i = 0; i < numRows; i++)
				if (costs[i][j] == 0 && !rowStars[i] && !colStars[j])
				{
					stars[i][j] = true;
					rowStars[i] = true;
					colStars[j] = true;
					break;
				}
		}

		private void minUncoveredRowsCols()
		{
			// find min uncovered value
			var minUncovered = float.MaxValue;
			for (var i = 0; i < numRows; i++)
				if (!rowsCovered[i])
					for (var j = 0; j < numCols; j++)
						if (!colsCovered[j])
							if (costs[i][j] < minUncovered) minUncovered = costs[i][j];

			// add that value to all the COVERED rows.
			for (var i = 0; i < numRows; i++)
				if (rowsCovered[i])
					for (var j = 0; j < numCols; j++) costs[i][j] = costs[i][j] + minUncovered;

			// subtract that value from all the UNcovered columns
			for (var j = 0; j < numCols; j++)
				if (!colsCovered[j])
					for (var i = 0; i < numRows; i++) costs[i][j] = costs[i][j] - minUncovered;
		}

		private int[] primeUncoveredZero()
		{
			var location = new int[2];

			for (var i = 0; i < numRows; i++)
				if (!rowsCovered[i])
					for (var j = 0; j < numCols; j++)
						if (!colsCovered[j])
							if (costs[i][j] == 0)
							{
								primes[i][j] = true;
								location[0] = i;
								location[1] = j;
								return location;
							}

			location[0] = -1;
			location[1] = -1;
			return location;
		}

		private void augmentPathStartingAtPrime(int[] location)
		{
			// Make the arraylists sufficiently large to begin with
			var primeLocations = new List<int[]>(numRows + numCols);
			var starLocations = new List<int[]>(numRows + numCols);
			primeLocations.Add(location);

			var currentRow = location[0];
			var currentCol = location[1];
			while (true)
			{
				// add stars and primes in pairs
				var starRow = findStarRowInCol(currentCol);
				if (starRow == -1) break;

				var starLocation = new int[]
				{
					starRow, currentCol
				};
				starLocations.Add(starLocation);
				currentRow = starRow;

				var primeCol = findPrimeColInRow(currentRow);
				var primeLocation = new int[]
				{
					currentRow, primeCol
				};
				primeLocations.Add(primeLocation);
				currentCol = primeCol;
			}

			unStarLocations(starLocations);
			StarLocations(primeLocations);
		}


		private void StarLocations(List<int[]> locations)
		{
			for (var k = 0; k < locations.Count; k++)
			{
				var location = locations[k];
				var row = location[0];
				var col = location[1];
				stars[row][col] = true;
			}
		}

		private void unStarLocations(List<int[]> starLocations)
		{
			for (var k = 0; k < starLocations.Count; k++)
			{
				var starLocation = starLocations[k];
				var row = starLocation[0];
				var col = starLocation[1];
				stars[row][col] = false;
			}
		}

		private int findPrimeColInRow(int theRow)
		{
			for (var j = 0; j < numCols; j++)
				if (primes[theRow][j]) return j;

			return -1;
		}

		public int findStarRowInCol(int theCol)
		{
			for (var i = 0; i < numRows; i++)
				if (stars[i][theCol]) return i;

			return -1;
		}


		public int findStarColInRow(int theRow)
		{
			for (var j = 0; j < numCols; j++)
				if (stars[theRow][j]) return j;

			return -1;
		}

		private bool allColsCovered()
		{
			for (var j = 0; j < numCols; j++)
				if (!colsCovered[j]) return false;

			return true;
		}

		private void coverStarredZeroCols()
		{
			for (var j = 0; j < numCols; j++)
			{
				colsCovered[j] = false;
				for (var i = 0; i < numRows; i++)
					if (stars[i][j])
					{
						colsCovered[j] = true;
						break; // break inner loop to save a bit of time
					}
			}
		}

		private void subtractRowColMins()
		{
			for (var i = 0; i < numRows; i++)
			{
				//for each row
				var rowMin = float.MaxValue;
				for (var j = 0; j < numCols; j++)
				// grab the smallest element in that row
					if (costs[i][j] < rowMin) rowMin = costs[i][j];

				for (var j = 0; j < numCols; j++)
				// subtract that from each element
					costs[i][j] = costs[i][j] - rowMin;
			}

			for (var j = 0; j < numCols; j++)
			{
				// for each col
				var colMin = float.MaxValue;
				for (var i = 0; i < numRows; i++)
				// grab the smallest element in that column
					if (costs[i][j] < colMin) colMin = costs[i][j];

				for (var i = 0; i < numRows; i++)
				// subtract that from each element
					costs[i][j] = costs[i][j] - colMin;
			}
		}

	}
}
