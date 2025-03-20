//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Options;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using SchoolSystem.Models; 

//public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser>
//{
//	public CustomClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> optionsAccessor)
//		: base(userManager, optionsAccessor) { }

//	protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
//	{
//		var identity = await base.GenerateClaimsAsync(user);
//		identity.AddClaim(new Claim("FullName", user.Name ?? ""));
//		return identity;
//	}
//}
