namespace ValidationTestSite.Controllers
{
	using System;
	using Castle.Components.Validator;
	using Castle.MonoRail.Framework;
	using ValidationTestSite.Models;

	[Layout("default"), Rescue("generalerror")]
	public class HomeController : SmartDispatcherController
	{
		public HomeController()
		{
			// validatorEngine = new Validator(new PropertyBasedRegistry());
		}

		public void Index()
		{
			PropertyBag["client"] = new Client();
		}

		public void Index2()
		{
			PropertyBag["client"] = new Client();
		}

		public void Save([DataBind("client")] Client client)
		{
			PropertyBag["isvalid"] = ValidatorEngine.IsValid(client);
		}
	}
}
