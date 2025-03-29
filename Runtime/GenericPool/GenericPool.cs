using System.Collections.Generic;
using UnityEngine;

namespace FingTools.Internal
{
    public class GenericPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool;
        private readonly int _maxSize;

        public GenericPool(int maxSize)
        {
            _maxSize = maxSize;
            _pool = new Stack<T>(maxSize); // Initialize the stack with the specified max size
        }
        
        public virtual T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new T(); // Create a new instance if the pool is empty
        }

        public virtual void Release(T item)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(item);
            }
            else
            {
                Debug.LogWarning("Pool is full. Cannot release the item.");
            }
        }
    }
}