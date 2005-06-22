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

namespace Castle.MonoRail.Framework
{
	using System;

	/// <summary>
	/// Abstract base class for View Engines.
	/// </summary>
	public abstract class ViewEngineBase : IViewEngine
	{
		private string _viewRootDir;

		public ViewEngineBase()
		{
		}

		/// <summary>
		/// The root directory of views, obtained
		/// from the configuration.
		/// </summary>
		public String ViewRootDir
		{
			get { return _viewRootDir; }
			set { _viewRootDir = value; }
		}

		/// <summary>
		/// Initializes the View Engine.
		/// </summary>
		public abstract void Init();

		/// <summary>
		/// Implementors should check if the specified
		/// template exists
		/// </summary>
		/// <param name="templateName"></param>
		/// <returns><c>true</c> if it exists</returns>
		public abstract bool HasTemplate(String templateName);

		/// <summary>
		/// Implementors should process the view (using the templateName to
		/// obtain the correct template) and use the context to output
		/// the result.
		/// </summary>
		public abstract void Process(IRailsEngineContext context, Controller controller, String templateName);

		public abstract void ProcessContents(IRailsEngineContext context, Controller controller, String contents);
	}
}