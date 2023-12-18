// using System;
// using System.Threading;
//
// namespace DotNetty.Common.Utilities
// {
//     public abstract class AbstractConstant : IConstant
//     {
//         private static long nextUniqueId;
//         private long uniqueId;
//
//         public int Id { get; }
//         public string Name { get; }
//         
//         protected AbstractConstant(int id, string name)
//         {
//             this.Id = id;
//             this.Name = name;
//         }
//
//         public sealed override string ToString() => this.Name;
//
//         protected long UniqueId
//         {
//             get
//             {
//                 long result;
//                 if ((result = Volatile.Read(ref this.uniqueId)) == 0)
//                 {
//                     result = Interlocked.Increment(ref nextUniqueId);
//                     long previousUniqueId = Interlocked.CompareExchange(ref this.uniqueId, result, 0);
//                     if (previousUniqueId != 0)
//                     {
//                         result = previousUniqueId;
//                     }
//                 }
//
//                 return result;
//             }
//         }
//     }
//
//     public abstract class AbstractConstant<T> : AbstractConstant, IComparable<T>, IEquatable<T> where T : AbstractConstant<T>
//     {
//         protected AbstractConstant(int id, string name) : base(id, name)
//         {
//         }
//
//         public sealed override int GetHashCode() => base.GetHashCode();
//
//         public sealed override bool Equals(object obj) => base.Equals(obj);
//
//         public bool Equals(T other) => ReferenceEquals(this, other);
//
//         public int CompareTo(T other)
//         {
//             if (ReferenceEquals(this, other)) return 0;
//
//             var returnCode = this.GetHashCode() - other.GetHashCode();
//             if (returnCode != 0)
//                 return returnCode;
//             if (this.UniqueId < other.UniqueId)
//                 return -1;
//             if (this.UniqueId > other.UniqueId)
//                 return 1;
//
//             throw new Exception("failed to compare two different constants");
//         }
//     }
// }