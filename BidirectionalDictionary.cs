using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Copyright
/// MIT License
/// 
/// Copyright Fizz Cube Ltd(c) 2018 
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
/// 
/// ===
///
/// Copyright Marco Hoffmann(c) 2020
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
///furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
///
/// MIT License
/// </summary>

namespace EpicTransport {

    public class BidirectionalDictionary<T1, T2> : IEnumerable {
        private Dictionary<T1, T2> t1ToT2Dict = new Dictionary<T1, T2>();
        private Dictionary<T2, T1> t2ToT1Dict = new Dictionary<T2, T1>();

        public IEnumerable<T1> FirstTypes => t1ToT2Dict.Keys;
        public IEnumerable<T2> SecondTypes => t2ToT1Dict.Keys;

        public IEnumerator GetEnumerator() => t1ToT2Dict.GetEnumerator();

        public int Count => t1ToT2Dict.Count;

        public void Add(T1 key, T2 value) {
            t1ToT2Dict[key] = value;
            t2ToT1Dict[value] = key;
        }

        public void Add(T2 key, T1 value) {
            t2ToT1Dict[key] = value;
            t1ToT2Dict[value] = key;
        }

        public T2 Get(T1 key) => t1ToT2Dict[key];

        public T1 Get(T2 key) => t2ToT1Dict[key];

        public bool TryGetValue(T1 key, out T2 value) => t1ToT2Dict.TryGetValue(key, out value);

        public bool TryGetValue(T2 key, out T1 value) => t2ToT1Dict.TryGetValue(key, out value);

        public bool Contains(T1 key) => t1ToT2Dict.ContainsKey(key);

        public bool Contains(T2 key) => t2ToT1Dict.ContainsKey(key);

        public void Remove(T1 key) {
            if (Contains(key)) {
                T2 val = t1ToT2Dict[key];
                t1ToT2Dict.Remove(key);
                t2ToT1Dict.Remove(val);
            }
        }
        public void Remove(T2 key) {
            if (Contains(key)) {
                T1 val = t2ToT1Dict[key];
                t1ToT2Dict.Remove(val);
                t2ToT1Dict.Remove(key);
            }
        }

        public T1 this[T2 key] {
            get => t2ToT1Dict[key];
            set {
                t2ToT1Dict[key] = value;
                t1ToT2Dict[value] = key;
            }
        }

        public T2 this[T1 key] {
            get => t1ToT2Dict[key];
            set {
                t1ToT2Dict[key] = value;
                t2ToT1Dict[value] = key;
            }
        }

    }
}