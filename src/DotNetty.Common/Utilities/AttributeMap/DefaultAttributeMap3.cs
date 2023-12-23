// using System;
// using System.Collections;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Threading;
//
// namespace DotNetty.Common.Utilities
// {
//     public class AtomicReference1
//     {
//         
//     }
//     public class AtomicReference1<T> : AtomicReference1
//     {
//         public AtomicReference1(T originalValue)
//         {
//             atomicValue = originalValue;
//         }
//         public AtomicReference1()
//         {
//             atomicValue = default;
//         }
//
//         public T atomicValue;
//         
//         // public static implicit operator T(AtomicReference1<T> aRef)
//         // {
//         //     return aRef.atomicValue.Value;
//         // }
//     }
//     
//
//     public class ConstantMap1
//     {
//         private readonly ConcurrentDictionary<IConstant, int> attributes;
//
//         public ConstantMap1()
//         {
//             this.attributes = new ConcurrentDictionary<IConstant, int>();
//         }
//
//         public bool HasConstant<T>(IConstant<T> key)
//         {
//             return this.attributes.ContainsKey(key);
//         }
//
//         public bool DelConstant<T>(IConstant<T> key)
//         {
//             return this.attributes.TryRemove(key, out _);
//         }
//         private static int refint;
//
//         public int SetConstant<T>(IConstant<T> key, Func<int, int> updateValueFactory)
//         {
//             var addOrUpdate = this.attributes.AddOrUpdate(key, constant => default(int),
//                 (constant, reference1) =>
//                 {
//                     Console.WriteLine($"{Interlocked.Increment(ref refint)}");
//                     return Interlocked.Increment(ref reference1);
//                 });
//             return addOrUpdate;
//         }
//
//         // public IEnumerator<KeyValuePair<IConstant, IConstantAccessor>> GetEnumerator() => this.attributes.GetEnumerator();
//         //
//         // IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
//         public IEnumerator<KeyValuePair<IConstant, IConstantAccessor>> GetEnumerator() => default;
//         
//
//         public ICollection<IConstantAccessor> Values => new List<IConstantAccessor>();
//     }
// }