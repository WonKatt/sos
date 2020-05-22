using System;

namespace SOSLab1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
	        var random = new  Random();
	        var m = new mem(1000);
	        var maxSize = 100;
	        var n = 100;
	        var ptr = new int[n];
	        var controlSum = new int[n];
	        for (var i = 0; i < ptr.Length; i++)
		        ptr[i] = -1;
	        for (var i = 0; i < 1000000; i++) {
		        var index = (int) (random.NextDouble()  * (n - 1));
		        m.Blocks();
		        if (ptr[index] == -1) {
			        var size = (int)random.NextDouble() * maxSize;
			        ptr[index] = m.mem_alloc(size);
			        m.FillBlock(ptr[index]);
			        controlSum[index] = m.GetControlSum(ptr[index]);
		        } else {
			        if (random.NextDouble()  > 0.5) {
				        m.MemFree(ptr[index]);
				        ptr[index] = -1;
			        } else {
				        var t = m.MemRealloc(ptr[index],
					        (int) (random.NextDouble()  * maxSize));
				        if (t != -1) {
					        var s = m.GetControlSum(t);
					        if (s != controlSum[index] && t != ptr[index]
					                                     && s > controlSum[index]) {
						        Console.WriteLine("ERROR: Error control sum");
						        break;
					        }
					        ptr[index] = t;
					        m.FillBlock(ptr[index]);
					        controlSum[index] = m.GetControlSum(ptr[index]);
				        }
			        }
		        }
		        if (m.TestPtrs() == -1)
			        break;
	        }
        }
    }
    public class mem {
	public mem(int memSize) {
		if (memSize < 10)
			memSize = 10;
		_memory = new int[memSize];
		_memory[0] = 1;
		_memory[1] = 3;
		_memory[2] = 0;
		_memory[4] = memSize - 6;
		_memory[5] = 3;
		_memory[memSize - 3] = 1;
		_memory[memSize - 2] = 0;
		_memory[memSize - 1] = memSize - 3;

	}
	//выдиление памяти
	public int mem_alloc(int size) {
		if (size < 1)
			size = 1;
		var index = GetNextPtr(0);
		var ptr = -1;
		while (GetOffsetN(index) != 0) {
			if (GetHeader(index) != 1 && GetOffsetN(index) - 3 >= size) {
				SetHeader(index, 1);
				if (GetOffsetN(index) - 7 < size) return index;
				var Nptr = index + size + 3;
				var next = GetNextPtr(index);
				var dCs = next - Nptr;
				var dPs = Nptr - index;
				SetHeader(Nptr, 0);
				SetOffsetP(next, dCs);
				SetOffsetN(Nptr, dCs);
				SetOffsetN(index, dPs);
				SetOffsetP(Nptr, dPs);
				return index;
			}
			index = GetNextPtr(index);
		}
		return ptr;
	}
	//Освобождение памяти
	public void MemFree(int ptr) {
		var ps = GetPrevPtr(ptr);
		if (GetHeader(ps) != 1) {
			ps = GetPrevPtr(ps);
			if (GetHeader(ps) != 1)
				Console.WriteLine("ERROR: ERROR offset prev ");
		}
		var cs = GetNextPtr(ptr);
		if (GetHeader(cs) != 1) {
			cs = GetNextPtr(cs);
			if (GetHeader(cs) != 1)
				Console.WriteLine("ERROR: ERROR offset next ");
		}
		var freePtr = GetNextPtr(ps);
		SetHeader(freePtr, 0);
		var dCs = cs - freePtr;
		SetOffsetN(freePtr, dCs);
		SetOffsetP(cs, dCs);
		SetHeader(ptr, 0);
		for (var i = 3; i < GetOffsetN(freePtr); i++)
			_memory[freePtr + i] = 0;

	}
	//изменение розмеров выделеной памяти
	public int MemRealloc(int ptr, int size) {
		if (size == 0)
			size = 1;
		var csize = GetOffsetN(ptr) - 3;
		var d = Math.Abs(csize - size);
		if (csize == size)
			return ptr;
		if (csize > size) { //уменшение
			var index = ptr + size + 3;
			var next = GetNextPtr(ptr);
			if (GetHeader(next) != 1) {//с корекцией пустой ячейки с права
				var oldCs = GetOffsetN(next);
				var oldPs = GetOffsetP(next);
				SetHeader(index, 0);
				SetOffsetN(index, oldCs + d);
				SetOffsetP(index, oldPs - d);
				SetOffsetN(ptr, GetOffsetN(ptr) - d);
				next = GetNextPtr(index);
				SetOffsetP(next, GetOffsetP(next) + d);
				for (var i = 3; i < GetOffsetN(index); i++)
					_memory[index + i] = 0;
			} else {
				if (d > 3) {//с созданием пустой ячейки с права
					SetHeader(index, 0);
					SetOffsetN(index, next - index);
					SetOffsetP(index, size + 3);
					SetOffsetP(next, next - index);
					SetOffsetN(ptr, size + 3);
					for (var i = 3; i < GetOffsetN(index); i++)
						_memory[index + i] = 0;
				}
			}
			return ptr;
		} else {//увеличение
			var next = GetNextPtr(ptr);
			if (GetHeader(next) != 1) {//если с права пусто
				if (GetOffsetN(next) >= d) {//если места достаточно
					if (GetOffsetN(next) - d > 3) {//с созданием пустой ячейки
						var oldCs = GetOffsetN(next);
						var oldPs = GetOffsetP(next);
						next += d;
						SetOffsetN(next, oldCs - d);
						SetOffsetP(next, oldPs + d);
						SetHeader(next, 0);
						SetOffsetN(ptr, GetOffsetN(ptr) + d);
						next = GetNextPtr(next);
						SetOffsetP(next, GetOffsetP(next) - d);
						for (var i = GetOffsetN(ptr) - d; i < GetOffsetN(ptr); i++)
							_memory[ptr + i] = 0;
					} else {
						var oldCs = GetOffsetN(ptr);
						var cs = GetOffsetN(ptr) + GetOffsetN(next);
						SetOffsetN(ptr, cs);
						next = GetNextPtr(ptr);
						SetOffsetP(next, cs);
						for (var i = oldCs; i < GetOffsetN(ptr); i++)
							_memory[ptr + i] = 0;
					}
				} else {
					var prev = GetPrevPtr(ptr);
					if (GetHeader(prev) != 1) {//если с лева пусто
						if (GetOffsetN(next) + GetOffsetN(prev) >= d) {//если места с лева и права достаточно
							if (GetOffsetN(next) + GetOffsetN(prev) - d > 3) {//с созданием
								var oldCs = GetOffsetN(ptr);
								var delta = d - GetOffsetN(next);
								SetOffsetN(prev, GetOffsetN(prev) - delta);
								SetOffsetP(ptr, GetOffsetP(ptr) - delta);
								SetOffsetN(ptr, GetOffsetN(ptr) + d);
								next = GetNextPtr(next);
								SetOffsetP(next, GetOffsetN(ptr));
								for (var i = ptr; i < ptr + oldCs; i++)
									_memory[i - delta] = _memory[i];
								ptr -= delta;
								for (var i = oldCs; i < GetOffsetN(ptr); i++)
									_memory[ptr + i] = 0;
							} else {
								var t = GetOffsetN(prev) + GetOffsetN(ptr) + GetOffsetN(next);
								var oldCs = GetOffsetN(ptr);
								var oldPs = GetOffsetP(ptr);
								SetOffsetN(ptr, t);
								SetOffsetP(ptr, GetOffsetP(prev));
								next = GetNextPtr(next);
								SetOffsetP(next, t);
								for (var i = ptr; i < ptr + oldCs; i++)
									_memory[i - oldPs] = _memory[i];
								ptr -= oldPs;
								for (var i = oldCs; i < GetOffsetN(ptr); i++)
									_memory[ptr + i] = 0;
							}
						} else {//найти подходящее место
							var t = mem_alloc(size);
							if (t != -1) {
								for (var i = 3; i < GetOffsetN(ptr); i++)
									_memory[t + i] = _memory[ptr + i];
								MemFree(ptr);
								return t;
							}
							return ptr;
						}
					}
				}
				return ptr;
			} else if (GetHeader(GetPrevPtr(ptr)) != 1) {//если с лева пусто
				var prev = GetPrevPtr(ptr);
				if (GetOffsetP(ptr) >= d) {//если достаточно места
					if (GetOffsetP(ptr) - d > 3) {//с созданием
						var oldCs = GetOffsetN(ptr);
						SetOffsetN(prev, GetOffsetN(prev) - d);
						SetOffsetN(ptr, GetOffsetN(ptr) + d);
						SetOffsetP(ptr, GetOffsetP(ptr) - d);
						SetOffsetP(next, GetOffsetP(next) + d);
						for (var i = ptr; i < ptr + oldCs; i++)
							_memory[i - d] = _memory[i];
						ptr -= d;
						for (var i = oldCs; i < GetOffsetN(ptr); i++)
							_memory[ptr + i] = 0;
					} else {
						SetHeader(prev, 1);
						var cs = GetOffsetN(prev);
						var oldCs = GetOffsetN(ptr);
						SetOffsetN(prev, GetOffsetN(ptr) + cs);
						SetOffsetP(next, GetOffsetN(ptr) + cs);
						for (var i = ptr + 3; i < next; i++)
							_memory[i - cs] = _memory[i];
						for (var i = oldCs; i < GetOffsetN(prev); i++)
							_memory[prev + i] = 0;
						return prev;
					}
				} else {//найти подходящее место
					var t = mem_alloc(size);
					if (t != -1) {
						for (var i = 3; i < GetOffsetN(ptr); i++)
							_memory[t + i] = _memory[ptr + i];
						MemFree(ptr);
						return t;
					}
				}
				return ptr;
			}
			return ptr;
		}

	}
	//Вивод виделеной памяти (заголовков)
	public void Blocks() {
		var ptr = 0;
		var t = 0;
		while (GetOffsetN(ptr) != 0) {
			Console.WriteLine(GetHeader(ptr) + "| " + GetOffsetN(ptr) + "| " + GetOffsetP(ptr)
					+ "| ");
			ptr = GetNextPtr(ptr);
			if (t > _memory.Length) {
				Console.WriteLine();
				break;
			}
			t++;
		}
		Console.WriteLine(GetHeader(ptr) + " " + GetOffsetN(ptr) + " " + GetOffsetP(ptr)
				+ " ");
	}
	//проверка коректности заголовков
	public int TestPtrs() {
		var ptr = GetNextPtr(0);
		var prev = 0;
		var t = 0;
		while (ptr != _memory.Length - 3) {
			if (GetHeader(prev) == 0 && GetHeader(ptr) == 0) {
				Console.WriteLine("ERROR: heads = 0 ");
				return -1;
			}
			if (prev != GetPrevPtr(ptr)) {
				Console.WriteLine("ERROR: prev ");
				return -1;
			}
			if (t > _memory.Length) {
				Console.WriteLine("ERROR: t > length ");
				return -1;
			}
			if (GetOffsetN(ptr) < 4) {
				Console.WriteLine("ERROR: next < 4 ");
				return -1;
			}
			ptr = GetNextPtr(ptr);
			prev = GetNextPtr(prev);
			t++;
		}
		return 0;
	}
	//заполнение виделеней памяти
	public void FillBlock(int ptr) {
		var maxValue = 100;
		var random = new Random();
		for (var i = 3; i < GetOffsetN(ptr); i++)
			_memory[ptr + i] = (int)random.NextDouble() * maxValue;
	}
	//Контрольная сума выделеной памяти
	public int GetControlSum(int ptr) {
		var s = 0;
		for (var i = 3; i < GetOffsetN(ptr); i++)
			s += _memory[ptr + i];
		return s;
	}
	private int GetNextPtr(int ptr) {
		return ptr + _memory[ptr + 1];
	}

	private int GetPrevPtr(int ptr) {
		return ptr - _memory[ptr + 2];
	}

	private int GetOffsetN(int ptr) {
		return _memory[ptr + 1];
	}

	private int GetOffsetP(int ptr) {
		return _memory[ptr + 2];
	}

	private int GetHeader(int ptr) {
		return _memory[ptr];
	}

	private void SetOffsetN(int ptr, int value) {
		_memory[ptr + 1] = value;
	}

	private void SetOffsetP(int ptr, int value) {
		_memory[ptr + 2] = value;
	}

	private void SetHeader(int ptr, int value) {
		_memory[ptr] = value;
	}
	private readonly int[] _memory;
}
}