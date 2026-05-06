using System;
using System.Collections;
using System.Collections.Generic;

namespace MyShapeApp;

public class ShapeArray : IEnumerable<BaseShape>
{
    private BaseShape[] _items = new BaseShape[1000]; 
    public int Count { get; private set; } = 0;

    public void Add(BaseShape item) {
        if (Count < _items.Length) {
            _items[Count] = item; 
            Count++;
        }
    }

    public void Remove(BaseShape item) {
        int index = Array.IndexOf(_items, item, 0, Count);
        if (index != -1) {
            for (int i = index; i < Count - 1; i++) _items[i] = _items[i + 1];
            _items[Count - 1] = null!; 
            Count--;
        }
    }

    public void Clear() {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    public BaseShape this[int index] {
        get => _items[index];
        set => _items[index] = value;
    }

    public IEnumerator<BaseShape> GetEnumerator() {
        for (int i = 0; i < Count; i++) yield return _items[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}