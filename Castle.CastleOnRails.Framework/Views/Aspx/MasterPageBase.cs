using System.IO;
// Copyright 2004-2005 Castle Project - http://www.castleproject.org/
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

namespace Castle.CastleOnRails.Framework.Views.Aspx
{
	using System;
	using System.Web.UI;

	/// <summary>
	/// Summary description for MasterPageBase.
	/// </summary>
	public abstract class MasterPageBase : Page
	{
		const string ViewStateKey = "__MASTERVIEWSTATE";

		protected override object LoadPageStateFromPersistenceMedium()
		{
			String viewState =  Request.Params[ViewStateKey];

			if (viewState == null)
			{
				return null;
			}
			else
			{
				LosFormatter formatter = new LosFormatter();
				return formatter.Deserialize(viewState);
			}
		}

		protected override void SavePageStateToPersistenceMedium(object viewState)
		{
			StringWriter writer = new StringWriter();
			new LosFormatter().Serialize(writer, viewState);
			RegisterHiddenField( ViewStateKey, writer.GetStringBuilder().ToString() );
		}
	}
}
