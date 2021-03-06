#region Apache Notice
/*****************************************************************************
 * 
 * Castle.Igloo
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 ********************************************************************************/
#endregion

using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Castle.Core;
using Castle.Igloo.Util;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;
using Castle.Igloo.Attributes;


namespace Castle.Igloo
{
    /// <summary>
    /// Retrieves injected/outjected properties
    /// </summary>
    public class BijectionInspector : IContributeComponentModelConstruction
    {
        /// <summary>
        /// In members token
        /// </summary>
        public const string IN_MEMBERS = "_IN_MEMBERS_";

        /// <summary>
        /// Out members token
        /// </summary>
        public const string OUT_MEMBERS = "_OUT_MEMBERS_";

        /// <summary>
        /// Binding token
        /// </summary>
        private BindingFlags BINDING_FLAGS_SET
            = BindingFlags.Public
            | BindingFlags.SetProperty
            | BindingFlags.Instance
            | BindingFlags.SetField
            ;
        
        #region IContributeComponentModelConstruction Members

        /// <summary>
        /// Usually the implementation will look in the configuration property
        /// of the model or the service interface, or the implementation looking for
        /// something.
        /// </summary>
        /// <param name="kernel">The kernel instance</param>
        /// <param name="model">The component model</param>
        public void ProcessModel(IKernel kernel, ComponentModel model)
        {
            RetrieveInMembers(model);
            RetrieveOutMembers(model);
        }

        #endregion

        private void RetrieveInMembers(ComponentModel model)
        {
            PropertyInfo[] properties = model.Implementation.GetProperties(BINDING_FLAGS_SET);

            IDictionary<InjectAttribute, PropertyInfo> inMembers = new Dictionary<InjectAttribute, PropertyInfo>();

            for (int i = 0; i < properties.Length; i++)
            {
                InjectAttribute injectAttribute = AttributeUtil.GetInjectAttribute(properties[i]);
                if (injectAttribute != null)
                {
                    if (injectAttribute.Name.Length == 0)
                    {
                        injectAttribute.Name = properties[i].Name;
                    }
                    MethodInfo methodInfo = properties[i].GetSetMethod();
                    if (methodInfo == null)
                    {
                        throw new ConfigurationErrorsException("Outject attribute requires a set method on properties " + properties[i].Name + " in model " + model.Name);
                    }

                    inMembers.Add(injectAttribute, properties[i]);
                }
            }
            model.ExtendedProperties[IN_MEMBERS] = inMembers;
        }

        private void RetrieveOutMembers(ComponentModel model)
        {
            PropertyInfo[] properties = model.Implementation.GetProperties(BINDING_FLAGS_SET);

            IDictionary<OutjectAttribute, PropertyInfo> outMembers = new Dictionary<OutjectAttribute, PropertyInfo>();

            for (int i = 0; i < properties.Length; i++)
            {
                OutjectAttribute outjectAttribute = AttributeUtil.GetOutjectAttribute(properties[i]);
                if (outjectAttribute != null)
                {
                    if (outjectAttribute.Name.Length == 0)
                    {
                        outjectAttribute.Name = properties[i].Name;
                    }
                    MethodInfo methodInfo = properties[i].GetGetMethod();
                    if (methodInfo==null)
                    {
                        throw new ConfigurationErrorsException("Outject attribute requires a get method on properties "+properties[i].Name+" in model "+model.Name);
                    }
                    outMembers.Add(outjectAttribute, properties[i]);
                }
            }
            model.ExtendedProperties[OUT_MEMBERS] = outMembers;
        }
    }
}
