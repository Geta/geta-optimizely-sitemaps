using AlloyTemplates.Models;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using EPiServer.Web.Routing;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Security;
using EPiServer.DataAbstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AlloyMvcTemplates.Infrastructure;
using System.Threading.Tasks;
using EPiServer.Authorization;
using EPiServer.Framework.Security;

namespace AlloyTemplates.Controllers
{
    /// <summary>
    /// Used to register a user for first time
    /// </summary>
    [RegisterFirstAdminWithLocalRequest]
    public class RegisterController : Controller
    {
        string AdminRoleName = Roles.WebAdmins;
        public const string ErrorKey = "CreateError";

        public IActionResult Index()
        {
            return View();
        }

        //
        // POST: /Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryReleaseToken]
        public async Task<ActionResult> Index(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UIUserProvider.CreateUserAsync(model.Username, model.Password, model.Email, null, null, true);
                if (result.Status == UIUserCreateStatus.Success)
                {
                    await UIRoleProvider.CreateRoleAsync(AdminRoleName);
                    await UIRoleProvider.AddUserToRolesAsync(result.User.Username, new string[] { AdminRoleName});

                    AdministratorRegistrationPageMiddleware.IsEnabled = false;
                    SetFullAccessToWebAdmin();
                    var resFromSignIn = await UISignInManager.SignInAsync(UIUserProvider.Name, model.Username, model.Password);
                    if (resFromSignIn)
                    {
                        return Redirect("/");
                    }
                }
                AddErrors(result.Errors);
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private void SetFullAccessToWebAdmin()
        {
            var securityrep = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            var permissions = securityrep.Get(ContentReference.RootPage).CreateWritableClone() as IContentSecurityDescriptor;
            permissions.AddEntry(new AccessControlEntry(AdminRoleName, AccessLevel.FullAccess));
            securityrep.Save(ContentReference.RootPage, permissions, SecuritySaveType.Replace);
        }

        private void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(ErrorKey, error);
            }
        }

        UIUserProvider UIUserProvider
        {
            get
            {
                return ServiceLocator.Current.GetInstance<UIUserProvider>();
            }
        }
        UIRoleProvider UIRoleProvider
        {
            get
            {
                return ServiceLocator.Current.GetInstance<UIRoleProvider>();
            }
        }
        UISignInManager UISignInManager
        {
            get
            {
                return ServiceLocator.Current.GetInstance<UISignInManager>();
            }
        }

    }
}
