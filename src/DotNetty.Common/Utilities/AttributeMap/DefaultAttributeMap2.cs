// using System;
// using System.Collections;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace DotNetty.Common.Utilities
// {
//     public interface IConstantTransfer
//     {
//         protected internal bool TransferSet<T>(IConstant<T> constant, T value);
//     }
//
//     public interface IConstantAccessor
//     {
//         public bool TransferSet(IConstantTransfer transfer);
//     }
//
//     public class ConstantMap : IEnumerable<KeyValuePair<IConstant, IConstantAccessor>>
//     {
//         private readonly ConcurrentDictionary<IConstant, Lazy<IConstantAccessor>> attributes;
//
//         private class Atomic<T> : IConstantAccessor
//         {
//             private readonly IConstant<T> key;
//             internal T Value;
//
//             public Atomic(IConstant key, T value = default)
//             {
//                 this.key = (IConstant<T>)key;
//                 this.Value = value;
//             }
//             
//             public static implicit operator T(Atomic<T> atomic) => atomic.Value;
//             
//             public bool TransferSet(IConstantTransfer transfer) => transfer.TransferSet(this.key, this.Value);
//         }
//         
//         public ConstantMap()
//         {
//             this.attributes = new ConcurrentDictionary<IConstant, Lazy<IConstantAccessor>>();
//         }
//         
//         public ConstantMap(ConstantMap attributeMap)
//         {
//             this.attributes = new ConcurrentDictionary<IConstant, Lazy<IConstantAccessor>>(attributeMap.attributes);
//         }
//         
//         public T GetConstant<T>(IConstant<T> key)
//         {
//             if (this.attributes.TryGetValue(key, out var lazy))
//             {
//                 return (Atomic<T>)lazy.Value;
//             }
//
//             throw new Exception($"NotFind {key}");
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
//
//         public T SetConstant<T>(IConstant<T> key, T value)
//         {
//             var lazy = this.attributes.AddOrUpdate(key, new Lazy<IConstantAccessor>(() => new Atomic<T>(key, value)),
//                 (_, oldLazy) => new Lazy<IConstantAccessor>(() =>
//                 {
//                     var atomic = (Atomic<T>)oldLazy.Value;
//                     atomic.Value = value;
//                     return oldLazy.Value;
//                 }));
//             return (Atomic<T>)lazy.Value;
//         }
//         
//         public T UpdateConstant<T>(IConstant<T> key, Func<T, T> updateFactory)
//         {
//             var lazy = this.attributes.AddOrUpdate(key, new Lazy<IConstantAccessor>(() => new Atomic<T>(key)), 
//                 (_, oldLazy) => new Lazy<IConstantAccessor>(() =>
//                 {
//                     var atomic = (Atomic<T>)oldLazy.Value;
//                     atomic.Value = updateFactory(atomic.Value);
//                     return oldLazy.Value;
//                 }));
//             return (Atomic<T>)lazy.Value;
//         }
//
//         public ICollection<IConstantAccessor> Values => this.attributes.Values.Select(lazy => lazy.Value).ToList();
//
//         public IEnumerator<KeyValuePair<IConstant, IConstantAccessor>> GetEnumerator() => 
//             this.attributes.Select(pair => new KeyValuePair<IConstant, IConstantAccessor>(pair.Key, pair.Value.Value)).GetEnumerator(); 
//
//         IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
//     }
// }