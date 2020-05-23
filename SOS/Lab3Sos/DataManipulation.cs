﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab3Sos
{
    internal class Package
    {
        public Package(int t, int p)
        {
            Time = t;
            Priority = p;
        }
        public int Time { get; set; }
        public int Priority { get; set; }

    }

    internal class PriorityQueue
    {
        public int size;
        public int avt = 0;
        public int dt = 0;
        public int number = 0;
        readonly List<Package> pq = new List<Package>();
        public PriorityQueue(int size)
        {
            this.size = size;
        }
        public void Tick()
        {
            avt += pq.Count;
            if (pq.Count == 0)
            {
                dt++;
            }
            else
            {
                pq[0].Time--;
                if (pq[0].Time == 0)
                {
                    pq.RemoveAt(0);
                }
            }
        }

        public void Add(Package pc)
        {
            if (pq.Count < size)
            {
                number++;
                int idx = 0;
                for (; idx < pq.Count; idx++)
                {
                    if (pq[idx].Priority > pc.Priority)
                    {
                        break;
                    }
                }
                pq.Insert(idx, pc);
            }
        }

    }
    internal static class GenFactory
    {
        public static IEnumerator<Package> getGenerator(int minTime, int maxTime,
            int minPriority, int maxPriority)
        {
            Random rnd = new Random();
            while (true)
            {
                yield return new Package(rnd.Next(minTime, maxTime),
                    rnd.Next(minPriority, maxPriority));
            }
        }
    }

}
