using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Svelto.Common;

namespace Svelto.DataStructures
{
    sealed class NBDebugProxy<T> where T : struct
    {
        NB<T> m_Array;

        public NBDebugProxy(NB<T> array)
        {
            this.m_Array = array;
        }

        public T[] Items
        {
            get
            {
                T[] array = new T[m_Array.capacity];
                
                m_Array.CopyTo(0, array, 0, (uint) m_Array.capacity);

                return array;
            }
        }
    }

    /// <summary>
    /// NB stands for NativeBuffer
    /// 
    /// NativeBuffers were initially mainly designed to be used inside Unity Jobs. They wrap an EntityDB array of components
    /// but do not track it. Hence it's meant to be used temporary and locally as the array can become invalid
    /// after a submission of entities. However they cannot be used as ref struct
    ///
    /// ------> NBs are wrappers of native arrays. Are not meant to resize or be freed
    ///
    /// NBs cannot have a count, because a count of the meaningful number of items is not tracked.
    /// Example: an NB could be initialized with a size 10 and count 0. Then the buffer is used to fill entities
    /// but the count will stay zero. It's not the NB responsibility to track the count
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    
    [DebuggerTypeProxy(typeof(NBDebugProxy<>))]
    public struct NB<T>:IBuffer<T> where T:struct
    {
        static NB()
        {
            if (TypeCache<T>.IsUnmanaged == false)
                throw new Exception("NativeBuffer (NB) supports only unmanaged types");
        }
        
        public NB(IntPtr array, uint capacity) : this()
        {
            _ptr = array;
            _capacity = capacity;
        }

        public void CopyTo(uint sourceStartIndex, T[] destination, uint destinationStartIndex, uint count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[i] = this[i];
            }
        }
        public void Clear()
        {
            MemoryUtilities.MemClear<T>(_ptr, _capacity);
        }

        public T[] ToManagedArray()
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr ToNativeArray(out int capacity)
        {
            capacity = (int) _capacity; return _ptr; 
        }

        public int capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int) _capacity;
        }

        public bool isValid => _ptr != IntPtr.Zero;

        public ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
#if DEBUG && !PROFILE_SVELTO
                    if (index >= _capacity)
                        throw new Exception($"NativeBuffer - out of bound access: index {index} - capacity {capacity}");
#endif
                    var size = MemoryUtilities.SizeOf<T>();
                    ref var asRef = ref Unsafe.AsRef<T>((void*) (_ptr + (int) (index * size)));
                    return ref asRef;
                }
            }
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
#if DEBUG && !PROFILE_SVELTO
                    if (index < 0 || index >= _capacity)
                        throw new Exception($"NativeBuffer - out of bound access: index {index} - capacity {capacity}");
#endif
                    var size = MemoryUtilities.SizeOf<T>();
                    ref var asRef = ref Unsafe.AsRef<T>((void*) (_ptr + (int) (index * size)));
                    return ref asRef;
                }
            }
        }

        readonly uint _capacity;
#if UNITY_COLLECTIONS
        //todo can I remove this from here? it should be used outside
        [Unity.Burst.NoAlias]
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
#endif
        readonly IntPtr _ptr; 

        public NB<T> AsReader() { return this; }
        public NB<T> AsWriter() { return this; }
    }
}
