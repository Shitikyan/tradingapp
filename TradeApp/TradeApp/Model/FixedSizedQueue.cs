﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.Model
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public FixedSizedQueue(IEnumerable<T> collection)
            : base(collection)
        {
            Size = collection.Count();
        }


        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (this)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
