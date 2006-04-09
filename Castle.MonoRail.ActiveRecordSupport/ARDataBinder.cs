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

namespace Castle.MonoRail.ActiveRecordSupport
{
	using System;
	using System.Collections;
	using System.Reflection;

	using Castle.ActiveRecord;
	using Castle.ActiveRecord.Framework.Internal;
	using Castle.Components.Binder;
	using Castle.MonoRail.Framework;
	
	using Iesi.Collections;

	/// <summary>
	/// Extends <see cref="DataBinder"/> class with some 
	/// ActiveRecord specific functionality.
	/// <seealso cref="AutoLoadBehavior"/>
	/// <seealso cref="ARDataBindAttribute"/>
	/// </summary>
	/// <remarks>
	/// Autoload can be turned on on the parameter, see <see cref="AutoLoadBehavior"/>.
	/// </remarks>
	public class ARDataBinder : DataBinder
	{
		protected internal static readonly object[] EmptyArg = new object[0];

		private AutoLoadBehavior autoLoad;
		private bool persistchanges;

		public ARDataBinder() : base()
		{
		}

		public bool PersistChanges
		{
			get { return persistchanges; }
			set { persistchanges = value; }
		}

		public AutoLoadBehavior AutoLoad
		{
			get { return autoLoad; }
			set { autoLoad = value; }
		}

		protected override object CreateInstance(Type instanceType, String paramPrefix, IBindingDataSourceNode node)
		{
			if (node == null)
			{
				throw new RailsException("Nothing found for the given prefix. Are you sure the form fields are using the prefix " + paramPrefix + "?");
			}

			if (IsContainerType(instanceType))
			{
				return CreateContainer(instanceType);
			}

			object instance = null;

			bool shouldLoad = autoLoad != AutoLoadBehavior.Never;

			String autoloadOverride = node.GetMetaEntryValue("autoload");

			if (autoloadOverride != null)
			{
				shouldLoad = (autoloadOverride == "yes" || autoloadOverride == "true");
			}

			if (shouldLoad)
			{
				if (instanceType.IsArray)
				{
					throw new RailsException("ARDataBinder AutoLoad does not support arrays");
				}

				ActiveRecordModel model = ActiveRecordModel.GetModel(instanceType);

				PrimaryKeyModel pkModel;

				object id = ObtainPKValue(model, node, paramPrefix, out pkModel);
				
				if (IsValidKey(id))
				{
					instance = ActiveRecordMediator.FindByPrimaryKey(instanceType, id, true);
				}
				else
				{
					if (autoLoad == AutoLoadBehavior.NewInstanceIfInvalidKey)
					{
						instance = base.CreateInstance(instanceType, paramPrefix, node);
					}
					else if (autoLoad == AutoLoadBehavior.NullIfInvalidKey)
					{
						instance = null;
					}
					else
					{
						throw new RailsException(string.Format(
							"Could not find primary key '{0}' for '{1}'", 
								pkModel.Property.Name, instanceType.FullName));
					}
				}
			}
			else
			{
				instance = base.CreateInstance(instanceType, paramPrefix, node);
			}

			return instance;
		}

		protected override bool ShouldRecreateInstance(object value, Type type, string prefix, IBindingDataSourceNode node)
		{
			// See http://support.castleproject.org/jira//browse/AR-41
			if (value == null) return true;

			ActiveRecordModel model = ActiveRecordModel.GetModel(type);

			if (AutoLoad == AutoLoadBehavior.Never || model == null)
			{
				return base.ShouldRecreateInstance(value, type, prefix, node);
			}

			PrimaryKeyModel pkModel;

			object id = ObtainPKValue(model, node, prefix, out pkModel);

			object currentId = pkModel.Property.GetValue(value, null);

			return !Object.ReferenceEquals(id, currentId);
		}

		protected override bool ShouldIgnoreType(Type instanceType)
		{
			if (IsContainerType(instanceType))
			{
				return false;
			}

			return base.ShouldIgnoreType(instanceType);
		}

