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
//             atomicValue = () => originalValue;
//         }
//         public AtomicReference1()
//         {
//             atomicValue = () => default;
//         }
//
//         public Func<T> atomicValue;
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
//         private readonly ConcurrentDictionary<IConstant, AtomicReference1> attributes;
//
//         public ConstantMap1()
//         {
//             this.attributes = new ConcurrentDictionary<IConstant, AtomicReference1>();
//         }
//         // public T GetConstant<T>(IConstant<T> key)
//         // {
//         //     return (AtomicReference1<T>)this.attributes.GetOrAdd(key, new AtomicReference1<T>());
//         // }
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
//
//         public T SetConstant<T>(IConstant<T> key, Func<T, T> updateValueFactory)
//         {
//             var addOrUpdate = (AtomicReference1<T>)this.attributes.AddOrUpdate(key, constant => new AtomicReference1<T>(),
//                 (constant, reference1) =>
//                 {
//                     var atomicReference1 = (AtomicReference1<T>)reference1;
//                     var atomicReference1AtomicValue = atomicReference1.atomicValue();
//                     atomicReference1.atomicValue = () => updateValueFactory(atomicReference1AtomicValue);
//                     return atomicReference1;
//                 });
//             return addOrUpdate.atomicValue();
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