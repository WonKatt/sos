using System;
using System.Collections.Generic;
using System.Threading;
using Lab4;

namespace Lab4
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

			var res = new Hungarian(matr).Execute();

			Console.WriteLine("Оптимальні значення:");
			for (var i = 0; i < res.Length; i++)
			{
				Console.WriteLine("X");
				for (var j = 0; j < res[i].Length; j++) Console.WriteLine(res[i][j]);

				Console.WriteLine(", ");
			}


			Console.WriteLine("\nЦілова функція: \nF(X') = ");
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

		private readonly int _numRows;
		private readonly int _numCols;

		private readonly bool[][] _primes;
		private readonly bool[][] _stars;
		private readonly bool[] _rowsCovered;
		private readonly bool[] colsCovered;
		private float[][] costs;

		public Hungarian(float[][] theCosts)
		{
			costs = theCosts;
			_numRows = costs.Length;
			_numCols = costs[0].Length;

			_primes = new bool[_numRows][];
			_stars = new bool[_numRows][];

			_rowsCovered = new bool[_numRows];
			colsCovered = new bool[_numCols];
			for (var i = 0; i < _numRows; i++) _rowsCovered[i] = false;

			for (var j = 0; j < _numCols; j++) colsCovered[j] = false;

			for (var i = 0; i < _numRows; i++)
			{
				_primes[i] = new bool[_numCols];
				_stars[i] = new bool[_numCols];
				for (var j = 0; j < _numCols; j++)
				{
					_primes[i][j] = false;
					_stars[i][j] = false;
				}
			}
		}

		public int[][] Execute()
		{
			SubtractRowColMins();

			FindStars(); // O(n^2)
			ResetCovered(); // O(n);
			CoverStarredZeroCols(); // O(n^2)

			while (!AllColsCovered())
			{
				var primedLocation = PrimeUncoveredZero(); // O(n^2)
				if (primedLocation[0] == -1)
				{
					MinUncoveredRowsCols(); // O(n^2)
					primedLocation = PrimeUncoveredZero(); // O(n^2)
				}

				// is there a starred 0 in the primed zeros row?
				var primedRow = primedLocation[0];
				var starCol = FindStarColInRow(primedRow);
				if (starCol != -1)
				{
					// cover ther row of the primedLocation and uncover the star column
					_rowsCovered[primedRow] = true;
					colsCovered[starCol] = false;
				}
				else
				{
					// otherwise we need to find an augmenting path and start over.
					AugmentPathStartingAtPrime(primedLocation);
					ResetCovered();
					ResetPrimes();
					CoverStarredZeroCols();
				}
			}

			return StarsToAssignments(); // O(n^2)

		}

		public int[][] StarsToAssignments()
		{
			var toRet = new int[_numCols][];
			for (var j = 0; j < _numCols; j++)
				toRet[j] = new int[]
				{
					FindStarRowInCol(j), j
				}; // O(n)

			return toRet;
		}

		public void ResetPrimes()
		{
			for (var i = 0; i < _numRows; i++)
			for (var j = 0; j < _numCols; j++) _primes[i][j] = false;
		}

		public void ResetCovered()
		{
			for (var i = 0; i < _numRows; i++) _rowsCovered[i] = false;

			for (var j = 0; j < _numCols; j++) colsCovered[j] = false;
		}

		public void FindStars()
		{
			var rowStars = new bool[_numRows];
			var colStars = new bool[_numCols];

			for (var i = 0; i < _numRows; i++) rowStars[i] = false;

			for (var j = 0; j < _numCols; j++) colStars[j] = false;

			for (var j = 0; j < _numCols; j++)
			for (var i = 0; i < _numRows; i++)
				if (costs[i][j] == 0 && !rowStars[i] && !colStars[j])
				{
					_stars[i][j] = true;
					rowStars[i] = true;
					colStars[j] = true;
					break;
				}
		}

		private void MinUncoveredRowsCols()
		{
			// find min uncovered value
			var minUncovered = float.MaxValue;
			for (var i = 0; i < _numRows; i++)
				if (!_rowsCovered[i])
					for (var j = 0; j < _numCols; j++)
						if (!colsCovered[j])
							if (costs[i][j] < minUncovered) minUncovered = costs[i][j];

			// add that value to all the COVERED rows.
			for (var i = 0; i < _numRows; i++)
				if (_rowsCovered[i])
					for (var j = 0; j < _numCols; j++) costs[i][j] = costs[i][j] + minUncovered;

			// subtract that value from all the UNcovered columns
			for (var j = 0; j < _numCols; j++)
				if (!colsCovered[j])
					for (var i = 0; i < _numRows; i++) costs[i][j] = costs[i][j] - minUncovered;
		}

		private int[] PrimeUncoveredZero()
		{
			var location = new int[2];

			for (var i = 0; i < _numRows; i++)
				if (!_rowsCovered[i])
					for (var j = 0; j < _numCols; j++)
						if (!colsCovered[j])
							if (costs[i][j] == 0)
							{
								_primes[i][j] = true;
								location[0] = i;
								location[1] = j;
								return location;
							}

			location[0] = -1;
			location[1] = -1;
			return location;
		}

		private void AugmentPathStartingAtPrime(int[] location)
		{
			var primeLocations = new List<int[]>(_numRows + _numCols);
			var starLocations = new List<int[]>(_numRows + _numCols);
			primeLocations.Add(location);

			var currentRow = location[0];
			var currentCol = location[1];
			while (true)
			{
				// add stars and primes in pairs
				var starRow = FindStarRowInCol(currentCol);
				if (starRow == -1) break;

				var starLocation = new int[]
				{
					starRow, currentCol
				};
				starLocations.Add(starLocation);
				currentRow = starRow;

				var primeCol = FindPrimeColInRow(currentRow);
				var primeLocation = new int[]
				{
					currentRow, primeCol
				};
				primeLocations.Add(primeLocation);
				currentCol = primeCol;
			}

			UnStarLocations(starLocations);
			StarLocations(primeLocations);
		}


		private void StarLocations(List<int[]> locations)
		{
			for (var k = 0; k < locations.Count; k++)
			{
				var location = locations[k];
				var row = location[0];
				var col = location[1];
				_stars[row][col] = true;
			}
		}

		private void UnStarLocations(List<int[]> starLocations)
		{
			for (var k = 0; k < starLocations.Count; k++)
			{
				var starLocation = starLocations[k];
				var row = starLocation[0];
				var col = starLocation[1];
				_stars[row][col] = false;
			}
		}

		private int FindPrimeColInRow(int theRow)
		{
			for (var j = 0; j < _numCols; j++)
				if (_primes[theRow][j]) return j;

			return -1;
		}

		public int FindStarRowInCol(int theCol)
		{
			for (var i = 0; i < _numRows; i++)
				if (_stars[i][theCol]) return i;

			return -1;
		}


		public int FindStarColInRow(int theRow)
		{
			for (var j = 0; j < _numCols; j++)
				if (_stars[theRow][j]) return j;

			return -1;
		}

		private bool AllColsCovered()
		{
			for (var j = 0; j < _numCols; j++)
				if (!colsCovered[j]) return false;

			return true;
		}

		private void CoverStarredZeroCols()
		{
			for (var j = 0; j < _numCols; j++)
			{
				colsCovered[j] = false;
				for (var i = 0; i < _numRows; i++)
					if (_stars[i][j])
					{
						colsCovered[j] = true;
						break; // break inner loop to save a bit of time
					}
			}
		}

		private void SubtractRowColMins()
		{
			for (var i = 0; i < _numRows; i++)
			{
				//for each row
				var rowMin = float.MaxValue;
				for (var j = 0; j < _numCols; j++)
				// grab the smallest element in that row
					if (costs[i][j] < rowMin) rowMin = costs[i][j];

				for (var j = 0; j < _numCols; j++)
				// subtract that from each element
					costs[i][j] = costs[i][j] - rowMin;
			}

			for (var j = 0; j < _numCols; j++)
			{
				// for each col
				var colMin = float.MaxValue;
				for (var i = 0; i < _numRows; i++)
				// grab the smallest element in that column
					if (costs[i][j] < colMin) colMin = costs[i][j];

				for (var i = 0; i < _numRows; i++)
				// subtract that from each element
					costs[i][j] = costs[i][j] - colMin;
			}
		}

	}
}