		protected override bool PerformCustomBinding(object instance, string prefix, IBindingDataSourceNode node)
		{
			if (instance == null)
			{
				return true;
			}
			object stackInstance = InstanceOnStack;

			if (stackInstance == null)
			{
				return false;
			}

			if (IsContainerInstance(instance))
			{
				Type type = stackInstance.GetType();

				ActiveRecordModel model = ActiveRecordModel.GetModel(type);
				ActiveRecordModel targetModel = null;

				bool found = false;
				Type targetType = null;

				foreach(HasAndBelongsToManyModel hasManyModel in model.HasAndBelongsToMany)
				{
					if (hasManyModel.Property.Name == prefix)
					{
						targetType = hasManyModel.HasManyAtt.MapType;

						targetModel = ActiveRecordModel.GetModel(targetType);

						found = true;
						break;
					}
				}

				if (found)
				{
					ClearContainer(instance);

					if (node.IsIndexed)
					{
						Array collArray = Array.CreateInstance(targetType, node.IndexedNodes.Length);

						collArray = (Array) InternalBindObject(collArray.GetType(), prefix, node);

						foreach(object item in collArray)
						{
							AddToContainer(instance, item);
						}

						return true;
					}

					PrimaryKeyModel pkModel = targetModel.Ids[0] as PrimaryKeyModel;

					String ids = node.GetEntryValue(pkModel.Property.Name);

					if (ids == null)
					{
						return true;
					}

					foreach(String id in ids.Split(','))
					{
						object convertedId = ConvertUtils.Convert(pkModel.Property.PropertyType, id);

						AddToContainer(instance, ActiveRecordMediator.FindByPrimaryKey(targetType, convertedId, true));
					}
				}

				return true;
			}

			return false;
		}

		private static object ObtainPKValue(ActiveRecordModel model, IBindingDataSourceNode node, 
			String prefix, out PrimaryKeyModel pkModel)
		{
			if (model.Ids.Count != 1)
			{
				throw new RailsException("ARDataBinder does not support more than one primary key");
			}

			pkModel = model.Ids[0] as PrimaryKeyModel;

			String pkPropName = pkModel.Property.Name;
			String propValue = node.GetEntryValue(pkPropName);

			if (propValue == null)
			{
				throw new RailsException("ARDataBinder autoload failed as element {0} " +
					"doesn't have a primary key {1} value", prefix, pkPropName);
			}

			return ConvertUtils.Convert(pkModel.Property.PropertyType, propValue);
		}

		protected override void AfterBinding(object instance, String paramPrefix, IBindingDataSourceNode node)
		{
			// Defensive programming
			if (instance == null) return;

			ActiveRecordModel model = ActiveRecordModel.GetModel(instance.GetType());

			if (model == null) return;

//			if (validate)
//			{
//				ValidateInstances(instance);
//			}

			if (persistchanges)
			{
				SaveManyMappings(instance, model, node);
				PersistInstances(instance);
			}
		}

		private void PersistInstances(object instances)
		{
			Type instanceType = instances.GetType();
			ActiveRecordBase[] records = null;

			if (instanceType.IsArray)
			{
				records = instances as ActiveRecordBase[];
			}
			else if (typeof(ActiveRecordBase).IsAssignableFrom(instanceType))
			{
				records = new ActiveRecordBase[] {(ActiveRecordBase) instances};
			}

			if (records != null)
			{
				foreach(ActiveRecordBase record in records)
				{
					record.Save();
				}
			}
		}

//		private void ValidateInstances(object instances)
//		{
//			Type instanceType = instances.GetType();
//			ActiveRecordValidationBase[] records = null;
//
//			if (instanceType.IsArray)
//			{
//				records = instances as ActiveRecordValidationBase[];
//			}
//			else if (typeof(ActiveRecordValidationBase).IsAssignableFrom(instanceType))
//			{
//				records = new ActiveRecordValidationBase[] {(ActiveRecordValidationBase) instances};
//			}
//
//			if (records != null)
//			{
//				foreach(ActiveRecordValidationBase record in records)
//				{
//					if (!record.IsValid())
//					{
//						throw new RailsException("Error validating {0} {1}",
//							record.GetType().Name, string.Join("\n", record.ValidationErrorMessages));
//					}
//				}
//			}
//		}

