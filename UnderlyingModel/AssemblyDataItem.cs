/**
 * @(#) AssemblyDataItem.cs
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;

namespace UnderlyingModel
{
	/// <summary>
    /// Object represents each Assembly Info.
    /// </summary>
    [Serializable]
    public class AssemblyDataItem : IDataItem, IComparable
    {
		internal String UniqueName { get; set; }

		public AssemblyType assemblyType { get; private set; }
		public TypeDefinition cecilType { get; private set; }
		internal AssemblyDataItem Parent { get; private set; }

		private readonly List<AssemblyDataItem> _children;

        public MethodDefinition MInfo = null;
        public PropertyDefinition PInfo = null;
		public FieldDefinition FInfo = null;

        internal DocumentDataItem DocDataItem;

        public AssemblyDataItem()
        {
            _children = new List<AssemblyDataItem>();
        }

		public AssemblyDataItem(AssemblyType at, TypeDefinition td, AssemblyDataItem p) : this()
	    {
			assemblyType = at;
			cecilType = td;
			Parent = p;
		}

		public AssemblyDataItem(AssemblyType at, TypeDefinition td, string n): this()
		{
			assemblyType = at;
			cecilType = td;
			UniqueName = n;
		}

        public void AddChild(AssemblyDataItem child)
        {
            _children.Add(child);
        }

        // DataItem Impl.
        public String ItemName()
        {
            return UniqueName;
        }

        public String ItemType()
        {
            switch(assemblyType) {
                case AssemblyType.Enum:
                    return "enum type";
                case AssemblyType.EnumValue:
                    return "enum value";
                case AssemblyType.Property:
                case AssemblyType.Primitive:
				case AssemblyType.Class:
				case AssemblyType.Struct:
				case AssemblyType.Constructor:
				case AssemblyType.Field:
            		return assemblyType.ToString().ToLower();
				case AssemblyType.Method:
            		return UniqueName.Contains("implop") ? "implicit operator" : "method";
            }

            return "unclassified(todo)";
        }

		public AssemblyDataItem GetAssemblyDataItem()
        {
            return this;
        }
        public DocumentDataItem GetDocumentDataItem()
        {
            return DocDataItem;
        }

		public void SetDocumentDataItem(DocumentDataItem other)
		{
			DocDataItem = other;
		}

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj"/>. Zero This instance is equal to <paramref name="obj"/>. Greater than zero This instance is greater than <paramref name="obj"/>. 
		/// </returns>
		/// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
		public int CompareTo(object obj)
        {
            var item = (IDataItem)obj;
            return UniqueName.CompareTo(item.ItemName());
        }
    }
}
