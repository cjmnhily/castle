// Copyright 2004-2006 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Core
{
	using System;
	using System.Collections;
	using System.Reflection;

	/// <summary>
	/// Collection of <see cref="PropertySet"/>
	/// </summary>
	[Serializable]
	public class PropertySetCollection : ReadOnlyCollectionBase
	{
		/// <summary>
		/// Adds the specified property.
		/// </summary>
		/// <param name="property">The property.</param>
		public void Add(PropertySet property)
		{
			InnerList.Add(property);
		}

		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear()
		{
			InnerList.Clear();
		}
		
		/// <summary>
		/// Finds a PropertySet the by PropertyInfo.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <returns></returns>
		public PropertySet FindByPropertyInfo(PropertyInfo info)
		{
			foreach(PropertySet prop in InnerList)
			{
				if (info == prop.Property)
				{
					return prop;
				}
			}
			
			return null;
		}
	}
}