		protected void SaveManyMappings(object instance, ActiveRecordModel model, IBindingDataSourceNode node)
		{
			foreach(HasManyModel hasManyModel in model.HasMany)
			{
				if (hasManyModel.HasManyAtt.Inverse) continue;
				if (hasManyModel.HasManyAtt.RelationType != RelationType.Bag &&
					hasManyModel.HasManyAtt.RelationType != RelationType.Set) continue;

				ActiveRecordModel otherModel = ActiveRecordModel.GetModel(hasManyModel.HasManyAtt.MapType);

				PrimaryKeyModel keyModel = ARCommonUtils.ObtainPKProperty(otherModel);

				if (otherModel == null || keyModel == null)
				{
					continue; // Impossible to save
				}

				CreateMappedInstances(instance, hasManyModel.Property, keyModel, otherModel, node);
			}

			foreach(HasAndBelongsToManyModel hasManyModel in model.HasAndBelongsToMany)
			{
				if (hasManyModel.HasManyAtt.Inverse) continue;
				if (hasManyModel.HasManyAtt.RelationType != RelationType.Bag &&
					hasManyModel.HasManyAtt.RelationType != RelationType.Set) continue;

				ActiveRecordModel otherModel = ActiveRecordModel.GetModel(hasManyModel.HasManyAtt.MapType);

				PrimaryKeyModel keyModel = ARCommonUtils.ObtainPKProperty(otherModel);

				if (otherModel == null || keyModel == null)
				{
					continue; // Impossible to save
				}

				CreateMappedInstances(instance, hasManyModel.Property, keyModel, otherModel, node);
			}
		}

		private void CreateMappedInstances(object instance, PropertyInfo prop,
			PrimaryKeyModel keyModel, ActiveRecordModel otherModel, IBindingDataSourceNode node)
		{
			object container = InitializeRelationPropertyIfNull(instance, prop);

			String paramName = String.Format("{0}.{1}", prop.Name, keyModel.Property.Name);

			int[] ids = (int[]) ConvertUtils.Convert(typeof(int[]), paramName, node, null);

			if (ids != null)
			{
				foreach(int id in ids)
				{
					object item = Activator.CreateInstance(otherModel.Type);

					keyModel.Property.SetValue(item, id, EmptyArg);

					AddToContainer(container, item);
				}
			}
		}

		private static object InitializeRelationPropertyIfNull(object instance, PropertyInfo property)
		{
			object container = property.GetValue(instance, EmptyArg);

			if (container == null)
			{
				if (property.PropertyType == typeof(IList))
				{
					container = new ArrayList();
				}
				else if (property.PropertyType == typeof(ISet))
				{
					container = new HashedSet();
				}

				property.SetValue(instance, container, EmptyArg);
			}

			return container;
		}

		private void AddToContainer(object container, object item)
		{
			if (container is IList)
			{
				(container as IList).Add(item);
			}
			else if (container is ISet)
			{
				(container as ISet).Add(item);
			}
		}

		private bool IsContainerType(Type type)
		{
			return type == typeof(IList) || type == typeof(ISet);
		}

		private bool IsContainerInstance(object instance)
		{
			return (instance is IList || instance is ISet);
		}

		private bool IsValidKey(object id)
		{
			if (id != null)
			{
				if (id.GetType() == typeof(String))
				{
					return id.ToString() != String.Empty;
				}
				else
				{
					return Convert.ToInt64(id) != 0;
				}
			}
			
			return false;
		}

		private object CreateContainer(Type type)
		{
			if (type == typeof(IList))
			{
				return new ArrayList();
			}
			else if (type == typeof(ISet))
			{
				return new HashedSet();
			}

			return null;
		}

		private void ClearContainer(object instance)
		{
			if (instance is IList)
			{
				(instance as IList).Clear();
			}
			else if (instance is ISet)
			{
				(instance as ISet).Clear();
			}
		}
	}
}