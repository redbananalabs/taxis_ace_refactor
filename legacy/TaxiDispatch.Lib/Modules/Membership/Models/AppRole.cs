using Microsoft.AspNetCore.Identity;

namespace TaxiDispatch.Modules.Membership;

public class AppRole : IdentityRole<int>
{
    public AppRole()
    {
    }

    public AppRole(string name)
    {
        Name = name;
    }
}
