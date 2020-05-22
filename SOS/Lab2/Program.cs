using System;

namespace Lab2
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var m = new Memory(4096, 256);
            var maxSize = 500;
            var n = 100;
            var ptr = new int[n];
            var random = new Random();
            for (var i = 0; i < ptr.Length; i++)
                ptr[i] = -1;
            for (var i = 0; i < 10000000; i++)
            {
                var index = (int) (random.NextDouble() * (n - 1));
                Console.WriteLine(i + ") ");
                if (ptr[index] == -1)
                {
                    var size = (int) (random.NextDouble() * maxSize) + 1;
                    Console.WriteLine("alloc " + size + " ");
                    ptr[index] = m.MemAllocation(size);
                }
                else
                {
                    if (random.NextDouble() > 0.5)
                    {
                        Console.WriteLine("free ");
                        m.MemFree(ptr[index]);
                        ptr[index] = -1;
                    }
                    else
                    {
                        var size = (int) (random.NextDouble() * maxSize) + 1;
                        Console.WriteLine("realloc " + size + ' ');
                        var t = m.MemRealoc(ptr[index], size);
                        if (t != -1) ptr[index] = t;
                    }
                }

                if (!m.Test())
                    break;
                Console.WriteLine("OK");
            }

            if (m.Test())
                Console.WriteLine("Test success");
        }
    }


    public class Pages
    {
        public Pages(int Pointer)
        {
            this._pointer = Pointer;
            next = null;
            _prev = null;
        }

        public Pages(Pages prev, int Pointer)
        {
            next = null;
            this._pointer = Pointer;
            this._prev = prev;
        }

        public void SetNext(Pages next)
        {
            this.next = next;
        }

        public Pages GetNext()
        {
            return next;
        }

        public void SetPrev(Pages prev)
        {
            this._prev = prev;
        }

        public Pages GetPrev()
        {
            return _prev;
        }

        public void SetPointer(int pointer)
        {
            _pointer = pointer;
        }

        public int GetPointer()
        {
            return _pointer;
        }

        public Pages next;
        private Pages _prev;
        private int _pointer;
    }

    public class Memory
    {
        public Memory(int size, int sizePage)
        {
            this.sizePage = sizePage;
            var t = size / sizePage;
            t *= 3;
            t += size;
            CurrentMemory = new int[t];
            _correctPtr = new int[t / (sizePage + 3)];
            t = minSize;
            var c = 0;
            while (t <= sizePage >> 1)
            {
                c++;
                t = t << 1;
            }

            _ptrs = new int[c];
            int i;
            for (i = 0; i < c; i++)
                _ptrs[i] = -1;
            i = 0;
            var ptr = 0;
            _correctPtr[i] = ptr;
            i++;
            _freeH = Page(0);
            var l = _freeH;
            ptr += sizePage + 3;
            while (ptr < CurrentMemory.Length)
            {
                _correctPtr[i] = ptr;
                i++;
                SetNextPtr(l, Page(l, ptr));
                l = GetNextPtr(l);
                ptr += sizePage + 3;
            }
        }

        private int Page(int i)
        {
            SetH(i, 0);
            SetCs(i, 0);
            SetPs(i, 0);
            return i;
        }

        private int Page(int i, int ptr)
        {
            SetH(ptr, 0);
            SetCs(ptr, 0);
            SetPs(ptr, Math.Abs(ptr - i));
            return ptr;
        }

        public int MemAllocation(int size)
        {
            if (size <= sizePage >> 1)
            {
                var s = minSize;
                var i = 0;
                while (s < size)
                {
                    i++;
                    s = s << 1;
                }

                if (_ptrs[i] == -1)
                {
                    if (_freeH == -1)
                        return -1;
                    _ptrs[i] = _freeH;
                    var ptr = _freeH;
                    if (GetCs(_freeH) != 0)
                        _freeH = GetNextPtr(_freeH);
                    else
                        _freeH = -1;
                    if (_freeH != -1)
                        SetPs(_freeH, 0);
                    setSize(ptr, s);
                    var t = 1;
                    t = t << (sizePage / s - 1);
                    SetBitMap(ptr, t);
                    SetPs(ptr, 0);
                    return ptr + 3;
                }

                var b = false;
                var l = _ptrs[i];
                while (l != -1)
                {
                    if (GetCounter(l) < sizePage / getSize(l))
                    {
                        b = true;
                        break;
                    }

                    if (GetPs(l) == 0)
                        break;
                    l = GetPrevPtr(l);
                }

                if (b)
                {
                    var ptr = l;
                    var t = 1;
                    t = t << (sizePage / s - 1);
                    i = 0;
                    var map = GetBitMap(ptr);
                    while ((map & t) != 0)
                    {
                        i++;
                        t = t >> 1;
                    }

                    SetBitMap(ptr, map | t);
                    ptr += getSize(ptr) * i + 3;
                    return ptr;
                }
                else
                {
                    if (_freeH == -1)
                        return -1;
                    var p = _freeH;
                    if (GetCs(_freeH) != 0)
                    {
                        _freeH = GetNextPtr(_freeH);
                        SetPs(_freeH, 0);
                    }
                    else
                    {
                        _freeH = -1;
                    }

                    p = Page(p);
                    SetPs(p, p - _ptrs[i]);
                    _ptrs[i] = p;
                    var ptr = p;
                    setSize(ptr, s);
                    var t = 1;
                    t = t << (sizePage / s - 1);
                    SetBitMap(ptr, t);
                    return ptr + 3;
                }
            }

            {
                var n = 1;
                while (size > sizePage * n)
                    n++;
                var p = GetNFreePages(n);
                if (p == -1)
                    return -1;
                var t = p;
                for (var i = 0; i < n; i++)
                {
                    if (GetCs(t) == 0)
                    {
                        t = -1;
                        break;
                    }

                    t = GetNextPtr(t);
                }

                int ptr;
                if (p == _freeH)
                {
                    if (t != -1)
                        SetPs(t, 0);
                    _freeH = t;
                }
                else
                {
                    SetNextPtr(GetPrevPtr(p), t);
                    SetPrevPtr(t, GetPrevPtr(p));
                    ptr = p;
                    for (var i = 0; i < n; i++)
                    {
                        SetH(ptr, 1);
                        SetCs(ptr, sizePage + 3);
                        SetPs(ptr, sizePage + 3);
                        ptr += sizePage + 3;
                    }

                    ptr -= sizePage + 3;
                    SetPs(p, 0);
                    SetCs(ptr, 0);
                    _nBpages += n;
                    return p + 3;
                }

                ptr = p;
                for (var i = 0; i < n; i++)
                {
                    SetH(ptr, 1);
                    SetCs(ptr, sizePage + 3);
                    SetPs(ptr, sizePage + 3);
                    ptr += sizePage + 3;
                }

                SetPs(p, 0);
                SetCs(ptr - sizePage - 3, 0);
                _nBpages += n;
                return p + 3;
            }
        }

        public void MemFree(int ptr)
        {
            var ptrS = ptr / (sizePage + 3);
            ptrS *= sizePage + 3;
            if (getSize(ptrS) != 1)
            {
                if (GetCounter(ptrS) > 1)
                {
                    var n = ptr - ptrS - 3;
                    n /= getSize(ptrS);
                    n = sizePage / getSize(ptrS) - n;
                    var t = 1;
                    t = t << (n - 1);
                    SetBitMap(ptrS, GetBitMap(ptrS) ^ t);
                }
                else
                {
                    var s = minSize;
                    var i = 0;
                    while (s < getSize(ptrS))
                    {
                        i++;
                        s = s << 1;
                    }

                    var p = ptrS;
                    if (p == _ptrs[i])
                    {
                        if (GetPs(_ptrs[i]) == 0)
                            _ptrs[i] = -1;
                        else
                            _ptrs[i] = GetPrevPtr(_ptrs[i]);
                    }
                    else
                    {
                        if (GetPs(p) == 0)
                        {
                            SetPs(GetNexti(i, p), 0);
                        }
                        else
                        {
                            var t = GetNexti(i, p);
                            SetPs(t, t - GetPrevPtr(p));
                        }
                    }

                    AddFreePage(ptrS);
                }
            }
            else
            {
                while (GetCs(ptrS) != 0)
                {
                    AddFreePage(ptrS);
                    ptrS += sizePage + 3;
                    _nBpages--;
                }

                AddFreePage(ptrS);
                _nBpages--;
            }
        }

        public int GetNexti(int i, int ptr)
        {
            var p = _ptrs[i];
            while (GetPrevPtr(p) != ptr)
                p = GetPrevPtr(p);
            return p;
        }

        private int FindFreePages(int ptrH, int ptrL, int n)
        {
            var r = 0;
            var l = 0;
            if (ptrL + sizePage + 3 < CurrentMemory.Length)
                ptrL += sizePage + 3;
            while (GetCs(ptrL) != 0)
            {
                if (GetH(ptrL) != 0)
                    break;
                r++;
                ptrL += sizePage + 3;
            }

            if (ptrH - (sizePage + 3) >= 0)
                ptrH -= sizePage + 3;
            while (GetPs(ptrH) != 0)
            {
                if (GetH(ptrH) != 0)
                    break;
                l++;
                ptrH -= sizePage + 3;
            }

            if (l + r >= n)
                return r;
            return -1;
        }

        public int MemRealoc(int ptr, int size)
        {
            var ptrS = ptr / (sizePage + 3);
            ptrS *= sizePage + 3;
            if (getSize(ptrS) != 1)
            {
                // ptr <= sizePage/2
                if (size <= sizePage >> 1)
                {
                    // new ptr <= sizePage/2
                    var s = minSize;
                    var i = 0;
                    while (s < size)
                    {
                        i++;
                        s = s << 1;
                    }

                    if (_ptrs[i] != -1)
                        if (getSize(_ptrs[i]) == getSize(ptrS))
                            return ptr;
                    var t = MemAllocation(size);
                    if (t != -1)
                    {
                        var tS = t / (sizePage + 3);
                        tS = tS * (sizePage + 3);
                        var d = getSize(ptrS);
                        if (d > getSize(tS))
                            d = getSize(tS);
                        for (var j = 0; j < d; j++)
                            CurrentMemory[t + j] = CurrentMemory[ptr + j];
                        MemFree(ptr);
                    }

                    return t;
                }
                else
                {
                    // new ptr > sizePage/2
                    var t = MemAllocation(size);
                    if (t != -1)
                    {
                        for (var j = 0; j < getSize(ptrS); j++)
                            CurrentMemory[t + j] = CurrentMemory[ptr + j];
                        MemFree(ptr);
                    }

                    return t;
                }
            } // ptr > sizePage./2

            if (size <= sizePage >> 1)
            {
                // new ptr <= sizePage/2
                var t = MemAllocation(size);
                if (t != -1)
                {
                    var tS = t / (sizePage + 3);
                    tS = tS * (sizePage + 3);
                    for (var j = 0; j < getSize(tS); j++)
                        CurrentMemory[t + j] = CurrentMemory[ptr + j];
                    MemFree(ptr);
                }

                return t;
            } // new ptr > sizePage/2

            var n = 1;
            while (size > sizePage * n)
                n++;
            var cn = 1;
            var tptr = ptrS;
            while (GetCs(tptr) != 0)
            {
                cn++;
                tptr += sizePage + 3;
            }

            if (n == cn)
                return ptr;
            if (cn > n)
            {
                var d = cn - n;
                tptr = ptrS;
                for (var i = 0; i < cn - d; i++)
                    tptr += sizePage + 3;
                SetCs(tptr - (sizePage + 3), 0);
                while (GetCs(tptr) != 0)
                {
                    AddFreePage(tptr);
                    _nBpages--;
                    tptr += sizePage + 3;
                }

                AddFreePage(tptr);
                _nBpages--;
                return ptr;
            }
            else
            {
                var d = n - cn;
                tptr = ptrS;
                while (GetCs(tptr) != 0)
                    tptr += sizePage + 3;
                var r = FindFreePages(ptrS, tptr, d);
                if (r != -1)
                {
                    if (r >= d)
                    {
                        var p = _freeH;
                        while (p != -1)
                        {
                            if (p == tptr + sizePage + 3)
                                break;
                            p = GetNextPtr(p);
                        }

                        var t = p;
                        for (var i = 0; i < d; i++)
                            t = GetNextPtr(t);
                        if (p == _freeH)
                        {
                            if (t != -1)
                                SetPs(t, 0);
                            _freeH = t;
                        }
                        else
                        {
                            SetNextPtr(GetPrevPtr(p), t);
                            if (t != -1)
                                SetPrevPtr(t, GetPrevPtr(p));
                        }

                        SetCs(tptr, sizePage + 3);
                        tptr = p;
                        for (var i = 0; i < d; i++)
                        {
                            SetH(tptr, 1);
                            SetCs(tptr, sizePage + 3);
                            SetPs(tptr, sizePage + 3);
                            tptr += sizePage + 3;
                        }

                        SetCs(tptr - sizePage - 3, 0);
                        _nBpages += d;
                        return ptr;
                    }
                    else
                    {
                        var p = _freeH;
                        var t = p;
                        if (r != 0)
                        {
                            p = _freeH;
                            while (p != -1)
                            {
                                if (p == tptr + sizePage + 3)
                                    break;
                                p = GetNextPtr(p);
                            }

                            t = p;
                            for (var i = 0; i < r; i++)
                                t = GetNextPtr(t);
                            if (p == _freeH)
                            {
                                if (t != -1)
                                    SetPs(t, 0);
                                _freeH = t;
                            }
                            else
                            {
                                SetNextPtr(GetPrevPtr(p), t);
                                if (t != -1)
                                    SetPrevPtr(t, GetPrevPtr(p));
                            }

                            SetCs(tptr, sizePage + 3);
                            tptr = p;
                            for (var i = 0; i < r; i++)
                            {
                                SetH(tptr, 1);
                                SetCs(tptr, sizePage + 3);
                                SetPs(tptr, sizePage + 3);
                                tptr += sizePage + 3;
                            }

                            SetCs(tptr - sizePage - 3, 0);
                        }

                        tptr = ptrS;
                        p = _freeH;
                        while (p != -1)
                        {
                            if (p == tptr - (sizePage + 3))
                                break;
                            p = GetNextPtr(p);
                        }

                        t = p;
                        for (var i = 0; i < d - r - 1; i++)
                            p = GetPrevPtr(p);
                        if (p == _freeH)
                        {
                            if (t != -1)
                                SetPs(t, 0);
                            _freeH = t;
                        }
                        else
                        {
                            if (GetCs(t) != 0)
                            {
                                t = GetNextPtr(t);
                                SetNextPtr(GetPrevPtr(p), t);
                                SetPrevPtr(t, GetPrevPtr(p));
                            }
                            else
                            {
                                SetCs(GetPrevPtr(p), 0);
                            }
                        }

                        tptr = p;
                        for (var i = 0; i < d - r; i++)
                        {
                            SetH(tptr, 1);
                            SetCs(tptr, sizePage + 3);
                            SetPs(tptr, sizePage + 3);
                            tptr += sizePage + 3;
                        }

                        tptr = p;
                        for (var i = 0; i < cn; i++)
                        {
                            for (var j = 0; j < sizePage + 3; j++)
                                CurrentMemory[tptr + j] = CurrentMemory[ptrS + j];
                            tptr += sizePage + 3;
                            ptrS += sizePage + 3;
                        }

                        SetCs(tptr, sizePage + 3);
                        _nBpages += d;
                        return p;
                    }
                }

                {
                    var t = MemAllocation(size);
                    if (t != -1)
                    {
                        tptr = ptrS;
                        for (var i = 0; i < cn; i++)
                        {
                            for (var j = 0; j < GetCs(ptrS); j++)
                                CurrentMemory[t + j] = CurrentMemory[ptrS + j];
                            ptrS += sizePage + 3;
                        }

                        MemFree(tptr);
                    }

                    return t;
                }
            }
        }

        private int GetNFreePages(int n)
        {
            var p = _freeH;
            if (p != -1)
            {
                if (n == 1)
                    return p;
                while (GetCs(p) != 0)
                {
                    var find = true;
                    for (var i = 0; i < n; i++)
                        if (GetCs(p) == 0)
                        {
                            find = false;
                            break;
                        }
                        else
                        {
                            if (p + sizePage + 3 != GetNextPtr(p))
                            {
                                find = false;
                                break;
                            }

                            p = GetNextPtr(p);
                        }

                    if (find)
                    {
                        for (var i = 0; i < n; i++)
                            p = GetPrevPtr(p);
                        return p;
                    }

                    if (GetCs(p) != 0)
                        p = GetNextPtr(p);
                    else
                        break;
                }
            }

            return -1;
        }

        public bool TestPtr(int ptr)
        {
            for (var i = 0; i < _correctPtr.Length; i++)
                if (ptr == _correctPtr[i])
                    return true;
            throw new Exception(" test ptr ");
            return false;
        }

        private void AddFreePage(int ptr)
        {
            if (_freeH == -1)
            {
                _freeH = Page(ptr);
            }
            else
            {
                SetH(ptr, 0);
                var p = _freeH;
                while (p < ptr)
                    if (GetCs(p) == 0)
                        break;
                    else
                        p = GetNextPtr(p);
                SetNextPtr(ptr, p);
                if (ptr < p)
                {
                    if (p == _freeH)
                    {
                        _freeH = ptr;
                        SetPs(ptr, 0);
                    }
                    else
                    {
                        SetPrevPtr(ptr, GetPrevPtr(p));
                        SetNextPtr(GetPrevPtr(p), ptr);
                    }

                    SetPrevPtr(p, ptr);
                }
                else
                {
                    ptr = Page(ptr);
                    SetNextPtr(p, ptr);
                    SetPrevPtr(ptr, p);
                }
            }
        }

        public bool Test()
        {
            if (_nBpages < 0)
            {
                throw new Exception(" nBpages ");
                return false;
            }

            if (_freeH != -1)
                if (GetPs(_freeH) != 0)
                {
                    throw new Exception(" freeH ");
                    return false;
                }

            var ptr = 0;
            var n = 0;
            while (ptr < CurrentMemory.Length)
            {
                n++;
                ptr += sizePage + 3;
            }

            var cn = 0;
            var p = _freeH;
            var pointers = new int[n];
            while (p != -1)
            {
                if (!TestPtr(p))
                    return false;
                pointers[cn] = p;
                cn++;
                if (GetCs(p) == 0)
                    break;
                p = GetNextPtr(p);
                if (p != -1)
                    if (GetPrevPtr(p) > p)
                    {
                        throw new Exception(" free pages ");
                        return false;
                    }
            }

            for (var i = 0; i < cn; i++)
            for (var j = i + 1; j < cn; j++)
                if (pointers[i] == pointers[j])
                {
                    throw new Exception(" ptr=ptr ");
                    return false;
                }

            for (var i = 0; i < _ptrs.Length; i++)
            {
                p = _ptrs[i];
                while (p != -1)
                {
                    if (!TestPtr(p))
                        return false;
                    if (GetCounter(p) < 1)
                        break;
                    cn++;
                    if (GetPs(p) == 0)
                        break;
                    p = GetPrevPtr(p);
                }
            }

            cn += _nBpages;
            if (cn != n)
            {
                throw new Exception(" Pages ");
                return false;
            }

            return true;
        }

        private void setSize(int ptr, int size)
        {
            CurrentMemory[ptr] = size;
        }

        private int getSize(int ptr)
        {
            return CurrentMemory[ptr];
        }

        private void SetBitMap(int ptr, int map) => CurrentMemory[ptr + 1] = map;

        private int GetBitMap(int ptr) => CurrentMemory[ptr + 1];

        private int GetCounter(int ptr)
        {
            var t = 1;
            var c = 0;
            while (t != 0)
            {
                if ((GetBitMap(ptr) & t) > 0)
                    c++;
                t = t << 1;
            }

            return c;
        }

        public void SetCs(int ptr, int value) => CurrentMemory[ptr + 1] = value;

        private int GetCs(int ptr) => CurrentMemory[ptr + 1];

        private int GetPs(int ptr) => CurrentMemory[ptr + 2];

        private void SetPs(int ptr, int value) => CurrentMemory[ptr + 2] = value;

        private int GetH(int ptr) => CurrentMemory[ptr];

        private void SetH(int ptr, int value) => CurrentMemory[ptr] = value;

        private int GetNextPtr(int ptr) => ptr + CurrentMemory[ptr + 1];

        private int GetPrevPtr(int ptr) => ptr - CurrentMemory[ptr + 2];

        private void SetNextPtr(int ptr, int next) => CurrentMemory[ptr + 1] = Math.Abs(ptr - next);

        private void SetPrevPtr(int ptr, int pr) => CurrentMemory[ptr + 2] = Math.Abs(ptr - pr);

        private readonly int[] _correctPtr;
        private readonly int[] CurrentMemory;
        private readonly int[] _ptrs;
        private int _freeH;
        private int _nBpages;
        private readonly int sizePage;
        private readonly int minSize = 16;
    }
}